using System.Text.Json;
using System.Text.Json.Serialization;
using FluxRoute.AI.Models;

namespace FluxRoute.AI.Services;

public sealed class BanditStateEntry
{
    public Guid GenomeId { get; set; }
    public string NetworkHash { get; set; } = "";
    public double Alpha { get; set; } = 1;
    public double Beta { get; set; } = 1;
    public double AvgLatency { get; set; } = 1000;
}

public sealed class AiStrategyPersistedModel
{
    public List<StrategyGenome> Genomes { get; set; } = [];
    public List<BanditStateEntry> Bandit { get; set; } = [];
    public int GenerationCounter { get; set; }
    public List<string> SeenNetworkHashes { get; set; } = [];
}

public sealed class AiStrategyRegistry
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly string _path;
    private readonly object _gate = new();
    private AiStrategyPersistedModel _model = new();

    // BOLT ⚡: Indexing dictionaries to replace O(N) linear searches with O(1) lookups.
    // Significant performance boost when bandit history grows across multiple networks.
    private readonly Dictionary<(Guid GenomeId, string NetworkHash), BanditStateEntry> _banditLookup = [];
    private readonly Dictionary<Guid, List<BanditStateEntry>> _genomeLookup = [];
    private readonly Dictionary<string, List<BanditStateEntry>> _networkLookup = [];
    private readonly Dictionary<Guid, StrategyGenome> _genomeIdLookup = [];

    public AiStrategyRegistry(string path)
    {
        _path = path;
    }

    public int GenerationCounter
    {
        get { lock (_gate) return _model.GenerationCounter; }
        set { lock (_gate) _model.GenerationCounter = value; }
    }

    private void RebuildLookups()
    {
        _banditLookup.Clear();
        _genomeLookup.Clear();
        _networkLookup.Clear();
        _genomeIdLookup.Clear();

        foreach (var g in _model.Genomes)
        {
            _genomeIdLookup[g.Id] = g;
        }

        foreach (var e in _model.Bandit)
        {
            AddEntryToLookups(e);
        }
    }

    private void AddEntryToLookups(BanditStateEntry e)
    {
        _banditLookup[(e.GenomeId, e.NetworkHash)] = e;

        if (!_genomeLookup.TryGetValue(e.GenomeId, out var gList))
        {
            gList = [];
            _genomeLookup[e.GenomeId] = gList;
        }
        gList.Add(e);

        if (!_networkLookup.TryGetValue(e.NetworkHash, out var nList))
        {
            nList = [];
            _networkLookup[e.NetworkHash] = nList;
        }
        nList.Add(e);
    }

    public void Load()
    {
        lock (_gate)
        {
            if (!File.Exists(_path))
            {
                _model = new AiStrategyPersistedModel();
                return;
            }

            try
            {
                var json = File.ReadAllText(_path);
                _model = JsonSerializer.Deserialize<AiStrategyPersistedModel>(json, JsonOptions) ?? new();
                _model.Genomes ??= [];
                _model.Bandit ??= [];
                _model.SeenNetworkHashes ??= [];
            }
            catch
            {
                _model = new AiStrategyPersistedModel();
            }

            RebuildLookups();
        }
    }

    public void Save()
    {
        lock (_gate)
        {
            var dir = Path.GetDirectoryName(_path);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            var json = JsonSerializer.Serialize(_model, JsonOptions);
            var tmp = _path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            File.WriteAllText(tmp, json);
            File.Copy(tmp, _path, overwrite: true);
            File.Delete(tmp);
        }
    }

    public IReadOnlyList<StrategyGenome> GetGenomes()
    {
        lock (_gate)
            return _model.Genomes.ToList();
    }

    public IReadOnlyList<StrategyGenome> GetActiveGenomes()
    {
        lock (_gate)
            return _model.Genomes.Where(g => g.OrchestratorEnabled).ToList();
    }

    public void SetOrchestratorEnabled(Guid id, bool enabled)
    {
        lock (_gate)
        {
            var g = _model.Genomes.FirstOrDefault(x => x.Id == id);
            if (g is null)
                return;
            g.OrchestratorEnabled = enabled;
        }

        Save();
    }

    public StrategyGenome? GetById(Guid id)
    {
        lock (_gate)
        {
            _genomeIdLookup.TryGetValue(id, out var g);
            return g;
        }
    }

    public void Upsert(StrategyGenome g)
    {
        lock (_gate)
        {
            var idx = _model.Genomes.FindIndex(x => x.Id == g.Id);
            if (idx >= 0)
                _model.Genomes[idx] = g;
            else
                _model.Genomes.Add(g);

            _genomeIdLookup[g.Id] = g;
        }
    }

    public bool Remove(Guid id)
    {
        lock (_gate)
        {
            var n = _model.Genomes.RemoveAll(g => g.Id == id);
            var removedBandits = _model.Bandit.RemoveAll(b => b.GenomeId == id);
            _genomeIdLookup.Remove(id);

            if (n > 0 || removedBandits > 0)
            {
                RebuildLookups();
            }

            return n > 0;
        }
    }

    public void MarkNetworkSeen(string hash)
    {
        lock (_gate)
        {
            if (!_model.SeenNetworkHashes.Contains(hash))
                _model.SeenNetworkHashes.Add(hash);
        }
    }

    public bool HasSeenNetwork(string hash)
    {
        lock (_gate)
            return _model.SeenNetworkHashes.Contains(hash);
    }

    public BanditStateEntry GetOrCreateBandit(Guid genomeId, string networkHash)
    {
        lock (_gate)
        {
            if (_banditLookup.TryGetValue((genomeId, networkHash), out var e))
                return e;

            e = new BanditStateEntry { GenomeId = genomeId, NetworkHash = networkHash, Alpha = 1, Beta = 1 };
            _model.Bandit.Add(e);
            AddEntryToLookups(e);
            return e;
        }
    }

    public void RecordBanditSuccess(Guid genomeId, string networkHash, double latencyMs)
    {
        lock (_gate)
        {
            if (!_banditLookup.TryGetValue((genomeId, networkHash), out var e))
            {
                e = new BanditStateEntry { GenomeId = genomeId, NetworkHash = networkHash, Alpha = 1, Beta = 1, AvgLatency = latencyMs };
                _model.Bandit.Add(e);
                AddEntryToLookups(e);
            }

            e.Alpha += 1;
            UpdateLatency(e, latencyMs);
        }
    }

    public void RecordBanditFailure(Guid genomeId, string networkHash, double latencyMs)
    {
        lock (_gate)
        {
            if (!_banditLookup.TryGetValue((genomeId, networkHash), out var e))
            {
                e = new BanditStateEntry { GenomeId = genomeId, NetworkHash = networkHash, Alpha = 1, Beta = 1, AvgLatency = latencyMs };
                _model.Bandit.Add(e);
                AddEntryToLookups(e);
            }

            e.Beta += 1;
            UpdateLatency(e, latencyMs);
        }
    }

    private static void UpdateLatency(BanditStateEntry e, double newLat)
    {
        if (newLat <= 0) return;
        // Moving average over ~10 trials
        const double alpha = 0.2;
        if (e.AvgLatency >= 999) e.AvgLatency = newLat;
        else e.AvgLatency = e.AvgLatency * (1 - alpha) + newLat * alpha;
    }

    public double SumPullsForGenomeOnNetwork(Guid genomeId, string networkHash)
    {
        lock (_gate)
        {
            if (!_banditLookup.TryGetValue((genomeId, networkHash), out var e))
                return 0;
            return e.Alpha + e.Beta - 2;
        }
    }

    public (double Alpha, double Beta) GetAggregatedBeta(Guid genomeId)
    {
        lock (_gate)
        {
            double succ = 0;
            double fail = 0;
            if (_genomeLookup.TryGetValue(genomeId, out var entries))
            {
                foreach (var x in entries)
                {
                    succ += x.Alpha - 1;
                    fail += x.Beta - 1;
                }
            }

            return (succ + 1, fail + 1);
        }
    }

    public double TotalPullsOnNetwork(string networkHash)
    {
        lock (_gate)
        {
            double n = 0;
            if (_networkLookup.TryGetValue(networkHash, out var entries))
            {
                foreach (var x in entries)
                    n += x.Alpha + x.Beta - 2;
            }
            return Math.Max(n, 0);
        }
    }

    public void ResetAll()
    {
        lock (_gate)
        {
            _model = new AiStrategyPersistedModel();
            RebuildLookups();
        }

        Save();
    }

    /// <summary>
    /// Retrieves a snapshot of all bandit states for a specific network under a single lock.
    /// BOLT ⚡: Optimizes UI updates and selector loops from O(N) to O(1) locking.
    /// </summary>
    public Dictionary<Guid, BanditStateEntry> GetBanditSnapshot(string networkHash)
    {
        lock (_gate)
        {
            var result = new Dictionary<Guid, BanditStateEntry>();
            if (_networkLookup.TryGetValue(networkHash, out var entries))
            {
                foreach (var e in entries)
                {
                    result[e.GenomeId] = e;
                }
            }
            return result;
        }
    }

    /// <summary>
    /// Retrieves aggregated Alpha/Beta statistics for all genomes under a single lock.
    /// BOLT ⚡: Optimizes global ranking calculations.
    /// </summary>
    public Dictionary<Guid, (double Alpha, double Beta)> GetAggregatedStatsSnapshot()
    {
        lock (_gate)
        {
            var result = new Dictionary<Guid, (double Alpha, double Beta)>();
            foreach (var kv in _genomeLookup)
            {
                double succ = 0;
                double fail = 0;
                foreach (var x in kv.Value)
                {
                    succ += x.Alpha - 1;
                    fail += x.Beta - 1;
                }
                result[kv.Key] = (succ + 1, fail + 1);
            }
            return result;
        }
    }
}
