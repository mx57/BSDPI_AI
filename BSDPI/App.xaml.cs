using System.IO;
using System.Net.Http;
using System.Security.Principal;
using System.Windows;
using BSDPI.AI.Services;
using BSDPI.Core.Models;
using BSDPI.Core.Services;
using BSDPI.Core.Services.ChainBuilder;
using BSDPI.Services;
using BSDPI.Updater.Services;
using BSDPI.ViewModels;
using BSDPI.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Application = System.Windows.Application;
using WpfMessageBox = System.Windows.MessageBox;

namespace BSDPI;

public partial class App : Application
{
    private IHost? _host;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        Log.Logger = ConfigureSerilog(new LoggerConfiguration()).CreateLogger();

        try
        {
            _host = Host.CreateDefaultBuilder(e.Args)
                .UseContentRoot(AppContext.BaseDirectory)
                .UseSerilog((_, _, loggerConfiguration) => ConfigureSerilog(loggerConfiguration))
                .ConfigureServices(ConfigureApplicationServices)
                .Build();

            await _host.StartAsync();

            Log.Information("BSDPI application host started. Arguments: {Arguments}", e.Args);

            if (!IsRunningAsAdmin())
            {
                Log.Warning("BSDPI is running without administrator privileges.");

                ShutdownMode = ShutdownMode.OnExplicitShutdown;

                var prompt = new AdminPromptWindow();
                prompt.ShowDialog();

                if (!prompt.ContinueWithoutAdmin)
                {
                    Log.Information("User declined to continue without administrator privileges.");
                    Shutdown();
                    return;
                }
            }

            ShutdownMode = ShutdownMode.OnMainWindowClose;

            var mainWindow = _host.Services.GetRequiredService<MainWindow>();
            MainWindow = mainWindow;
            mainWindow.Show();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "BSDPI failed to start.");

            WpfMessageBox.Show(
                $"BSDPI не удалось запустить.\n\n{ex.Message}",
                "Критическая ошибка запуска",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            Shutdown(-1);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        try
        {
            if (_host is not null)
            {
                Log.Information("Stopping BSDPI application host.");
                _host.StopAsync(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();
                _host.Dispose();
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "BSDPI application host stopped with errors.");
        }
        finally
        {
            Log.Information("BSDPI application exited with code {ExitCode}.", e.ApplicationExitCode);
            Log.CloseAndFlush();
            base.OnExit(e);
        }
    }

    private static void ConfigureApplicationServices(IServiceCollection services)
    {
        services.AddHttpClient(BSDPI.Updater.Services.HttpClientNames.Updater, client =>
        {
            client.Timeout = TimeSpan.FromSeconds(60);
            client.DefaultRequestHeaders.Add("User-Agent", "BSDPI-Updater");
        })
        .AddStandardResilienceHandler();

        services.AddHttpClient(BSDPI.Core.Services.HttpClientNames.Connectivity, client =>
        {
            client.Timeout = TimeSpan.FromSeconds(10);
            client.DefaultRequestHeaders.Add("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            client.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
            client.DefaultRequestHeaders.Add("Accept-Language", "ru-RU,ru;q=0.9,en-US;q=0.8,en;q=0.7");
        })
        .AddStandardResilienceHandler();

        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<IUpdaterService, UpdaterService>();
        services.AddSingleton<IByeDpiUpdaterService, ByeDpiUpdaterService>();
        services.AddSingleton<IWarpUpdaterService, WarpUpdaterService>();
        services.AddSingleton<IAppUpdaterService, AppUpdaterService>();
        services.AddSingleton<IConnectivityChecker, ConnectivityChecker>();

        services.AddSingleton(sp =>
        {
            var engineDir = Path.Combine(AppContext.BaseDirectory, "engine");
            return new DpiEngineManager(engineDir);
        });

        services.AddSingleton<NetworkFingerprintProvider>();
        services.AddSingleton(sp =>
        {
            var settingsService = sp.GetRequiredService<ISettingsService>();
            var dir = Path.GetDirectoryName(settingsService.SettingsPath)!;
            var registryPath = Path.Combine(dir, "fluxroute-ai-strategies.json");
            var registry = new AiStrategyRegistry(registryPath);
            registry.Load();
            return registry;
        });
        services.AddSingleton(sp =>
        {
            var settingsService = sp.GetRequiredService<ISettingsService>();
            var dir = Path.GetDirectoryName(settingsService.SettingsPath)!;
            return new AiHistoryStore(Path.Combine(dir, "fluxroute-ai-history.jsonl"));
        });

        services.AddSingleton(sp =>
            new BanditSelector(
                sp.GetRequiredService<AiStrategyRegistry>(),
                () => sp.GetRequiredService<ISettingsService>().Load().Ai,
                new Random()));

        services.AddSingleton(sp =>
        {
            var engineDir = Path.Combine(AppContext.BaseDirectory, "engine");
            return new StrategyEvolver(
                sp.GetRequiredService<AiStrategyRegistry>(),
                sp.GetRequiredService<AiHistoryStore>(),
                () => engineDir,
                () => sp.GetRequiredService<ISettingsService>().Load().Ai);
        });

        services.AddSingleton(sp =>
        {
            var engineDir = Path.Combine(AppContext.BaseDirectory, "engine");
            return new BatMaterializer(() => engineDir);
        });

        services.AddSingleton(sp =>
            new NetworkChangeWatcher(sp.GetRequiredService<NetworkFingerprintProvider>()));

        services.AddSingleton(sp =>
        {
            var settings = sp.GetRequiredService<ISettingsService>();
            var dir = Path.GetDirectoryName(settings.SettingsPath)!;
            return new ChainStore(Path.Combine(dir, "chains"));
        });

        services.AddSingleton<MainViewModel>(sp => new MainViewModel(
            sp.GetRequiredService<ISettingsService>(),
            sp.GetRequiredService<IUpdaterService>(),
            sp.GetRequiredService<IAppUpdaterService>(),
            sp.GetRequiredService<IByeDpiUpdaterService>(),
            sp.GetRequiredService<IWarpUpdaterService>(),
            sp.GetRequiredService<IConnectivityChecker>(),
            sp.GetRequiredService<DpiEngineManager>(),
            sp.GetRequiredService<NetworkFingerprintProvider>(),
            sp.GetRequiredService<NetworkChangeWatcher>(),
            sp.GetRequiredService<AiStrategyRegistry>(),
            sp.GetRequiredService<AiHistoryStore>(),
            sp.GetRequiredService<BanditSelector>(),
            sp.GetRequiredService<StrategyEvolver>(),
            sp.GetRequiredService<BatMaterializer>(),
            sp.GetRequiredService<ChainStore>()));
        services.AddSingleton<TrayIconService>();
        services.AddSingleton<MainWindow>();
    }

    private static LoggerConfiguration ConfigureSerilog(LoggerConfiguration loggerConfiguration)
    {
        var logDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BSDPI",
            "logs");

        Directory.CreateDirectory(logDirectory);

        return loggerConfiguration
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.File(
                path: Path.Combine(logDirectory, "fluxroute-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14,
                shared: true,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}");
    }

    private static bool IsRunningAsAdmin()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }
}
