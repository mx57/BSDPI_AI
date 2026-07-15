using FluxRoute.Core.Models;
using FluxRoute.AI.Stats;
using FluxRoute.AI.Models;

namespace FluxRoute.AI.Services;

public sealed class StrategyEvolver
{
    private static readonly string[] SemanticMarkers = ["host", "endhost", "midsld", "sniext", "endsld"];
    private static readonly string[] DesyncModes = ["split", "fake", "fakesplit", "disorder", "fakedisorder", "multidisorder", "multisplit"];
    private static readonly string[] FakeTlsMods = ["orig", "rand", "rndsni", "dupsid", "padencap"];
    private static readonly DpiEngineType[] EngineTypes = [DpiEngineType.Zapret, DpiEngineType.ByeDpi, DpiEngineType.Warp];
    private static readonly string[] SplitPosCandidates = ["1", "2", "3", "7", "10", "1+s", "2+s", "3+s", "host", "midsld", "sniext"];
    private static readonly string[] DisorderPosCandidates = ["1", "3", "5", "1+s", "3+s"];
    private static readonly string[] FakePosCandidates = ["-1", "3", "7", "10"];
    private static readonly string[] OobPosCandidates = ["1", "3", "7", "10", "3+s", "5+s"];
    private static readonly string[] DisoobPosCandidates = ["1", "3", "7", "10"];
    private static readonly string[] TlsrecPosCandidates = ["1", "3", "7", "1+s", "3+s"];
    private static readonly string[] ModHttpCandidates = ["hcsmix", "dcsmix", "rmspace", "hcsmix,dcsmix", "hcsmix,rmspace"];
    private static readonly string[] FoolingCandidates = ["md5sig", "badseq", "datanoack", "hopbyhop", "hopbyhop2", "badsum"];
    private static readonly string[] AnyProtocolCandidates = ["0", "1"];
    private static readonly string[] FakeResendCandidates = ["orig", "proxy", "null"];
    private static readonly int[] RepeatCountCandidates = [1, 2, 3, 5, 10];
    private static readonly string[] PsiphonCountries = ["AT", "AU", "BE", "BG", "CA", "CH", "CZ", "DE", "DK", "EE", "ES", "FI", "FR", "GB", "HR", "HU", "IE", "IN", "IT", "JP", "LV", "NL", "NO", "PL", "PT", "RO", "RS", "SE", "SG", "SK", "US"];

    private readonly AiStrategyRegistry _registry;
    private readonly AiHistoryStore _history;
    private readonly Random _rng;
    private readonly Func<string> _engineDir;
    private readonly Func<AiSettings> _aiSettings;

    public StrategyEvolver(
        AiStrategyRegistry registry,
        AiHistoryStore history,
        Func<string> engineDir,
        Func<AiSettings> aiSettings,
        Random? rng = null)
    {
        _registry = registry;
        _history = history;
        _engineDir = engineDir;
        _aiSettings = aiSettings;
        _rng = rng ?? new Random();
    }

    public StrategyGenome? Evolve(NetworkFingerprint net)
    {
        var pool = _registry.GetActiveGenomes().ToList();
        if (pool.Count < 2)
            return null;

        var outcomes = _history.LoadForNetwork(net.Hash);
        var byGenome = outcomes.GroupBy(o => o.GenomeId).ToDictionary(g => g.Key, g => g.ToList());

        var scored = pool
            .Select(g =>
            {
                byGenome.TryGetValue(g.Id, out var list);
                list ??= [];
                var succ = list.Count(o => o.Score >= 50);
                var trials = list.Count;
                var wilson = WilsonScore.LowerBound(succ, trials);
                return (g, wilson, trials);
            })
            .OrderByDescending(x => x.wilson)
            .ThenByDescending(x => x.trials)
            .ToList();

        var parents = scored.Take(Math.Min(6, scored.Count)).Select(x => x.g).ToList();
        if (parents.Count < 2)
            parents = pool.OrderBy(_ => _rng.Next()).Take(Math.Min(4, pool.Count)).ToList();

        if (parents.Count < 2)
            return null;

        StrategyGenome? child = null;
        for (var attempt = 0; attempt < 25; attempt++)
        {
            var p0 = parents[_rng.Next(parents.Count)];
            var p1 = parents[_rng.Next(parents.Count)];
            if (p0.Id == p1.Id && parents.Count > 1)
            {
                var idx = parents.IndexOf(p0);
                p1 = parents[(idx + 1) % parents.Count];
            }

            child = Crossover(p0, p1);
            Mutate(child);
            StrategyGenomeValidator.Normalize(child);

            if (!StrategyGenomeValidator.IsValid(child))
                continue;

            var sig = GenomeSignature.Compute(child);
            if (pool.Any(x => GenomeSignature.Compute(x) == sig))
                continue;

            break;
        }

        if (child is null || !StrategyGenomeValidator.IsValid(child))
            return null;

        _registry.GenerationCounter++;
        child.Generation = _registry.GenerationCounter;
        child.Origin = StrategyOrigin.Evolved;
        child.Id = Guid.NewGuid();
        child.CreatedAt = DateTimeOffset.UtcNow;

        var engineTag = child.EngineType == DpiEngineType.ByeDpi ? "byedpi" : "zapret";
        child.DisplayName = $"FR-ev-{child.Generation}-{engineTag}-{_rng.Next(1000, 9999)}";
        child.SourceBatPath = null;
        child.BatFileName = null;

        _registry.Upsert(child);
        GarbageCollectEvolved();
        _registry.Save();

        return child;
    }

