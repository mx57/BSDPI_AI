using System.Collections.Concurrent;
using FluxRoute.AI.Models;
using FluxRoute.Core.Models;

namespace FluxRoute.AI.Services;

public sealed class BanditSelector
{
    private readonly AiStrategyRegistry _registry;
    private readonly Func<AiSettings> _aiSettings;
    private readonly Random _rng;
    private readonly ConcurrentDictionary<Guid, DateTimeOffset> _blockedUntil = new();
    private readonly ConcurrentDictionary<string, DateTimeOffset> _sigCooldown = new();
    private readonly ConcurrentDictionary<string, DateTimeOffset> _familyCooldown = new();
    private readonly ConcurrentDictionary<Guid, int> _failureStreak = new();

    public BanditSelector(AiStrategyRegistry registry, Func<AiSettings>? aiSettings = null, Random? rng = null)
    {
        _registry = registry;
        _aiSettings = aiSettings ?? (() => new AiSettings());
        _rng = rng ?? new Random();
    }

    public StrategyGenome? Pick(
        IReadOnlyList<StrategyGenome> candidates,
        string networkHash,
        int explorationPermil)
    {
        var now = DateTimeOffset.UtcNow;
        var usable = candidates.Where(g =>
            !_blockedUntil.TryGetValue(g.Id, out var u) || u <= now).ToList();
        if (usable.Count == 0)
            return null;

        var totalTOnNet = _registry.TotalPullsOnNetwork(networkHash);

        // BOLT ⚡: Adaptive exploration. Reduce exploration as we gain more experience on this network.
        var adaptiveExploration = explorationPermil / (1.0 + Math.Sqrt(totalTOnNet / 50.0));

        if (_rng.NextDouble() * 1000 < adaptiveExploration)
        {
            usable.Sort((a, b) =>
                _registry.SumPullsForGenomeOnNetwork(a.Id, networkHash)
                    .CompareTo(_registry.SumPullsForGenomeOnNetwork(b.Id, networkHash)));
            return usable[0];
        }

        var useUcb1 = _aiSettings().UseUcb1;
        var scoredUsable = usable.Select(g =>
        {
            var entry = _registry.GetOrCreateBandit(g.Id, networkHash);
            var pulls = entry.Alpha + entry.Beta - 2;
            double alpha = entry.Alpha;
            double beta = entry.Beta;
            double score;

            if (useUcb1)
            {
                double mean;
                if (pulls < 1)
                {
                    var agg = _registry.GetAggregatedBeta(g.Id);
                    mean = agg.Alpha / (agg.Alpha + agg.Beta);
                }
                else
                {
                    mean = alpha / (alpha + beta);
                }
                var n = Math.Max(1.0, pulls);
                score = mean + Math.Sqrt(2 * Math.Log(totalTOnNet + 1) / n);
            }
            else
            {
                if (pulls < 1)
                {
                    var agg = _registry.GetAggregatedBeta(g.Id);
                    var apulls = agg.Alpha + agg.Beta - 2;
                    score = apulls < 0.5 ? 0.5 : SampleBeta(agg.Alpha, agg.Beta);
                }
                else
                {
                    score = SampleBeta(alpha, beta);
                }
            }

            return (g, score, latency: entry.AvgLatency);
        }).ToList();

        if (_aiSettings().ParetoEnabled)
        {
            var pareto = GetParetoFrontInternal(scoredUsable);
            // BOLT ⚡: Multi-objective selection. Pick a random strategy from the Pareto front
            // to properly balance exploration-aware success vs. latency.
            return pareto[_rng.Next(pareto.Count)].g;
        }

        return scoredUsable.OrderByDescending(x => x.score).First().g;
    }

    public StrategyGenome? BestKnownForNetwork(IReadOnlyList<StrategyGenome> candidates, string networkHash)
    {
        StrategyGenome? best = null;
        double bestMean = -1;
        foreach (var g in candidates)
        {
            var entry = _registry.GetOrCreateBandit(g.Id, networkHash);
            var pulls = entry.Alpha + entry.Beta - 2;
            if (pulls < 1)
                continue;

            var mean = entry.Alpha / (entry.Alpha + entry.Beta);
            if (mean > bestMean)
            {
                bestMean = mean;
                best = g;
            }
        }

        return best;
    }

