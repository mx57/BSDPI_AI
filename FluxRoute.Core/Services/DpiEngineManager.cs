using System.Collections.Concurrent;
using FluxRoute.Core.Models;

namespace FluxRoute.Core.Services;

public sealed class DpiRunMode
{
    public const string Standalone = "standalone";
    public const string Hybrid = "hybrid";
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
                _ => throw new ArgumentOutOfRangeException(nameof(key), $"Unsupported engine: {key}")
            };
            engine.StatusChanged += (_, status) =>
                AnyEngineStatusChanged?.Invoke(this, (engine.EngineType, status));
            return engine;
        });
    }

    public void SetRunMode(string mode)
    {
        lock (_gate)
        {
            _runMode = mode switch
            {
                DpiRunMode.Standalone or DpiRunMode.Hybrid or DpiRunMode.Bypass => mode,
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
                await StopAsync(DpiEngineType.GoodbyeDpi, ct).ConfigureAwait(false);
                var zapretOk = await StartAsync(DpiEngineType.Zapret,
                    profile.EngineType == DpiEngineType.Zapret ? profile : CloneWithDefaults(DpiEngineType.Zapret),
                    ct).ConfigureAwait(false);
                var byedpiOk = await StartAsync(DpiEngineType.ByeDpi,
                    profile.EngineType == DpiEngineType.ByeDpi ? profile : CloneWithDefaults(DpiEngineType.ByeDpi),
                    ct).ConfigureAwait(false);
                return zapretOk || byedpiOk;

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