    private void GarbageCollectEvolved()
    {
        var settings = _aiSettings();
        var settingsMax = Math.Max(4, settings.MaxEvolvedStrategies);
        var elitismCount = settings.ElitismEnabled ? Math.Max(2, settingsMax / 4) : 0;
        var evolved = _registry.GetGenomes().Where(g => g.Origin == StrategyOrigin.Evolved).ToList();
        if (evolved.Count <= settingsMax)
            return;

        var allOutcomes = _history.LoadAll();
        var byGenome = allOutcomes.GroupBy(o => o.GenomeId).ToDictionary(g => g.Key, g => g.ToList());

        var ranked = evolved
            .Select(g =>
            {
                byGenome.TryGetValue(g.Id, out var list);
                list ??= [];
                var succ = list.Count(o => o.Score >= 50);
                var trials = list.Count;
                var w = WilsonScore.LowerBound(succ, trials);
                return (g, w);
            })
            .OrderByDescending(x => x.w)
            .ToList();

        // Elitism: protect top N from deletion
        var protectedIds = ranked.Take(elitismCount).Select(x => x.g.Id).ToHashSet();
        var removable = ranked.Where(x => !protectedIds.Contains(x.g.Id)).ToList();

        var removeCount = evolved.Count - settingsMax;
        foreach (var (g, _) in removable.Take(removeCount))
        {
            TryDeleteBat(g);
            _registry.Remove(g.Id);
        }
    }

    private void TryDeleteBat(StrategyGenome g)
    {
        try
        {
            if (!string.IsNullOrEmpty(g.SourceBatPath) && File.Exists(g.SourceBatPath))
                File.Delete(g.SourceBatPath);
        }
        catch
        {
        }
    }

    private StrategyGenome Crossover(StrategyGenome a, StrategyGenome b)
    {
        return new StrategyGenome
        {
            EngineType = RngPick(a.EngineType, b.EngineType),
            FilterTcp = RngPick(a.FilterTcp, b.FilterTcp),
            FilterUdp = RngPick(a.FilterUdp, b.FilterUdp),
            DesyncMode = RngPick(a.DesyncMode, b.DesyncMode),
            SplitPos = RngPickNullableStruct(a.SplitPos, b.SplitPos),
            SplitPosSemantic = RngPickNullableRef(a.SplitPosSemantic, b.SplitPosSemantic),
            DisorderPos = RngPickNullableRef(a.DisorderPos, b.DisorderPos),
            FakePos = RngPickNullableRef(a.FakePos, b.FakePos),
            OobPos = RngPickNullableRef(a.OobPos, b.OobPos),
            DisoobPos = RngPickNullableRef(a.DisoobPos, b.DisoobPos),
            TlsrecPos = RngPickNullableRef(a.TlsrecPos, b.TlsrecPos),
            FakeTtl = RngPickNullableStruct(a.FakeTtl, b.FakeTtl),
            AutoTtl = RngPickBool(a.AutoTtl, b.AutoTtl),
            Md5sig = RngPickNullableStruct(a.Md5sig, b.Md5sig),
            FakeTlsMod = RngPickNullableRef(a.FakeTlsMod, b.FakeTlsMod),
            FakeSni = RngPickNullableRef(a.FakeSni, b.FakeSni),
            FakeData = RngPickNullableRef(a.FakeData, b.FakeData),
            ModHttp = RngPickNullableRef(a.ModHttp, b.ModHttp),
            Tlsminor = RngPickNullableStruct(a.Tlsminor, b.Tlsminor),
            Hosts = RngPickNullableRef(a.Hosts, b.Hosts),
            Hostlist = RngPickNullableRef(a.Hostlist, b.Hostlist),
            RepeatCount = RngPickNullableStruct(a.RepeatCount, b.RepeatCount),
            CacheTtl = RngPickNullableStruct(a.CacheTtl, b.CacheTtl),
            Auto = RngPickNullableRef(a.Auto, b.Auto),
            Timeout = RngPickNullableStruct(a.Timeout, b.Timeout),
            AutoMode = RngPickNullableStruct(a.AutoMode, b.AutoMode),
            DesyncAnyProtocol = RngPickNullableRef(a.DesyncAnyProtocol, b.DesyncAnyProtocol),
            DesyncFooling = RngPickNullableRef(a.DesyncFooling, b.DesyncFooling),
            FakeResend = RngPickNullableRef(a.FakeResend, b.FakeResend),
            WarpConfig = RngPickNullableRef(a.WarpConfig, b.WarpConfig),
            MTU = RngPickNullableStruct(a.MTU, b.MTU),
            GoolEnabled = RngPickBool(a.GoolEnabled, b.GoolEnabled),
            PsiphonEnabled = RngPickBool(a.PsiphonEnabled, b.PsiphonEnabled),
            PsiphonCountry = RngPickNullableRef(a.PsiphonCountry, b.PsiphonCountry),
            ScanEnabled = RngPickBool(a.ScanEnabled, b.ScanEnabled),
            Reserved = RngPickNullableRef(a.Reserved, b.Reserved),
            ExtraArgs = RngPickList(a.ExtraArgs, b.ExtraArgs),
            ParentIds = [a.Id, b.Id],
        };
    }