    public List<StrategyGenome> ParetoFront(IReadOnlyList<StrategyGenome> candidates, string networkHash)
    {
        var scored = candidates
            .Select(g =>
            {
                var entry = _registry.GetOrCreateBandit(g.Id, networkHash);
                var pulls = entry.Alpha + entry.Beta - 2;
                if (pulls < 1) return (g, score: 0.5, latency: 1000.0);
                var score = entry.Alpha / (entry.Alpha + entry.Beta);
                var latency = entry.AvgLatency;
                return (g, score, latency);
            })
            .ToList();

        return GetParetoFrontInternal(scored).Select(x => x.g).ToList();
    }

    private List<(StrategyGenome g, double score, double latency)> GetParetoFrontInternal(
        IReadOnlyList<(StrategyGenome g, double score, double latency)> scored)
    {
        var pareto = new List<(StrategyGenome g, double score, double latency)>();
        foreach (var item in scored)
        {
            bool dominated = false;
            foreach (var other in scored)
            {
                if (other.g.Id == item.g.Id) continue;
                // BOLT ⚡: Pareto now uses exploration-aware scores (UCB or Samples) when called from Pick(),
                // preventing under-explored strategies from being prematurely discarded.
                if (other.score >= item.score && other.latency <= item.latency &&
                    (other.score > item.score || other.latency < item.latency))
                {
                    dominated = true;
                    break;
                }
            }
            if (!dominated) pareto.Add(item);
        }

        return pareto;
    }

    public void RegisterSuccess(Guid genomeId)
    {
        _failureStreak.TryRemove(genomeId, out _);
        _blockedUntil.TryRemove(genomeId, out _);
    }

    public void RegisterFailure(StrategyGenome g, string? failureSignature)
    {
        var id = g.Id;
        var streak = _failureStreak.AddOrUpdate(id, 1, (_, s) => s + 1);

        var backoffMs = streak switch
        {
            1 => 300,
            2 => 700,
            3 => 1500,
            _ => 3000,
        };

        var jitter = 1 + (_rng.NextDouble() * 0.7 - 0.35);
        var delay = TimeSpan.FromMilliseconds(backoffMs * jitter);
        var until = DateTimeOffset.UtcNow + delay;

        _blockedUntil.AddOrUpdate(id, until, (_, existing) => until > existing ? until : existing);

        var famKey = $"{id:N}|{g.DesyncMode}";
        _familyCooldown[famKey] = DateTimeOffset.UtcNow.AddSeconds(15);

        if (!string.IsNullOrEmpty(failureSignature))
        {
            var sigKey = $"{id:N}|{failureSignature}";
            _sigCooldown[sigKey] = DateTimeOffset.UtcNow.AddSeconds(15);
        }
    }

    public bool IsFamilyCooling(StrategyGenome g)
    {
        var famKey = $"{g.Id:N}|{g.DesyncMode}";
        return _familyCooldown.TryGetValue(famKey, out var u) && u > DateTimeOffset.UtcNow;
    }

    private double SampleBeta(double alpha, double beta)
    {
        var x = GammaSample(alpha);
        var y = GammaSample(beta);
        return x / (x + y + 1e-12);
    }

    private double GammaSample(double shape)
    {
        if (shape < 1e-9)
            return 1e-9;
        if (shape < 1)
            return GammaSample(shape + 1) * Math.Pow(_rng.NextDouble(), 1 / shape);

        var d = shape - 1.0 / 3;
        var c = 1 / Math.Sqrt(9 * d);
        while (true)
        {
            double x;
            do
            {
                x = NormalSample();
            } while (x <= -1 / c);

            var v = 1 + c * x;
            v = v * v * v;
            var u = _rng.NextDouble();
            if (u < 1 - 0.0331 * x * x * x * x)
                return d * v;

            if (Math.Log(u) < 0.5 * x * x + d * (1 - v + Math.Log(v)))
                return d * v;
        }
    }

    private double NormalSample()
    {
        var u1 = 1 - _rng.NextDouble();
        var u2 = 1 - _rng.NextDouble();
        return Math.Sqrt(-2 * Math.Log(u1)) * Math.Cos(2 * Math.PI * u2);
    }
}
