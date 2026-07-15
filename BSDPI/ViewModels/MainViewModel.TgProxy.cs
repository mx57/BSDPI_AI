using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Security.Cryptography;
using CommunityToolkit.Mvvm.Input;
using Application = System.Windows.Application;
using BSDPI.Views;

namespace BSDPI.ViewModels;

public partial class MainViewModel
{
    // ── Пути ──
    private string TgProxyDir => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tg-proxy");
    private string PythonDir => Path.Combine(TgProxyDir, "python");
    private string PythonExe => Path.Combine(PythonDir, "python.exe");
    private string ProxyScriptDir => Path.Combine(TgProxyDir, "proxy");
    private string ProxyScript => Path.Combine(ProxyScriptDir, "tg_ws_proxy.py");

    // Файлы исходников proxy/ которые нужно скачать
    private static readonly string[] ProxySourceFiles =
    [
        "__init__.py",
        "_aes.py",
        "balancer.py",
        "bridge.py",
        "config.py",
        "fake_tls.py",
        "pool.py",
        "raw_websocket.py",
        "stats.py",
        "tg_ws_proxy.py",
        "utils.py"
    ];

    private const string ProxyRawBase = "https://raw.githubusercontent.com/Flowseal/tg-ws-proxy/main/proxy/";

    // ── Состояние ──
    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    private bool tgProxyRunning;

    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    private bool tgProxyInstalled;

    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    private bool isTgProxyDownloading;

    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    private string tgProxyDownloadStatus = "";

    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    private string tgProxyVersion = "—";

    private Process? _tgProxyProcess;

    public ObservableCollection<string> TgProxyLogs { get; } = new();

    // ── Настройки ──
    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    private string tgProxyHost = "127.0.0.1";
    partial void OnTgProxyHostChanged(string value) => SaveSettings();

    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    private string tgProxyPort = "1443";
    partial void OnTgProxyPortChanged(string value) => SaveSettings();

    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    private string tgProxySecret = "";
    partial void OnTgProxySecretChanged(string value) => SaveSettings();

    // Оставлено только для совместимости со старыми fluxroute-settings.json.
    // В UI, аргументах запуска и Telegram-ссылке SNI-домен больше не используется.
    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    private string tgProxyDomain = "";
    partial void OnTgProxyDomainChanged(string value) => SaveSettings();

    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    private bool tgProxyVerbose = false;
    partial void OnTgProxyVerboseChanged(bool value) => SaveSettings();

    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    private bool tgProxyPreferIPv4 = true;
    partial void OnTgProxyPreferIPv4Changed(bool value) => SaveSettings();

    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    private bool tgProxyAutoStartOnAppLaunch = true;
    partial void OnTgProxyAutoStartOnAppLaunchChanged(bool value) => SaveSettings();

    // DC → IP
    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    private string tgProxyDcIps = "2:149.154.167.220\n4:149.154.167.220";
    partial void OnTgProxyDcIpsChanged(string value) => SaveSettings();

    // Cloudflare Proxy
    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    private bool tgProxyCfEnabled = true;
    partial void OnTgProxyCfEnabledChanged(bool value) => SaveSettings();

    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    private bool tgProxyCfPriority = true;
    partial void OnTgProxyCfPriorityChanged(bool value) => SaveSettings();

    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    private bool tgProxyCfDomainEnabled = false;
    partial void OnTgProxyCfDomainEnabledChanged(bool value) => SaveSettings();

    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    private string tgProxyCfDomain = "";
    partial void OnTgProxyCfDomainChanged(string value) => SaveSettings();

    // Производительность
    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    private string tgProxyBufKb = "256";
    partial void OnTgProxyBufKbChanged(string value) => SaveSettings();

    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    private string tgProxyPoolSize = "4";
    partial void OnTgProxyPoolSizeChanged(string value) => SaveSettings();

    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    private string tgProxyLogMaxMb = "5.0";
    partial void OnTgProxyLogMaxMbChanged(string value) => SaveSettings();

    // ── Текст кнопки запуска ──
    public string TgProxyToggleText => TgProxyRunning ? "⏹ Остановить прокси" : "▶ Запустить прокси";
    partial void OnTgProxyRunningChanged(bool value) => OnPropertyChanged(nameof(TgProxyToggleText));

    // ── Инициализация при первом входе на вкладку ──
    private bool _tgProxyTabVisited = false;