    private T RngPick<T>(T x, T y) where T : struct => _rng.Next(2) == 0 ? x : y;

    private string RngPick(string x, string y) => _rng.Next(2) == 0 ? x : y;

    private T? RngPickNullableStruct<T>(T? x, T? y) where T : struct =>
        _rng.Next(2) == 0 ? x : y;

    private string? RngPickNullableRef(string? x, string? y) =>
        _rng.Next(2) == 0 ? x : y;

    private bool RngPickBool(bool x, bool y) =>
        _rng.Next(2) == 0 ? x : y;

    private List<string> RngPickList(IReadOnlyList<string> a, IReadOnlyList<string> b)
    {
        if (a.Count == 0) return [.. b];
        if (b.Count == 0) return [.. a];
        return _rng.Next(2) == 0 ? [.. a] : [.. b];
    }

    private void Mutate(StrategyGenome g)
    {
        // BOLT ⚡: Mutation roll range expanded to cover new Zapret positions.
        var roll = _rng.Next(15);

        if (g.EngineType == DpiEngineType.ByeDpi)
        {
            MutateByeDpi(g, roll);
        }
        else if (g.EngineType == DpiEngineType.Warp)
        {
            MutateWarp(g, roll);
        }
        else
        {
            MutateZapret(g, roll);
        }
    }

    private void MutateZapret(StrategyGenome g, int roll)
    {
        switch (roll)
        {
            case 0:
                g.SplitPosSemantic = SemanticMarkers[_rng.Next(SemanticMarkers.Length)];
                g.SplitPos = null;
                break;
            case 1:
                g.DesyncMode = DesyncModes[_rng.Next(DesyncModes.Length)];
                break;
            case 2:
                if (g.FakeTtl is null)
                    g.FakeTtl = 6 + _rng.Next(10);
                else
                    g.FakeTtl = Math.Clamp(g.FakeTtl.Value + PickDelta(), 3, 48);
                break;
            case 3:
                g.FakeTlsMod = FakeTlsMods[_rng.Next(FakeTlsMods.Length)];
                break;
            case 4:
                g.AutoTtl = !g.AutoTtl;
                break;
            case 5:
                g.DesyncFooling = FoolingCandidates[_rng.Next(FoolingCandidates.Length)];
                break;
            case 6:
                g.DesyncAnyProtocol = AnyProtocolCandidates[_rng.Next(AnyProtocolCandidates.Length)];
                break;
            case 7:
                g.FakeResend = FakeResendCandidates[_rng.Next(FakeResendCandidates.Length)];
                break;
            case 8:
                g.RepeatCount = RepeatCountCandidates[_rng.Next(RepeatCountCandidates.Length)];
                break;
            case 9:
                g.DisorderPos = DisorderPosCandidates[_rng.Next(DisorderPosCandidates.Length)];
                break;
            case 10:
                g.FakePos = FakePosCandidates[_rng.Next(FakePosCandidates.Length)];
                break;
            case 11:
                g.OobPos = OobPosCandidates[_rng.Next(OobPosCandidates.Length)];
                break;
            case 12:
                g.DisoobPos = DisoobPosCandidates[_rng.Next(DisoobPosCandidates.Length)];
                break;
            case 13:
                g.TlsrecPos = TlsrecPosCandidates[_rng.Next(TlsrecPosCandidates.Length)];
                break;
            case 14:
                g.EngineType = DpiEngineType.ByeDpi;
                g.SplitPos = null;
                g.SplitPosSemantic = null;
                g.AutoTtl = false;
                g.DisorderPos = "1+s";
                break;
            default:
                if (g.SplitPosSemantic is not null)
                    g.SplitPos = _rng.Next(16, 180);
                g.SplitPosSemantic = null;
                break;
        }
    }

