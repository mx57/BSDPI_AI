using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Application = System.Windows.Application;

using FluxRoute.Updater.Services;
using FluxRoute.Views;

namespace FluxRoute.ViewModels;

public sealed partial class UpdatesViewModel : ObservableObject
{
    private readonly IUpdaterService _updater;
    private readonly IAppUpdaterService _appUpdater;
    private readonly IByeDpiUpdaterService _byeDpiUpdater;
    private readonly IWarpUpdaterService _warpUpdater;
    private readonly Func<string> _getEngineDir;
    private readonly Func<bool> _getAutoUpdateEnabled;
    private readonly Func<string> _getCurrentEngineVersion;
    private readonly Action<string> _setCurrentEngineVersion;
    private readonly Action _stopEngine;
    private readonly Action _loadProfiles;
    private readonly Action _refreshDiagnostics;
    private readonly Action<string> _addAppLog;
    private readonly Action<string> _addRecentLog;

    private UpdateInfo? _pendingUpdate;
    private AppUpdateInfo? _pendingAppUpdate;

    public ObservableCollection<string> UpdateLogs { get; } = new();

    [ObservableProperty] private string updateStatus = "Не проверялось";
    [ObservableProperty] private bool isUpdating;
    [ObservableProperty] private bool isDownloadingEngine;
    [ObservableProperty] private string engineDownloadStatus = "";
    [ObservableProperty] private string currentEngineVersion = "—";
    [ObservableProperty] private string latestRemoteVersion = "—";
    [ObservableProperty] private string releaseNotes = "";

    // ── Обновление самого приложения ──────────────────────────────────────
    [ObservableProperty] private string currentAppVersion = "—";
    [ObservableProperty] private string latestAppVersion = "—";
    [ObservableProperty] private string appUpdateStatus = "Не проверялось";
    [ObservableProperty] private bool isCheckingAppUpdate;
    [ObservableProperty] private bool hasAppUpdate;

    // ── ByeDPI ────────────────────────────────────────────────────────────
    [ObservableProperty] private string byeDpiVersion = "—";
    [ObservableProperty] private string byeDpiLatestVersion = "—";
    [ObservableProperty] private string byeDpiUpdateStatus = "Не проверялось";
    [ObservableProperty] private bool isByeDpiUpdating;
    private UpdateInfo? _pendingByeDpiUpdate;

    // ── Warp ──────────────────────────────────────────────────────────────
    [ObservableProperty] private string warpVersion = "—";
    [ObservableProperty] private string warpLatestVersion = "—";
    [ObservableProperty] private string warpUpdateStatus = "Не проверялось";
    [ObservableProperty] private bool isWarpUpdating;
    private UpdateInfo? _pendingWarpUpdate;

    public UpdatesViewModel(
        IUpdaterService updater,
        IAppUpdaterService appUpdater,
        IByeDpiUpdaterService byeDpiUpdater,
        IWarpUpdaterService warpUpdater,
        Func<string> getEngineDir,
        Func<bool> getAutoUpdateEnabled,
        Func<string> getCurrentEngineVersion,
        Action<string> setCurrentEngineVersion,
        Action stopEngine,
        Action loadProfiles,
        Action refreshDiagnostics,
        Action<string> addAppLog,
        Action<string> addRecentLog)
    {
        _updater = updater;
        _appUpdater = appUpdater;
        _byeDpiUpdater = byeDpiUpdater;
        _warpUpdater = warpUpdater;
        _getEngineDir = getEngineDir;
        _getAutoUpdateEnabled = getAutoUpdateEnabled;
        _getCurrentEngineVersion = getCurrentEngineVersion;
        _setCurrentEngineVersion = setCurrentEngineVersion;
        _stopEngine = stopEngine;
        _loadProfiles = loadProfiles;
        _refreshDiagnostics = refreshDiagnostics;
        _addAppLog = addAppLog;
        _addRecentLog = addRecentLog;

        CurrentAppVersion = _appUpdater.GetCurrentVersion();
        RefreshByeDpiVersion();
        RefreshWarpVersion();
    }

    private string EngineDir => _getEngineDir();
    private string ByeDpiDir => Path.Combine(EngineDir, "byedpi");
    private string WarpDir => Path.Combine(EngineDir, "warp");

    private void AddLog(string message)
    {
        UpdateLogs.Add(message);
        while (UpdateLogs.Count > 200)
            UpdateLogs.RemoveAt(0);
    }

    private void RefreshByeDpiVersion()
    {
        ByeDpiVersion = _byeDpiUpdater.GetLocalVersion(ByeDpiDir);
    }

