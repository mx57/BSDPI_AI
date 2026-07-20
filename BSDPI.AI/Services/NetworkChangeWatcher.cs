using System.Net.NetworkInformation;
using BSDPI.AI.Models;

namespace BSDPI.AI.Services;

public sealed class NetworkChangeWatcher : IDisposable
{
    private readonly NetworkFingerprintProvider _fingerprints;
    private readonly TimeSpan _debounce = TimeSpan.FromSeconds(3);
    private readonly object _gate = new();

    private Timer? _debounceTimer;
    private NetworkFingerprint? _lastEmitted;

    public event EventHandler<(NetworkFingerprint OldFp, NetworkFingerprint NewFp)>? NetworkChanged;

    public NetworkChangeWatcher(NetworkFingerprintProvider fingerprints)
    {
        _fingerprints = fingerprints;

        // Подписка на события NetworkChange может бросить NetworkInformationException
        // в ограниченных средах (контейнеры, песочницы, некоторые ОС без прав на
        // enum сетевых интерфейсов). Не должна ронять весь оркестратор — отслеживание
        // смены сети просто отключается, базовая функциональность работает.
        try
        {
            NetworkChange.NetworkAddressChanged += OnNetworkChange;
            NetworkChange.NetworkAvailabilityChanged += OnNetworkAvailability;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
        {
            System.Diagnostics.Trace.TraceWarning($"NetworkChangeWatcher: не удалось подписаться на события сети: {ex.Message}");
        }

        _lastEmitted = _fingerprints.Capture();
    }

    private void OnNetworkChange(object? sender, EventArgs e) => ScheduleEmit();

    private void OnNetworkAvailability(object? sender, NetworkAvailabilityEventArgs e) => ScheduleEmit();

    private void ScheduleEmit()
    {
        lock (_gate)
        {
            _debounceTimer?.Dispose();
            _debounceTimer = new Timer(_ =>
            {
                try
                {
                    var next = _fingerprints.Capture();
                    NetworkFingerprint? oldSnap;
                    lock (_gate)
                    {
                        oldSnap = _lastEmitted;
                        if (oldSnap?.Hash == next.Hash)
                            return;
                        _lastEmitted = next;
                    }

                    if (oldSnap is not null)
                        NetworkChanged?.Invoke(this, (oldSnap, next));
                }
                catch
                {
                }
            }, null, _debounce, Timeout.InfiniteTimeSpan);
        }
    }

    public NetworkFingerprint GetLastFingerprint()
    {
        lock (_gate)
        {
            return _lastEmitted ?? _fingerprints.Capture();
        }
    }

    public void Dispose()
    {
        NetworkChange.NetworkAddressChanged -= OnNetworkChange;
        NetworkChange.NetworkAvailabilityChanged -= OnNetworkAvailability;
        lock (_gate)
        {
            _debounceTimer?.Dispose();
            _debounceTimer = null;
        }
    }
}
