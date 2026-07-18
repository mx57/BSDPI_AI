using System.Diagnostics;
using BSDPI.Core.Models;

namespace BSDPI.Core.Services;

public sealed class WarpEngine : IDpiEngine
{
    public DpiEngineType EngineType => DpiEngineType.Warp;
    public string DisplayName => "Warp (warp-plus)";
    public EngineStatus Status { get; private set; } = EngineStatus.Stopped;
    public EngineProcessInfo? ProcessInfo { get; private set; }

    private Process? _process;
    private readonly string _engineDir;
    private readonly object _gate = new();
    private bool _disposed;

    public event EventHandler<EngineStatus>? StatusChanged;
    public event EventHandler<string>? MessageReceived;

    public WarpEngine(string engineDir)
    {
        _engineDir = engineDir;
    }

    public async Task<bool> StartAsync(EngineProfile profile, CancellationToken ct = default)
    {
        lock (_gate)
        {
            if (Status == EngineStatus.Running || Status == EngineStatus.Starting)
                return true;
            Status = EngineStatus.Starting;
        }

        try
        {
            var executable = FindWarpExecutable();
            if (executable is null)
            {
                lock (_gate)
                {
                    Status = EngineStatus.Failed;
                }
                NotifyStatus();
                return false;
            }

            string? configPath = null;
            if (!string.IsNullOrWhiteSpace(profile.WarpConfig))
            {
                var warpDir = Path.GetDirectoryName(executable) ?? _engineDir;
                configPath = Path.Combine(warpDir, "warp.conf");
                await File.WriteAllTextAsync(configPath, profile.WarpConfig, new System.Text.UTF8Encoding(false), ct).ConfigureAwait(false);
            }

            var args = BuildWarpArgs(profile, configPath);

            var psi = new ProcessStartInfo
            {
                FileName = executable,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = Path.GetDirectoryName(executable) ?? _engineDir,
            };

            foreach (var arg in args)
                psi.ArgumentList.Add(arg);

            var process = new Process { StartInfo = psi };
            process.Exited += (_, _) =>
            {
                lock (_gate)
                {
                    if (_process != process) return;
                    Status = EngineStatus.Crashed;
                    _process = null;
                    ProcessInfo = null;
                }
                NotifyStatus();
            };
            process.OutputDataReceived += (_, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    MessageReceived?.Invoke(this, e.Data);
            };
            process.ErrorDataReceived += (_, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    MessageReceived?.Invoke(this, e.Data);
            };
            process.EnableRaisingEvents = true;

            if (!process.Start())
            {
                process.Dispose();
                lock (_gate)
                {
                    Status = EngineStatus.Failed;
                }
                NotifyStatus();
                return false;
            }

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            lock (_gate)
            {
                _process = process;
                ProcessInfo = new EngineProcessInfo(
                    process.Id, "warp-plus.exe", EngineStatus.Running,
                    DateTimeOffset.Now, profile.SocksPort);
                Status = EngineStatus.Running;
            }
            NotifyStatus();
            return true;
        }
        catch
        {
            lock (_gate)
            {
                Status = EngineStatus.Failed;
            }
            NotifyStatus();
            return false;
        }
    }

    public async Task<bool> StopAsync(CancellationToken ct = default)
    {
        Process? processToKill;
        lock (_gate)
        {
            processToKill = _process;
            _process = null;
            Status = EngineStatus.Stopped;
            ProcessInfo = null;
        }

        if (processToKill is not null)
        {
            await TryKillProcessAsync(processToKill, ct).ConfigureAwait(false);
        }

        NotifyStatus();
        return true;
    }

    public Task<EngineStatus> ProbeStatusAsync(CancellationToken ct = default)
    {
        lock (_gate)
        {
            if (_process is not null && _process.HasExited)
            {
                Status = EngineStatus.Crashed;
                _process = null;
                ProcessInfo = null;
            }
            return Task.FromResult(Status);
        }
    }

    private string? FindWarpExecutable()
    {
        var candidates = new[]
        {
            Path.Combine(_engineDir, "warp", "warp-plus.exe"),
            Path.Combine(_engineDir, "warp-plus.exe"),
        };

        return candidates.FirstOrDefault(File.Exists);
    }

    private static IReadOnlyList<string> BuildWarpArgs(EngineProfile p, string? configPath)
    {
        var list = new List<string>();

        // Basic flags for warp-plus
        list.Add("-b");
        list.Add($"127.0.0.1:{p.SocksPort}");

        if (configPath != null)
        {
            list.Add("--wgconf");
            list.Add(configPath);
        }

        if (p.MTU.HasValue)
        {
            list.Add("--mtu");
            list.Add(p.MTU.Value.ToString());
        }

        if (p.GoolEnabled)
        {
            list.Add("--gool");
        }

        if (p.PsiphonEnabled)
        {
            list.Add("--cfon");
            if (!string.IsNullOrWhiteSpace(p.PsiphonCountry))
            {
                list.Add("--country");
                list.Add(p.PsiphonCountry);
            }
        }

        if (p.ScanEnabled)
        {
            list.Add("--scan");
        }

        if (!string.IsNullOrWhiteSpace(p.Reserved))
        {
            list.Add("--reserved");
            list.Add(p.Reserved);
        }

        foreach (var x in p.ExtraArgs)
        {
            if (!string.IsNullOrWhiteSpace(x))
                list.Add(x);
        }

        return list;
    }

    private void NotifyStatus()
    {
        StatusChanged?.Invoke(this, Status);
    }

    private static async Task TryKillProcessAsync(Process process, CancellationToken ct)
    {
        try
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(2));
            try
            {
                await process.WaitForExitAsync(cts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
            process.Dispose();
        }
        catch
        {
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        lock (_gate)
        {
            if (_process is not null)
            {
                try
                {
                    if (!_process.HasExited)
                        _process.Kill(entireProcessTree: true);
                    _process.Dispose();
                }
                catch
                {
                }
                _process = null;
            }
            Status = EngineStatus.Stopped;
            ProcessInfo = null;
        }
    }
}
