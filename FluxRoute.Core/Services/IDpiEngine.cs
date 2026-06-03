using FluxRoute.Core.Models;

namespace FluxRoute.Core.Services;

public enum EngineStatus
{
    Stopped,
    Starting,
    Running,
    Failed,
    Crashed
}

public sealed record EngineProcessInfo(
    int ProcessId,
    string ExecutableName,
    EngineStatus Status,
    DateTimeOffset StartedAt,
    int? SocksPort);

public interface IDpiEngine : IDisposable
{
    DpiEngineType EngineType { get; }
    string DisplayName { get; }
    EngineStatus Status { get; }
    EngineProcessInfo? ProcessInfo { get; }

    Task<bool> StartAsync(EngineProfile profile, CancellationToken ct = default);
    Task<bool> StopAsync(CancellationToken ct = default);
    Task<EngineStatus> ProbeStatusAsync(CancellationToken ct = default);

    event EventHandler<EngineStatus>? StatusChanged;
}

public sealed class DpiEngineRunMode
{
    public const string Standalone = "standalone";
    public const string Hybrid = "hybrid";
    public const string Bypass = "bypass";
}
