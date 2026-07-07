using System.Diagnostics;
using FluxRoute.Core.Models;

namespace FluxRoute.Core.Services;

public sealed class ZapretEngine : IDpiEngine
{
    public DpiEngineType EngineType => DpiEngineType.Zapret;
    public string DisplayName => "Zapret (winws.exe)";
    public EngineStatus Status { get; private set; } = EngineStatus.Stopped;
    public EngineProcessInfo? ProcessInfo { get; private set; }

    private Process? _process;
    private readonly string _engineDir;
    private readonly object _gate = new();
    private bool _disposed;

    public event EventHandler<EngineStatus>? StatusChanged;
    public event EventHandler<string>? MessageReceived;

    public ZapretEngine(string engineDir)
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
            ProfileBatLauncher.PrepareRuntime(_engineDir);

            var args = BuildWinwsArgs(profile);
            var binDir = Path.Combine(_engineDir, "bin");
            var executable = Path.Combine(binDir, "winws.exe");

            if (!File.Exists(executable))
            {
                lock (_gate)
                {
                    Status = EngineStatus.Failed;
                }
                NotifyStatus();
                return false;
            }

            var psi = new ProcessStartInfo
            {
                FileName = executable,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = binDir,
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
                    process.Id, "winws.exe", EngineStatus.Running,
                    DateTimeOffset.Now, null);
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

    public Task<bool> StopAsync(CancellationToken ct = default)
    {
        lock (_gate)
        {
            TryKillProcess(_process);
            _process = null;
            Status = EngineStatus.Stopped;
            ProcessInfo = null;
        }
        NotifyStatus();
        return Task.FromResult(true);
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

    public static IReadOnlyList<string> BuildWinwsArgs(EngineProfile p)
    {
        var list = new List<string>();
        if (!string.IsNullOrWhiteSpace(p.FilterTcp)) { list.Add("--filter-tcp"); list.Add(p.FilterTcp); }
        if (!string.IsNullOrWhiteSpace(p.FilterUdp)) { list.Add("--filter-udp"); list.Add(p.FilterUdp); }

        if (!string.IsNullOrWhiteSpace(p.DesyncMode))
        {
            list.Add("--dpi-desync");
            list.Add(p.DesyncMode);
        }

        if (!string.IsNullOrWhiteSpace(p.SplitPos))
        {
            list.Add("--dpi-desync-split-pos");
            list.Add(p.SplitPos);
        }

        if (!string.IsNullOrWhiteSpace(p.DisorderPos))
        {
            list.Add("--dpi-desync-disorder-pos");
            list.Add(p.DisorderPos);
        }

        if (!string.IsNullOrWhiteSpace(p.FakePos))
        {
            list.Add("--dpi-desync-fake-pos");
            list.Add(p.FakePos);
        }

        if (!string.IsNullOrWhiteSpace(p.OobPos))
        {
            list.Add("--dpi-desync-oob-pos");
            list.Add(p.OobPos);
        }

        if (!string.IsNullOrWhiteSpace(p.DisoobPos))
        {
            list.Add("--dpi-desync-disoob-pos");
            list.Add(p.DisoobPos);
        }

        if (!string.IsNullOrWhiteSpace(p.TlsrecPos))
        {
            list.Add("--dpi-desync-tlsrec-pos");
            list.Add(p.TlsrecPos);
        }

        if (!string.IsNullOrWhiteSpace(p.FakeTlsMod))
        {
            list.Add("--dpi-desync-fake-tls-mod");
            list.Add(p.FakeTlsMod);
        }

        if (p.FakeTtl is not null)
        {
            list.Add("--dpi-desync-ttl");
            list.Add(p.FakeTtl.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        if (p.AutoTtl)
            list.Add("--dpi-desync-autottl");

        if (p.RepeatCount is not null)
        {
            list.Add("--dpi-desync-repeats");
            list.Add(p.RepeatCount.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        if (!string.IsNullOrWhiteSpace(p.DesyncAnyProtocol))
        {
            list.Add("--dpi-desync-any-protocol");
            list.Add(p.DesyncAnyProtocol);
        }

        if (!string.IsNullOrWhiteSpace(p.DesyncFooling))
        {
            list.Add("--dpi-desync-fooling");
            list.Add(p.DesyncFooling);
        }

        if (!string.IsNullOrWhiteSpace(p.FakeResend))
        {
            list.Add("--dpi-desync-fake-resend");
            list.Add(p.FakeResend);
        }

        if (!string.IsNullOrWhiteSpace(p.Hostlist))
        {
            list.Add("--hostlist");
            list.Add(p.Hostlist);
        }

        foreach (var x in p.ExtraArgs)
        {
            if (!string.IsNullOrWhiteSpace(x))
                list.Add(x);
        }

        list.Add("--new");

        return list;
    }

    private void NotifyStatus()
    {
        StatusChanged?.Invoke(this, Status);
    }

    private static void TryKillProcess(Process? process)
    {
        if (process is null) return;
        try
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
            process.WaitForExit(2000);
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
        StopAsync().GetAwaiter().GetResult();
    }
}