    private void RefreshWarpVersion()
    {
        WarpVersion = _warpUpdater.GetLocalVersion(WarpDir);
    }

    public async Task CheckOnStartupAsync()
    {
        if (!_getAutoUpdateEnabled()) return;

        var (update, _) = await _updater.CheckForUpdateAsync(EngineDir);
        if (update is null) return;

        _pendingUpdate = update;
        if (Application.Current != null && !Application.Current.Dispatcher.HasShutdownStarted)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                UpdateStatus = $"Доступна новая версия: {update.Version}";

                if (CustomDialog.Show(
                    "⬆️ Обновление доступно",
                    $"Доступно обновление Flowseal zapret!\n\nВерсия: {update.Version}\n\nОбновить сейчас?",
                    "Обновить", "Позже"))
                    _ = InstallUpdateAsync();
            });
        }
    }

    [RelayCommand]
    private async Task CheckUpdates()
    {
        UpdateStatus = "🔍 Проверяем обновления...";
        LatestRemoteVersion = "…";

        var (latest, latestError) = await _updater.GetLatestReleaseAsync();
        if (latest is not null)
        {
            LatestRemoteVersion = latest.Version;
            ReleaseNotes = latest.ReleaseNotes;
        }
        else
        {
            LatestRemoteVersion = "—";
        }

        var (update, error) = await _updater.CheckForUpdateAsync(EngineDir);

        if (update is null)
        {
            if (error is not null)
            {
                UpdateStatus = $"❌ {error}";
                _addAppLog($"Ошибка проверки: {error}");
                AddLog($"❌ {error}");
            }
            else
            {
                UpdateStatus = $"✅ Актуальная версия ({_getCurrentEngineVersion()})";
                _addAppLog("Обновлений не найдено.");
                AddLog("✅ Обновлений не найдено");
            }
            return;
        }

        _pendingUpdate = update;
        UpdateStatus = $"⬆️ Доступна версия {update.Version}";
        _addAppLog($"Доступно обновление: {update.Version}");
        AddLog($"⬆️ Доступно: {update.Version}");
    }

    [RelayCommand]
    private async Task InstallUpdates()
    {
        if (_pendingUpdate is null)
        {
            await CheckUpdates();
            if (_pendingUpdate is null)
            {
                UpdateStatus = "🔄 Принудительная проверка...";
                var (latest, forceError) = await _updater.GetLatestReleaseAsync();
                if (latest is null)
                {
                    var errMsg = forceError ?? "Неизвестная ошибка";
                    UpdateStatus = $"❌ {errMsg}";
                    AddLog($"❌ {errMsg}");
                    return;
                }

                if (!CustomDialog.Show(
                    "🔄 Принудительное обновление",
                    $"Локальная версия совпадает с последней ({latest.Version}).\n\nПринудительно переустановить Flowseal?\nЭто скачает и заменит все файлы engine/.",
                    "Переустановить", "Отмена", isDanger: true))
                {
                    UpdateStatus = $"✅ Актуальная версия ({_getCurrentEngineVersion()})";
                    return;
                }

                _pendingUpdate = latest;
                AddLog($"🔄 Принудительная переустановка {latest.Version}...");
            }
        }
        await InstallUpdateAsync();
    }

    internal async Task InstallUpdateAsync()
    {
        if (_pendingUpdate is null) return;
        IsUpdating = true;
        _stopEngine();

        var success = await _updater.InstallUpdateAsync(EngineDir, _pendingUpdate,
            msg =>
            {
                if (Application.Current != null && !Application.Current.Dispatcher.HasShutdownStarted)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        UpdateStatus = msg;
                        _addAppLog(msg);
                        AddLog(msg);
                    });
                }
            });

        if (success)
        {
            _setCurrentEngineVersion(_updater.GetLocalVersion(EngineDir));
            _pendingUpdate = null;
            _loadProfiles();
        }
        else
        {
            AddLog("⚠️ Нажмите «Обновить» для повторной попытки");
        }

        IsUpdating = false;
    }

    // ── Команды обновления приложения ────────────────────────────────────────

    [RelayCommand]
    private async Task CheckAppUpdate()
    {
        IsCheckingAppUpdate = true;
        AppUpdateStatus = "🔍 Проверяем обновление FluxRoute...";
        HasAppUpdate = false;
        _pendingAppUpdate = null;

        var (update, error) = await _appUpdater.CheckForAppUpdateAsync();

        if (error is not null)
        {
            AppUpdateStatus = $"❌ {error}";
            AddLog($"❌ FluxRoute: {error}");
        }
        else if (update is null)
        {
            AppUpdateStatus = $"✅ Актуальная версия FluxRoute ({CurrentAppVersion})";
            LatestAppVersion = CurrentAppVersion;
            AddLog("✅ FluxRoute: обновлений нет");
        }
        else
        {
            _pendingAppUpdate = update;
            HasAppUpdate = true;
            LatestAppVersion = update.Version;
            AppUpdateStatus = $"⬆️ Доступна FluxRoute v{update.Version}";
            AddLog($"⬆️ FluxRoute: доступна версия {update.Version}");
        }

        IsCheckingAppUpdate = false;
    }

    [RelayCommand]
    private async Task InstallAppUpdate()
    {
        if (_pendingAppUpdate is null)
        {
            await CheckAppUpdate();
            if (_pendingAppUpdate is null) return;
        }

        var update = _pendingAppUpdate;

        if (!CustomDialog.Show(
            "⬆️ Обновление FluxRoute",
            $"Будет установлена FluxRoute v{update.Version}.\n\n" +
            $"Приложение автоматически перезапустится после установки.\n\n" +
            $"Установить сейчас?",
            "Установить", "Отмена"))
            return;

        AppUpdateStatus = "⬇️ Загружаем...";
        _addAppLog($"Установка FluxRoute v{update.Version}...");

        var (success, error) = await _appUpdater.DownloadAndApplyAsync(
            update,
            msg =>
            {
                if (Application.Current != null && !Application.Current.Dispatcher.HasShutdownStarted)
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        AppUpdateStatus = msg;
                        AddLog(msg);
                        _addAppLog(msg);
                    });
            });

        if (success)
        {
            AddLog($"✅ FluxRoute v{update.Version} установлен, перезапуск...");
            Application.Current?.Dispatcher.Invoke(() => Application.Current.Shutdown());
        }
        else
        {
            AppUpdateStatus = $"❌ {error}";
            AddLog($"❌ {error}");
        }
    }

    // ── ByeDPI ──────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task CheckByeDpiUpdate()
    {
        ByeDpiUpdateStatus = "🔍 Проверка ByeDPI...";

        var (latest, error) = await _byeDpiUpdater.GetLatestReleaseAsync();
        if (latest is not null)
            ByeDpiLatestVersion = latest.Version;
        else
            ByeDpiLatestVersion = "—";

        var (update, checkError) = await _byeDpiUpdater.CheckForUpdateAsync(ByeDpiDir);

        if (update is null)
        {
            if (checkError is not null)
            {
                ByeDpiUpdateStatus = $"❌ {checkError}";
                AddLog($"❌ ByeDPI: {checkError}");
            }
            else
            {
                ByeDpiUpdateStatus = $"✅ Актуальная версия ({ByeDpiVersion})";
                AddLog("✅ ByeDPI: обновлений нет");
            }
            return;
        }

        _pendingByeDpiUpdate = update;
        ByeDpiUpdateStatus = $"⬆️ Доступна версия {update.Version}";
        AddLog($"⬆️ ByeDPI: {update.Version}");
    }

    [RelayCommand]
    private async Task InstallByeDpiUpdate()
    {
        if (_pendingByeDpiUpdate is null)
        {
            await CheckByeDpiUpdate();
            if (_pendingByeDpiUpdate is null) return;
        }

        IsByeDpiUpdating = true;
        var success = await _byeDpiUpdater.InstallUpdateAsync(ByeDpiDir, _pendingByeDpiUpdate,
            msg =>
            {
                if (Application.Current != null && !Application.Current.Dispatcher.HasShutdownStarted)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        ByeDpiUpdateStatus = msg;
                        AddLog(msg);
                        _addAppLog(msg);
                    });
                }
            });

        if (success)
        {
            RefreshByeDpiVersion();
            _pendingByeDpiUpdate = null;
            ByeDpiUpdateStatus = $"✅ ByeDPI {ByeDpiVersion}";
        }

        IsByeDpiUpdating = false;
    }

    public async Task AutoDownloadEngineAsync()
    {
        IsDownloadingEngine = true;
        EngineDownloadStatus = "🔍 Поиск последней версии Flowseal...";

        try
        {
            var (update, error) = await _updater.GetLatestReleaseAsync();
            if (update is null)
            {
                if (Application.Current != null && !Application.Current.Dispatcher.HasShutdownStarted)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        EngineDownloadStatus = $"❌ {error ?? "Не удалось получить информацию о релизе"}";
                        _addAppLog($"❌ Flowseal: {error ?? "неизвестная ошибка"}");
                        IsDownloadingEngine = false;
                    });
                }
                return;
            }

            var confirmed = false;
            if (Application.Current != null && !Application.Current.Dispatcher.HasShutdownStarted)
            {
                confirmed = Application.Current.Dispatcher.Invoke(() =>
                    CustomDialog.Show(
                        "⬇️ Скачивание Flowseal",
                        $"Для работы FluxRoute необходим движок Flowseal (v{update.Version}).\n\n" +
                        $"Источник: официальный GitHub-репозиторий\n" +
                        $"github.com/Flowseal/zapret-discord-youtube\n\n" +
                        $"Ссылка на скачивание:\n{update.DownloadUrl}\n\n" +
                        $"Это open-source проект — исходный код доступен публично.\n" +
                        $"После скачивания SHA-256 хеш будет отображён в логах.",
                        "Скачать", "Отмена"));
            }

            if (!confirmed)
            {
                if (Application.Current != null && !Application.Current.Dispatcher.HasShutdownStarted)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        EngineDownloadStatus = "⏹ Скачивание отменено пользователем";
                        _addAppLog("⏹ Пользователь отменил скачивание Flowseal");
                        IsDownloadingEngine = false;
                    });
                }
                return;
            }

            var success = await _updater.InstallUpdateAsync(EngineDir, update,
                msg =>
                {
                    if (Application.Current != null && !Application.Current.Dispatcher.HasShutdownStarted)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            EngineDownloadStatus = msg;
                            _addAppLog(msg);
                        });
                    }
                });

            if (Application.Current != null && !Application.Current.Dispatcher.HasShutdownStarted)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (success)
                    {
                        _setCurrentEngineVersion(_updater.GetLocalVersion(EngineDir));
                        EngineDownloadStatus = $"✅ Flowseal {update.Version} установлен!";
                        _addAppLog($"✅ Flowseal {update.Version} установлен автоматически");
                        _addRecentLog($"✅ Flowseal {update.Version} установлен");
                        _loadProfiles();
                        _refreshDiagnostics();
                    }
                    else
                    {
                        _addAppLog("❌ Установка Flowseal не завершена");
                        _addRecentLog("❌ Ошибка установки Flowseal");
                    }
                    IsDownloadingEngine = false;
                });
            }
        }
        catch (Exception ex)
        {
            if (Application.Current != null && !Application.Current.Dispatcher.HasShutdownStarted)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    EngineDownloadStatus = $"❌ Ошибка: {ex.Message}";
                    _addAppLog($"❌ Автоскачивание Flowseal: {ex.Message}");
                    IsDownloadingEngine = false;
                });
            }
        }
    }

    [RelayCommand]
    private async Task CheckWarpUpdate()
    {
        WarpUpdateStatus = "🔍 Проверяем Warp...";
        WarpLatestVersion = "…";

        var (update, checkError) = await _warpUpdater.CheckForUpdateAsync(WarpDir);

        if (update is null)
        {
            if (checkError is not null)
            {
                WarpUpdateStatus = $"❌ {checkError}";
                AddLog($"❌ Warp: {checkError}");
            }
            else
            {
                WarpUpdateStatus = $"✅ Актуальная версия ({WarpVersion})";
                AddLog("✅ Warp: обновлений нет");
            }
            return;
        }

        _pendingWarpUpdate = update;
        WarpLatestVersion = update.Version;
        WarpUpdateStatus = $"⬆️ Доступна версия {update.Version}";
        AddLog($"⬆️ Warp: {update.Version}");
    }

    [RelayCommand]
    private async Task InstallWarpUpdate()
    {
        if (_pendingWarpUpdate is null)
        {
            await CheckWarpUpdate();
            if (_pendingWarpUpdate is null) return;
        }

        IsWarpUpdating = true;
        var success = await _warpUpdater.InstallUpdateAsync(WarpDir, _pendingWarpUpdate,
            msg =>
            {
                if (Application.Current != null && !Application.Current.Dispatcher.HasShutdownStarted)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        WarpUpdateStatus = msg;
                        AddLog(msg);
                        _addAppLog(msg);
                    });
                }
            });

        if (success)
        {
            RefreshWarpVersion();
            _pendingWarpUpdate = null;
            WarpUpdateStatus = $"✅ Warp {WarpVersion}";
        }

        IsWarpUpdating = false;
    }
}
