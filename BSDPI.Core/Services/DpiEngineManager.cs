using System.Collections.Concurrent;
using BSDPI.Core.Models;

namespace BSDPI.Core.Services;

public sealed class DpiRunMode
{
    public const string Standalone = "standalone";
    public const string Hybrid = "hybrid";
    public const string Warp = "warp";
    public const string WarpZapret = "warp_zapret";
    public const string WarpByeDpi = "warp_byedpi";
    public const string WarpZapretChained = "warp_zapret_chained";
    public const string WarpByeDpiChained = "warp_byedpi_chained";
    public const string Bypass = "bypass";
}

public sealed class DpiEngineManager : IDisposable
{
    private readonly string _engineDir;
    private readonly ConcurrentDictionary<DpiEngineType, IDpiEngine> _engines = new();
    private readonly ConcurrentDictionary<DpiEngineType, EngineProfile> _activeProfiles = new();
    private readonly object _gate = new();
    private string _runMode = DpiRunMode.Standalone;
    private bool _disposed;

    public IReadOnlyCollection<IDpiEngine> Engines => _engines.Values.ToList();
    public string RunMode => _runMode;

    public event EventHandler<(DpiEngineType Engine, EngineStatus Status)>? AnyEngineStatusChanged;
    public event EventHandler<(DpiEngineType Engine, string Message)>? AnyEngineMessageReceived;

    public DpiEngineManager(string engineDir)
    {
        _engineDir = engineDir;
    }

    public IDpiEngine GetOrCreate(DpiEngineType type)
    {
        return _engines.GetOrAdd(type, key =>
        {
            IDpiEngine engine = key switch
            {
                DpiEngineType.Zapret => new ZapretEngine(_engineDir),
                DpiEngineType.ByeDpi => new ByeDpiEngine(_engineDir),
                DpiEngineType.Warp => new WarpEngine(_engineDir),
                _ => throw new ArgumentOutOfRangeException(nameof(key), $"Unsupported engine: {key}")
            };
            engine.StatusChanged += (_, status) =>
                AnyEngineStatusChanged?.Invoke(this, (engine.EngineType, status));
            engine.MessageReceived += (_, msg) =>
                AnyEngineMessageReceived?.Invoke(this, (engine.EngineType, msg));
            return engine;
        });
    }

    public void SetRunMode(string mode)
    {
        lock (_gate)
        {
            _runMode = mode switch
            {
                DpiRunMode.Standalone or DpiRunMode.Hybrid or DpiRunMode.Warp or
                DpiRunMode.WarpZapret or DpiRunMode.WarpByeDpi or
                DpiRunMode.WarpZapretChained or DpiRunMode.WarpByeDpiChained or
                DpiRunMode.Bypass => mode,
                _ => DpiRunMode.Standalone,
            };
        }
    }

    public async Task<bool> StartAsync(DpiEngineType type, EngineProfile profile, CancellationToken ct = default)
    {
        var engine = GetOrCreate(type);
        _activeProfiles[type] = profile;
        return await engine.StartAsync(profile, ct).ConfigureAwait(false);
    }

    public async Task<bool> StopAsync(DpiEngineType type, CancellationToken ct = default)
    {
        if (_engines.TryGetValue(type, out var engine))
        {
            _activeProfiles.TryRemove(type, out _);
            return await engine.StopAsync(ct).ConfigureAwait(false);
        }
        return true;
    }

    public async Task<bool> StopAllAsync(CancellationToken ct = default)
    {
        var results = await Task.WhenAll(
            _engines.Values.Select(e => e.StopAsync(ct))
        ).ConfigureAwait(false);
        _activeProfiles.Clear();
        return results.All(r => r);
    }