    private void MutateWarp(StrategyGenome g, int roll)
    {
        switch (roll)
        {
            case 0:
                g.EngineType = DpiEngineType.Zapret;
                break;
            case 1:
                g.EngineType = DpiEngineType.ByeDpi;
                break;
            case 2:
                g.MTU = (g.MTU ?? 1280) + (_rng.Next(3) - 1) * 20;
                g.MTU = Math.Clamp(g.MTU.Value, 1200, 1500);
                break;
            case 3:
                g.GoolEnabled = !g.GoolEnabled;
                break;
            case 4:
                g.PsiphonEnabled = !g.PsiphonEnabled;
                if (g.PsiphonEnabled) g.PsiphonCountry = PsiphonCountries[_rng.Next(PsiphonCountries.Length)];
                break;
            case 5:
                g.ScanEnabled = !g.ScanEnabled;
                break;
            case 6:
                g.Reserved = $"{_rng.Next(256)},{_rng.Next(256)},{_rng.Next(256)}";
                break;
            default:
                break;
        }
    }

    private void MutateByeDpi(StrategyGenome g, int roll)
    {
        switch (roll)
        {
            case 0:
                g.SplitPos = null;
                g.SplitPosSemantic = SplitPosCandidates[_rng.Next(SplitPosCandidates.Length)];
                break;
            case 1:
                g.DisorderPos = DisorderPosCandidates[_rng.Next(DisorderPosCandidates.Length)];
                break;
            case 2:
                g.FakePos = FakePosCandidates[_rng.Next(FakePosCandidates.Length)];
                if (g.FakeTtl is null)
                    g.FakeTtl = 5 + _rng.Next(8);
                break;
            case 3:
                g.TlsrecPos = TlsrecPosCandidates[_rng.Next(TlsrecPosCandidates.Length)];
                break;
            case 4:
                g.OobPos = OobPosCandidates[_rng.Next(OobPosCandidates.Length)];
                break;
            case 5:
                if (g.FakeTtl is null)
                    g.FakeTtl = 5 + _rng.Next(10);
                else
                    g.FakeTtl = Math.Clamp(g.FakeTtl.Value + PickDelta(), 3, 48);
                break;
            case 6:
                g.Md5sig = g.Md5sig is null ? true : !g.Md5sig;
                break;
            case 7:
                g.FakeTlsMod = FakeTlsMods[_rng.Next(FakeTlsMods.Length)];
                break;
            case 8:
                g.Auto = _rng.Next(2) == 0 ? "torst" : "ssl_err";
                g.Timeout = 3 + _rng.Next(4);
                break;
            case 9:
                g.ModHttp = ModHttpCandidates[_rng.Next(ModHttpCandidates.Length)];
                break;
            case 10:
                g.EngineType = DpiEngineType.Zapret;
                // BOLT ⚡: Preserving OobPos, DisoobPos, and TlsrecPos when switching to Zapret, as it supports them.
                g.DisorderPos = null;
                g.FakePos = null;
                g.Md5sig = null;
                g.FakeSni = null;
                g.FakeData = null;
                g.ModHttp = null;
                g.Tlsminor = null;
                g.Hosts = null;
                g.Auto = null;
                g.Timeout = null;
                g.AutoMode = null;
                g.CacheTtl = null;
                break;
            default:
                g.Tlsminor = _rng.Next(2, 4);
                break;
        }
    }

    private int PickDelta()
    {
        var d = new[] { 1, 2, 4, 8 };
        var v = d[_rng.Next(d.Length)];
        return _rng.Next(2) == 0 ? v : -v;
    }
}