    public void OnTgProxyTabActivated()
    {
        if (_tgProxyTabVisited)
            return;

        _tgProxyTabVisited = true;
        EnsureTgProxyStateInitialized();

        if (!TgProxyInstalled)
        {
            if (Application.Current != null && !Application.Current.Dispatcher.HasShutdownStarted)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (CustomDialog.Show(
                            " TG WS Proxy",
                            "Компонент TG WS Proxy не установлен.\n\nБудет скачано:\n• Python Embeddable (~12 МБ)\n• Исходники прокси (~50 КБ)\n• Пакеты cryptography и aiohttp\n\nЗагрузить сейчас?",
                            "Загрузить",
                            "Отмена"))
                    {
                        _ = DownloadTgProxyAsync();
                    }
                });
            }
        }
    }

    public void InitializeTgProxyOnStartup()
    {
        EnsureTgProxyStateInitialized();

        if (!TgProxyAutoStartOnAppLaunch || !TgProxyInstalled || TgProxyRunning)
            return;

        if (string.IsNullOrWhiteSpace(TgProxySecret))
        {
            AddTgProxyLog("⏭ TG WS Proxy автозапуск пропущен: secret не задан.");
            return;
        }

        StartTgProxy();
    }

    private void EnsureTgProxyStateInitialized()
    {
        TgProxyInstalled = File.Exists(PythonExe) && File.Exists(ProxyScript);
        TgProxyVersion = TgProxyInstalled ? GetTgProxyLocalVersion() : "—";
    }

    // ── Установка ──
    [RelayCommand]
    private async Task DownloadTgProxyAsync()
    {
        IsTgProxyDownloading = true;
        AddTgProxyLog("⬇️ Начало установки TG WS Proxy...");

        try
        {
            Directory.CreateDirectory(TgProxyDir);
            Directory.CreateDirectory(ProxyScriptDir);

            using var http = new HttpClient();
            http.DefaultRequestHeaders.Add("User-Agent", "BSDPI");
            http.Timeout = TimeSpan.FromMinutes(5);

            // Шаг 1: Python Embeddable
            if (!File.Exists(PythonExe))
            {
                TgProxyDownloadStatus = "⬇️ Скачиваем Python Embeddable...";
                AddTgProxyLog(" Скачиваем Python 3.11 Embeddable...");

                var pythonZipUrl = "https://www.python.org/ftp/python/3.11.9/python-3.11.9-embed-amd64.zip";
                var zipBytes = await http.GetByteArrayAsync(pythonZipUrl);
                var zipPath = Path.Combine(TgProxyDir, "python_embed.zip");
                await File.WriteAllBytesAsync(zipPath, zipBytes);

                TgProxyDownloadStatus = " Распаковываем Python...";
                Directory.CreateDirectory(PythonDir);
                ZipFile.ExtractToDirectory(zipPath, PythonDir, overwriteFiles: true);
                File.Delete(zipPath);

                // Правим python311._pth — единственный способ добавить пути в embeddable Python.
                // PYTHONPATH и sys.path игнорируются пока не включён site.
                // Используем относительные пути (через FixPythonPth), чтобы прокси работал
                // при переносе папки приложения.
                FixPythonPth();

                AddTgProxyLog("✅ Python распакован");
            }

            // Всегда обновляем .pth — на случай если пути изменились или установка неполная.
            FixPythonPth();

            // Шаг 2: pip через get-pip.py
            var pipExe = Directory.GetFiles(PythonDir, "pip.exe", SearchOption.AllDirectories).FirstOrDefault();
            if (pipExe is null)
            {
                TgProxyDownloadStatus = "⬇️ Устанавливаем pip...";
                AddTgProxyLog(" Устанавливаем pip...");

                var getPipBytes = await http.GetByteArrayAsync("https://bootstrap.pypa.io/get-pip.py");
                var getPipPath = Path.Combine(TgProxyDir, "get-pip.py");
                await File.WriteAllBytesAsync(getPipPath, getPipBytes);

                await RunProcessAsync(PythonExe, $"\"{getPipPath}\"", PythonDir, extraEnv: GetPythonEnv());
                File.Delete(getPipPath);

                pipExe = Directory.GetFiles(PythonDir, "pip.exe", SearchOption.AllDirectories).FirstOrDefault()
                         ?? Path.Combine(PythonDir, "Scripts", "pip.exe");

                AddTgProxyLog("✅ pip установлен");
            }

            // Шаг 3: сторонние зависимости (cryptography + aiohttp).
            // tg_ws_proxy.py и модули proxy/* используют относительные импорты и
            // обращаются к cryptography (AES) и aiohttp (Cloudflare-туннель/HTTP).
            foreach (var pkg in new[] { "cryptography", "aiohttp" })
            {
                if (!IsPythonPackageInstalled(pkg))
                {
                    TgProxyDownloadStatus = $" Устанавливаем {pkg}...";
                    AddTgProxyLog($" Устанавливаем {pkg}...");
                    await RunProcessAsync(pipExe!, $"install {pkg} --quiet --no-warn-script-location", PythonDir, extraEnv: GetPythonEnv(), ignoreExitCode: true);
                    AddTgProxyLog($"✅ {pkg} установлен");
                }
            }

            // Шаг 4: исходники proxy/
            TgProxyDownloadStatus = "⬇️ Скачиваем исходники прокси...";
            AddTgProxyLog(" Скачиваем исходники proxy/...");

            // Получаем версию
            using var noRedirect = new HttpClientHandler { AllowAutoRedirect = false };
            using var verHttp = new HttpClient(noRedirect);
            verHttp.DefaultRequestHeaders.Add("User-Agent", "BSDPI");
            var verResp = await verHttp.GetAsync("https://github.com/Flowseal/tg-ws-proxy/releases/latest");
            var tagName = verResp.Headers.Location?.ToString().Split('/').LastOrDefault() ?? "unknown";

            foreach (var file in ProxySourceFiles)
            {
                var url = ProxyRawBase + file;
                var dest = Path.Combine(ProxyScriptDir, file);
                var content = await http.GetByteArrayAsync(url);
                await File.WriteAllBytesAsync(dest, content);
            }

            File.WriteAllText(Path.Combine(TgProxyDir, "version.txt"), tagName);
            AddTgProxyLog($"✅ Исходники proxy/ скачаны ({tagName})");

            TgProxyVersion = tagName;
            TgProxyInstalled = true;
            TgProxyDownloadStatus = $"✅ Установлено {tagName}";
            AddTgProxyLog(" TG WS Proxy готов к работе!");

            if (string.IsNullOrWhiteSpace(TgProxySecret))
                GenerateTgProxySecret();

            // Если прокси запущен со старой сломанной установкой — перезапускаем.
            if (TgProxyRunning)
            {
                AddTgProxyLog(" Перезапускаем прокси с новой установкой...");
                StopTgProxy();
                await Task.Delay(1000);
                StartTgProxy();
            }
        }
        catch (Exception ex)
        {
            TgProxyDownloadStatus = $"❌ Ошибка: {ex.Message}";
            AddTgProxyLog($"❌ Ошибка установки: {ex.Message}");
        }
        finally
        {
            IsTgProxyDownloading = false;
        }
    }

    private async Task RunProcessAsync(string exe, string args, string workDir, Dictionary<string, string>? extraEnv = null, bool ignoreExitCode = false)
    {
        var psi = new ProcessStartInfo
        {
            FileName = exe,
            Arguments = args,
            WorkingDirectory = workDir,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        if (extraEnv != null)
        {
            foreach (var kv in extraEnv)
                psi.Environment[kv.Key] = kv.Value;
        }

        using var proc = Process.Start(psi) ?? throw new Exception($"Не удалось запустить {exe}");
        proc.OutputDataReceived += (_, e) => { if (e.Data != null) AppendTgLog(e.Data); };
        proc.ErrorDataReceived += (_, e) => { if (e.Data != null) AppendTgLog(e.Data); };
        proc.BeginOutputReadLine();
        proc.BeginErrorReadLine();
        await proc.WaitForExitAsync();

        if (!ignoreExitCode && proc.ExitCode != 0)
            throw new Exception($"{Path.GetFileName(exe)} завершился с кодом {proc.ExitCode}");
    }

    // Проверяем, установлен ли pip-пакет в embeddable Python (по каталогу модуля
    // или *.dist-info, т.к. cryptography/aiohttp — namespace-пакеты с собственными папками).
    private bool IsPythonPackageInstalled(string name)
    {
        var sitePackages = Path.Combine(PythonDir, "Lib", "site-packages");
        if (!Directory.Exists(sitePackages))
            return false;

        if (Directory.Exists(Path.Combine(sitePackages, name)))
            return true;

        return Directory.EnumerateFileSystemEntries(sitePackages, $"{name}-*.dist-info", SearchOption.TopDirectoryOnly)
            .Any();
    }

    // Прописываем нужные пути в python311._pth — единственный способ управлять sys.path
    // в embeddable Python (PYTHONPATH там игнорируется без включённого site).
    // Embeddable Python НЕ обрабатывает относительные пути вида "..\proxy" в ._pth,
    // поэтому пишем АБСОЛЮТНЫЕ пути, вычисленные от текущего расположения приложения.
    // Если ._pth указывает на старое расположение (например, из предыдущей установки),
    // python не находит пакет proxy и прокси падает/зависает при запуске.
    // Для «-m proxy.tg_ws_proxy» в sys.path должен быть родитель пакета proxy (каталог
    // tg-proxy), а не сама папка proxy.
    private void FixPythonPth()
    {
        var pthFile = Directory.GetFiles(PythonDir, "python*._pth").FirstOrDefault();
        if (pthFile is null)
            return;

        var sitePackages = Path.Combine(PythonDir, "Lib", "site-packages");
        Directory.CreateDirectory(sitePackages);

        var lines = new List<string>
        {
            ".",
            "python311.zip",
            sitePackages,
            TgProxyDir,
            "import site"
        };

        File.WriteAllLines(pthFile, lines);
    }

    // Переменные окружения для корректной работы embeddable Python с пакетами.
    // Для запуска «-m proxy.tg_ws_proxy» пакет proxy должен быть доступен по имени,
    // поэтому в PYTHONPATH добавляем родительский каталог (TgProxyDir = .../tg-proxy),
    // а не саму папку proxy.
    private Dictionary<string, string> GetPythonEnv()
    {
        var sitePackages = Path.Combine(PythonDir, "Lib", "site-packages");
        var scripts = Path.Combine(PythonDir, "Scripts");

        return new Dictionary<string, string>
        {
            ["PYTHONHOME"] = PythonDir,
            ["PYTHONPATH"] = $"{TgProxyDir};{sitePackages}",
            ["PATH"] = $"{PythonDir};{scripts};{Environment.GetEnvironmentVariable("PATH")}" 
        };
    }

    // Определяем, использует ли скачанный tg_ws_proxy.py относительные импорты
    // (пакетный режим, запуск через -m) или «плоские» абсолютные (прямой запуск).
    private bool ScriptUsesRelativeImports()
    {
        try
        {
            var text = File.ReadAllText(ProxyScript);
            return System.Text.RegularExpressions.Regex.IsMatch(text, @"^\s*from\s+\.", System.Text.RegularExpressions.RegexOptions.Multiline)
                || System.Text.RegularExpressions.Regex.IsMatch(text, @"^\s*from\s+proxy\s+import", System.Text.RegularExpressions.RegexOptions.Multiline)
                || System.Text.RegularExpressions.Regex.IsMatch(text, @"^\s*import\s+proxy\b", System.Text.RegularExpressions.RegexOptions.Multiline);
        }
        catch
        {
            return false;
        }
    }

    private string GetTgProxyLocalVersion()
    {
        var versionFile = Path.Combine(TgProxyDir, "version.txt");
        return File.Exists(versionFile) ? File.ReadAllText(versionFile).Trim() : "unknown";
    }

    // ── Генерация Secret (dd + 32 hex = dd-prefix + 16 байт) ──
    [RelayCommand]
    private void GenerateTgProxySecret()
    {
        var bytes = RandomNumberGenerator.GetBytes(16);
        TgProxySecret = "dd" + Convert.ToHexString(bytes).ToLowerInvariant();
        AddTgProxyLog(" Secret сгенерирован");
    }

    // ── Запуск / Остановка ──
    [RelayCommand]
    private void ToggleTgProxy()
    {
        if (TgProxyRunning)
            StopTgProxy();
        else
            StartTgProxy();
    }

    private void StartTgProxy()
    {
        if (TgProxyRunning)
        {
            AddTgProxyLog("⏭ TG WS Proxy уже запущен.");
            return;
        }

        if (IsTgProxyDownloading)
        {
            AddTgProxyLog("⏳ Идёт установка, подождите завершения...");
            return;
        }

        if (!File.Exists(PythonExe) || !File.Exists(ProxyScript))
        {
            AddTgProxyLog("❌ Компонент не установлен.\nНажмите «Обновления».");
            return;
        }

        // Освобождаем порт, если он занят осиротевшим экземпляром прокси
        // (предыдущий python мог не освободить порт после падения → новый
        // стартует с OSError [Errno 10048] Address already in use и падает с кодом 1).
        try { FreePort(int.Parse(TgProxyPort)); } catch { }

        if (string.IsNullOrWhiteSpace(TgProxySecret))
        {
            AddTgProxyLog("❌ Secret не задан. Нажмите для генерации.");
            return;
        }

        var scriptArgs = BuildArguments();

        // Гарантируем корректные пути в ._pth (актуально при переносе папки приложения,
        // когда ._pth может содержать устаревшие абсолютные пути из старой установки).
        FixPythonPth();

        // Исходники proxy/* новых версий используют относительные импорты
        // (from .config import ...), которые работают ТОЛЬКО при запуске в режиме
        // -m из каталога tg-proxy. Старые «плоские» версии используют абсолютные
        // импорты (import config) и запускаются напрямую. Определяем режим по тексту
        // скрипта, чтобы прокси стартовал независимо от скачанной версии.
        string fullArgs;
        if (ScriptUsesRelativeImports())
        {
            fullArgs = $"-m proxy.tg_ws_proxy {scriptArgs}";
            AddTgProxyLog($" python -m proxy.tg_ws_proxy {scriptArgs}");
        }
        else
        {
            fullArgs = $"\"{ProxyScript}\" {scriptArgs}";
            AddTgProxyLog($" python \"{ProxyScript}\" {scriptArgs}");
        }

        var psi = new ProcessStartInfo
        {
            FileName = PythonExe,
            Arguments = fullArgs,
            WorkingDirectory = TgProxyDir,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        foreach (var kv in GetPythonEnv())
            psi.Environment[kv.Key] = kv.Value;

        try
        {
            _tgProxyProcess = Process.Start(psi);
            if (_tgProxyProcess is null)
            {
                AddTgProxyLog("❌ Не удалось запустить процесс");
                return;
            }

            _tgProxyProcess.OutputDataReceived += (_, e) => { if (e.Data != null) AppendTgLog(e.Data); };
            _tgProxyProcess.ErrorDataReceived += (_, e) => { if (e.Data != null) AppendTgLog(e.Data); };
            _tgProxyProcess.BeginOutputReadLine();
            _tgProxyProcess.BeginErrorReadLine();

            TgProxyRunning = true;
            AddTgProxyLog($"▶ TG WS Proxy запущен (PID {_tgProxyProcess.Id})");
            AddTgProxyLog($" Слушает: {TgProxyHost}:{TgProxyPort}");
            _ = WatchTgProxyProcessAsync(_tgProxyProcess);
        }
        catch (Exception ex)
        {
            AddTgProxyLog($"❌ Ошибка запуска: {ex.Message}");
        }
    }

    private string BuildArguments()
    {
        var args = new System.Text.StringBuilder();

        // Python-скрипт принимает только 32 hex-символа (без dd/ee-префикса).
        var rawSecret = TgProxySecret.StartsWith("dd", StringComparison.OrdinalIgnoreCase)
            ? TgProxySecret[2..]
            : TgProxySecret;

        args.Append($"--host {TgProxyHost}");
        args.Append($" --port {TgProxyPort}");
        args.Append($" --secret {rawSecret}");

        // SNI/Fake-TLS домен намеренно не передаём: tg-ws-proxy в BSDPI работает через обычный dd-secret.

        // DC → IP
        foreach (var line in TgProxyDcIps.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var dc = line.Trim();
            if (!string.IsNullOrEmpty(dc))
                args.Append($" --dc-ip {dc}");
        }

        // Cloudflare
        if (!TgProxyCfEnabled)
            args.Append(" --no-cfproxy");
        else if (!TgProxyCfPriority)
            args.Append(" --cfproxy-priority false");

        if (TgProxyCfDomainEnabled && !string.IsNullOrWhiteSpace(TgProxyCfDomain))
            args.Append($" --cfproxy-domain {TgProxyCfDomain.Trim()}");

        // Производительность
        if (int.TryParse(TgProxyBufKb, out var bufKb) && bufKb != 256)
            args.Append($" --buf-kb {bufKb}");

        if (int.TryParse(TgProxyPoolSize, out var poolSize) && poolSize != 4)
            args.Append($" --pool-size {poolSize}");

        if (double.TryParse(TgProxyLogMaxMb, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var logMb) && logMb != 5.0)
            args.Append($" --log-max-mb {logMb.ToString(System.Globalization.CultureInfo.InvariantCulture)}");

        if (TgProxyVerbose)
            args.Append(" -v");

        return args.ToString();
    }

    // Буфер логов прокси: строки приходят из фонового потока процесса очень часто
    // (особенно при активном трафике/ошибках под перехватом WinDivert). Прямая отправка
    // каждой строки через Dispatcher.BeginInvoke забивает очередь UI-потока и вызывает
    // видимое «зависание» интерфейса. Поэтому буферизуем и сливаем пачкой не чаще раза
    // в 250 мс.
    private readonly object _tgLogBufferLock = new();
    private readonly List<string> _tgLogBuffer = new();
    private System.Windows.Threading.DispatcherTimer? _tgLogFlushTimer;

    private void AppendTgLog(string line)
    {
        lock (_tgLogBufferLock)
        {
            _tgLogBuffer.Add(line);
            if (_tgLogFlushTimer is null)
            {
                _tgLogFlushTimer = new System.Windows.Threading.DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(250)
                };
                _tgLogFlushTimer.Tick += (_, _) => FlushTgLogBuffer();
                _tgLogFlushTimer.Start();
            }
        }
    }

    private void FlushTgLogBuffer()
    {
        List<string> batch;
        lock (_tgLogBufferLock)
        {
            if (_tgLogBuffer.Count == 0)
                return;
            batch = new List<string>(_tgLogBuffer);
            _tgLogBuffer.Clear();
        }

        if (Application.Current != null && !Application.Current.Dispatcher.HasShutdownStarted)
        {
            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                foreach (var line in batch)
                    AddTgProxyLog(line);
            });
        }
    }

    private async Task WatchTgProxyProcessAsync(Process proc)
    {
        try
        {
            await proc.WaitForExitAsync();
        }
        catch (Exception)
        {
            // процесс удалён через StopTgProxy
        }

        if (Application.Current != null && !Application.Current.Dispatcher.HasShutdownStarted)
        {
            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                TgProxyRunning = false;
                int? code = null;

                try
                {
                    code = proc.ExitCode;
                }
                catch (Exception)
                {
                    // disposed
                }

                AddTgProxyLog(code.HasValue
                    ? $"⏹ TG WS Proxy остановлен (код: {code})"
                    : "⏹ TG WS Proxy остановлен");
            });
        }
    }

    private void StopTgProxy()
    {
        try
        {
            if (_tgProxyProcess is { HasExited: false })
            {
                _tgProxyProcess.Kill(entireProcessTree: true);
                _tgProxyProcess.Dispose();
                _tgProxyProcess = null;
            }
        }
        catch (Exception ex)
        {
            AddTgProxyLog($"⚠ Ошибка остановки: {ex.Message}");
        }

        // Даём ОС время освободить порт после завершения python-дерева процессов,
        // иначе следующий StartTgProxy может упасть с «Address already in use».
        try { System.Threading.Thread.Sleep(800); } catch { }

        _tgLogFlushTimer?.Stop();
        _tgLogFlushTimer = null;
        FlushTgLogBuffer();

        TgProxyRunning = false;
        AddTgProxyLog("⏹ TG WS Proxy остановлен");
    }

    // Убивает процесс, удерживающий указанный TCP-порт, чтобы новый запуск прокси
    // не падал с «Address already in use».
    private void FreePort(int port)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"/c netstat -ano | findstr :{port}",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
        };

        using var proc = Process.Start(psi);
        if (proc is null)
            return;

        var output = proc.StandardOutput.ReadToEnd();
        proc.WaitForExit();

        var pids = new HashSet<int>();
        foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 5)
                continue;
            if (!int.TryParse(parts[^1], out var pid))
                continue;
            if (pid <= 0)
                continue;
            var state = parts.Length >= 4 ? parts[^2] : string.Empty;
            // Убиваем только LISTENING-соединения (занимающие порт для приёма).
            if (state.Contains("LISTENING", StringComparison.OrdinalIgnoreCase))
                pids.Add(pid);
        }

        foreach (var pid in pids)
        {
            try
            {
                var p = Process.GetProcessById(pid);
                if (p.ProcessName.Equals("python", StringComparison.OrdinalIgnoreCase)
                    || p.ProcessName.Equals("pythonw", StringComparison.OrdinalIgnoreCase))
                {
                    p.Kill(entireProcessTree: true);
                    AddTgProxyLog($"⚠ Освобождён порт {port} (убит осиротевший PID {pid})");
                }
            }
            catch
            {
                // процесс уже завершён или доступ запрещён
            }
        }
    }

    [RelayCommand]
    private async Task CheckTgProxyUpdates()
    {
        AddTgProxyLog(" Проверяем обновления TG WS Proxy...");

        try
        {
            using var handler = new System.Net.Http.HttpClientHandler { AllowAutoRedirect = false };
            using var http = new HttpClient(handler);
            http.DefaultRequestHeaders.Add("User-Agent", "BSDPI");

            var response = await http.GetAsync("https://github.com/Flowseal/tg-ws-proxy/releases/latest");
            var latest = response.Headers.Location?.ToString().Split('/').LastOrDefault() ?? "?";
            var local = GetTgProxyLocalVersion();

            if (latest == local)
            {
                AddTgProxyLog($"✅ Актуальная версия ({local})");
            }
            else
            {
                AddTgProxyLog($"⬆️ Доступна версия {latest} (текущая {local})");

                if (Application.Current != null && !Application.Current.Dispatcher.HasShutdownStarted)
                {
                    var update = Application.Current.Dispatcher.Invoke(() =>
                        CustomDialog.Show(" Обновление", $"Доступна версия {latest}.\nОбновить исходники прокси?", "Обновить", "Отмена"));

                    if (update)
                        await UpdateProxySourcesAsync(latest);
                }
            }
        }
        catch (Exception ex)
        {
            AddTgProxyLog($"❌ Ошибка проверки: {ex.Message}");
        }
    }

    private async Task UpdateProxySourcesAsync(string tagName)
    {
        AddTgProxyLog($"⬇️ Обновляем исходники до {tagName}...");

        try
        {
            using var http = new HttpClient();
            http.DefaultRequestHeaders.Add("User-Agent", "BSDPI");
            var rawBase = $"https://raw.githubusercontent.com/Flowseal/tg-ws-proxy/{tagName}/proxy/";

            foreach (var file in ProxySourceFiles)
            {
                var content = await http.GetByteArrayAsync(rawBase + file);
                await File.WriteAllBytesAsync(Path.Combine(ProxyScriptDir, file), content);
            }

            File.WriteAllText(Path.Combine(TgProxyDir, "version.txt"), tagName);
            TgProxyVersion = tagName;
            AddTgProxyLog($"✅ Исходники обновлены до {tagName}");
        }
        catch (Exception ex)
        {
            AddTgProxyLog($"❌ Ошибка обновления: {ex.Message}");
        }
    }

    [RelayCommand]
    private void ClearTgProxyLogs() => TgProxyLogs.Clear();

    private void AddTgProxyLog(string msg)
    {
        TgProxyLogs.Add(msg);
        while (TgProxyLogs.Count > 500)
            TgProxyLogs.RemoveAt(0);
    }

    public void StopTgProxyOnExit() => StopTgProxy();

    private string TgDeepLink => $"tg://proxy?server=127.0.0.1&port={TgProxyPort}&secret={TgProxySecret}";

    [RelayCommand]
    private void OpenInTelegram()
    {
        if (string.IsNullOrWhiteSpace(TgProxySecret))
        {
            AddTgProxyLog("Secret not set.");
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo(TgDeepLink) { UseShellExecute = true });
            AddTgProxyLog("Opening Telegram with proxy settings...");
        }
        catch (Exception ex)
        {
            AddTgProxyLog($"Error: {ex.Message}");
        }
    }

    [RelayCommand]
    private void CopyTgLink()
    {
        if (string.IsNullOrWhiteSpace(TgProxySecret))
        {
            AddTgProxyLog("Secret not set.");
            return;
        }

        System.Windows.Clipboard.SetText(TgDeepLink);
        AddTgProxyLog($"Copied: {TgDeepLink}");
    }
}