    public async Task<bool> ApplyProfileAsync(EngineProfile profile, CancellationToken ct = default)
    {
        var mode = _runMode;
        var type = profile.EngineType;

        switch (mode)
        {
            case DpiRunMode.Standalone:
                await StopAllAsync(ct).ConfigureAwait(false);
                return await StartAsync(type, profile, ct).ConfigureAwait(false);

            case DpiRunMode.Hybrid:
                await StopAllAsync(ct).ConfigureAwait(false);
                var zapretOk = await StartAsync(DpiEngineType.Zapret,
                    profile.EngineType == DpiEngineType.Zapret ? profile : CloneWithDefaults(DpiEngineType.Zapret),
                    ct).ConfigureAwait(false);
                var byedpiOk = await StartAsync(DpiEngineType.ByeDpi,
                    profile.EngineType == DpiEngineType.ByeDpi ? profile : CloneWithDefaults(DpiEngineType.ByeDpi),
                    ct).ConfigureAwait(false);
                return zapretOk || byedpiOk;

            case DpiRunMode.Warp:
                await StopAllAsync(ct).ConfigureAwait(false);
                return await StartAsync(DpiEngineType.Warp,
                    profile.EngineType == DpiEngineType.Warp ? profile : CloneWithDefaults(DpiEngineType.Warp),
                    ct).ConfigureAwait(false);

            case DpiRunMode.WarpZapret:
                await StopAllAsync(ct).ConfigureAwait(false);
                var w1 = await StartAsync(DpiEngineType.Warp,
                    profile.EngineType == DpiEngineType.Warp ? profile : CloneWithDefaults(DpiEngineType.Warp),
                    ct).ConfigureAwait(false);
                var z1 = await StartAsync(DpiEngineType.Zapret,
                    profile.EngineType == DpiEngineType.Zapret ? profile : CloneWithDefaults(DpiEngineType.Zapret),
                    ct).ConfigureAwait(false);
                return w1 || z1;

            case DpiRunMode.WarpByeDpi:
                await StopAllAsync(ct).ConfigureAwait(false);
                var w2 = await StartAsync(DpiEngineType.Warp,
                    profile.EngineType == DpiEngineType.Warp ? profile : CloneWithDefaults(DpiEngineType.Warp),
                    ct).ConfigureAwait(false);
                var b2 = await StartAsync(DpiEngineType.ByeDpi,
                    profile.EngineType == DpiEngineType.ByeDpi ? profile : CloneWithDefaults(DpiEngineType.ByeDpi),
                    ct).ConfigureAwait(false);
                return w2 || b2;

            case DpiRunMode.WarpZapretChained:
                await StopAllAsync(ct).ConfigureAwait(false);
                var wc1 = await StartAsync(DpiEngineType.Warp,
                    profile.EngineType == DpiEngineType.Warp ? profile : CloneWithDefaults(DpiEngineType.Warp),
                    ct).ConfigureAwait(false);
                var zc1 = await StartAsync(DpiEngineType.Zapret,
                    ConfigureChained(profile.EngineType == DpiEngineType.Zapret ? CloneProfile(profile) : CloneWithDefaults(DpiEngineType.Zapret), 8086),
                    ct).ConfigureAwait(false);
                return wc1 && zc1;

            case DpiRunMode.WarpByeDpiChained:
                await StopAllAsync(ct).ConfigureAwait(false);
                var wc2 = await StartAsync(DpiEngineType.Warp,
                    profile.EngineType == DpiEngineType.Warp ? profile : CloneWithDefaults(DpiEngineType.Warp),
                    ct).ConfigureAwait(false);
                var bc2 = await StartAsync(DpiEngineType.ByeDpi,
                    ConfigureChained(profile.EngineType == DpiEngineType.ByeDpi ? CloneProfile(profile) : CloneWithDefaults(DpiEngineType.ByeDpi), 8086),
                    ct).ConfigureAwait(false);
                return wc2 && bc2;

            case DpiRunMode.Bypass:
                await StopAllAsync(ct).ConfigureAwait(false);
                return true;

            default:
                return false;
        }
    }

    public EngineProfile? GetActiveProfile(DpiEngineType type)
    {
        _activeProfiles.TryGetValue(type, out var profile);
        return profile;
    }

    private EngineProfile ConfigureChained(EngineProfile p, int upstreamPort)
    {
        if (p.EngineType == DpiEngineType.Zapret)
        {
            if (!p.ExtraArgs.Any(x => x == "--socks5"))
            {
                p.ExtraArgs.Add("--socks5");
                p.ExtraArgs.Add($"127.0.0.1:{upstreamPort}");
            }
        }
        else if (p.EngineType == DpiEngineType.ByeDpi)
        {
            if (!p.ExtraArgs.Any(x => x == "--socks"))
            {
                p.ExtraArgs.Add("--socks");
                p.ExtraArgs.Add($"127.0.0.1:{upstreamPort}");
            }
        }
        return p;
    }

    private static EngineProfile CloneProfile(EngineProfile p) => new()
    {
        EngineType = p.EngineType,
        SocksPort = p.SocksPort,
        FilterTcp = p.FilterTcp,
        FilterUdp = p.FilterUdp,
        DesyncMode = p.DesyncMode,
        SplitPos = p.SplitPos,
        DisorderPos = p.DisorderPos,
        FakePos = p.FakePos,
        OobPos = p.OobPos,
        DisoobPos = p.DisoobPos,
        TlsrecPos = p.TlsrecPos,
        FakeTtl = p.FakeTtl,
        FakeTlsMod = p.FakeTlsMod,
        FakeSni = p.FakeSni,
        FakeData = p.FakeData,
        ModHttp = p.ModHttp,
        Tlsminor = p.Tlsminor,
        Auto = p.Auto,
        Timeout = p.Timeout,
        AutoMode = p.AutoMode,
        Hosts = p.Hosts,
        Hostlist = p.Hostlist,
        Md5sig = p.Md5sig,
        RepeatCount = p.RepeatCount,
        CacheTtl = p.CacheTtl,
        ExtraArgs = [.. p.ExtraArgs],
        WarpConfig = p.WarpConfig,
        MTU = p.MTU,
        GoolEnabled = p.GoolEnabled,
        PsiphonEnabled = p.PsiphonEnabled,
        PsiphonCountry = p.PsiphonCountry,
        ScanEnabled = p.ScanEnabled,
        Reserved = p.Reserved,
    };

    public EngineProfile CloneWithDefaults(DpiEngineType type)
    {
        return type switch
        {
            DpiEngineType.Zapret => new EngineProfile
            {
                EngineType = DpiEngineType.Zapret,
                DesyncMode = "split",
                SplitPos = "2",
                FilterTcp = "80,443",
            },
            DpiEngineType.ByeDpi => new EngineProfile
            {
                EngineType = DpiEngineType.ByeDpi,
                SocksPort = 1080,
                DisorderPos = "1",
                SplitPos = "1+s",
                Auto = "torst",
                Timeout = 3,
            },
            DpiEngineType.Warp => new EngineProfile
            {
                EngineType = DpiEngineType.Warp,
                SocksPort = 8086
            },
            _ => new EngineProfile { EngineType = type },
        };
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        foreach (var engine in _engines.Values)
            engine.Dispose();
        _engines.Clear();
    }
}
