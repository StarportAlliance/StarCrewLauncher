using System.Globalization;
using System.Runtime.InteropServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using Microsoft.Windows.Storage;
using Serilog;
using StarportAlliance.StarCrewLauncher.Services;
using StarportAlliance.StarCrewLauncher.ViewModels;
using Velopack;

namespace StarportAlliance.StarCrewLauncher;

public sealed partial class App : Application
{
    private const string MainInstanceKey = "StarCrewLauncher.Main";

    private MainWindow? _window;

    internal static IServiceProvider Services { get; private set; } = null!;

    public App()
    {
        try
        {
            VelopackApp.Build().Run();

            IDictionary<string, object> store = CreateSettingsStore();
            ILoggingSettings loggingSettings = new LoggingSettings(store);
            LoggingBootstrap.Configure(loggingSettings);
            AttachGlobalExceptionHandlers();

            InitializeComponent();
            Services = BuildServices(store, loggingSettings);
            WriteStartupSummary();
        }
        catch (Exception ex)
        {
            StartupFailureDialog.Show(ex);
            Environment.Exit(1);
        }
    }

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        AppInstance mainInstance = AppInstance.FindOrRegisterForKey(MainInstanceKey);
        if (!mainInstance.IsCurrent)
        {
            await mainInstance.RedirectActivationToAsync(
                AppInstance.GetCurrent().GetActivatedEventArgs()
            );
            Exit();
            return;
        }

        DispatcherQueue dispatcher = DispatcherQueue.GetForCurrentThread();
        mainInstance.Activated += (_, _) =>
            dispatcher.TryEnqueue(() => _window?.BringToForeground());

        _window = Services.GetRequiredService<MainWindow>();
        _window.Activate();
    }

    private void AttachGlobalExceptionHandlers()
    {
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            Log.Fatal(e.ExceptionObject as Exception, "未处理的异常，进程即将终止");
            Log.CloseAndFlush();
        };

        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            Log.Error(e.Exception, "未观察到的任务异常");
            e.SetObserved();
        };

        UnhandledException += (_, e) =>
        {
            Log.Fatal(e.Exception, "UI 线程未处理的异常");
            Log.CloseAndFlush();
        };
    }

    private static void WriteStartupSummary() =>
        Log.Information(
            "StarCrew Launcher {Version} 启动：OS {Os}，架构 {Architecture}，语言 {Language}，日志目录 {LogDirectory}",
            Services.GetRequiredService<IAppInfo>().Version ?? "unknown",
            Environment.OSVersion.VersionString,
            RuntimeInformation.OSArchitecture,
            CultureInfo.CurrentUICulture.Name,
            AppPaths.LogDirectory
        );

    private static ServiceProvider BuildServices(
        IDictionary<string, object> store,
        ILoggingSettings loggingSettings
    )
    {
        ServiceCollection services = [];
        services.AddLogging(builder => builder.AddSerilog(Log.Logger, dispose: false));
        services.AddSingleton(store);
        services.AddSingleton(loggingSettings);
        services.AddSingleton<IVersionCacheSettings>(new VersionCacheSettings(store));
        services.AddSingleton<IAppStrings, AppStrings>();
        services.AddSingleton<ISystemFontCatalog, SystemFontCatalog>();
        services.AddSingleton<IThemeSettings>(sp => new ThemeSettings(
            sp.GetRequiredService<IDictionary<string, object>>()
        ));
        services.AddSingleton<IAppInfo, AppInfo>();
        services.AddSingleton<IExternalLauncher, ExternalLauncher>();
        services.AddSingleton<IFilePickerService, FilePickerService>();
        services.AddSingleton<IBackdropSupport, BackdropSupport>();
        services.AddSingleton<IBackgroundImageStore>(sp => new BackgroundImageStore(
            AppPaths.BackgroundDirectory
        ));
        services.AddSingleton<INotificationService, NotificationService>();
        services.AddSingleton<IAppRestarter, AppRestarter>();
        services.AddSingleton<FrameNavigationService>();
        services.AddSingleton<INavigationService>(sp =>
            sp.GetRequiredService<FrameNavigationService>()
        );
        services.AddSingleton<IUpdateClient>(_ => new VelopackUpdateClient(
            VelopackUpdateClient.DefaultFeedUrl
        ));
        services.AddSingleton<AppUpdater>();
        services.AddSingleton<IProcessStarter, ProcessStarter>();
        services.AddSingleton<ISteamEnvironment, WindowsSteamEnvironment>();
        services.AddSingleton<ISteamClient, WindowsSteamClient>();
        services.AddSingleton<IInstanceLauncher, InstanceLauncher>();
        services.AddSingleton<IInstanceStore>(sp => new InstanceStore(
            AppPaths.StateFile,
            sp.GetRequiredService<ILogger<InstanceStore>>()
        ));
        services.AddSingleton<IGameVersionCatalog, GameVersionCatalog>();
        services.AddSingleton<IDepotDownloaderRunner, ProcessRunner>();
        services.AddSingleton<DepotDownloaderLocator>();
        services.AddSingleton<ISteamLoginManager, SteamLoginManager>();
        services.AddSingleton<IDownloadCoordinator, DownloadCoordinator>();
        services.AddTransient<HomeViewModel>();
        services.AddTransient<AppearanceViewModel>();
        services.AddTransient<AdvancedViewModel>();
        services.AddTransient<AboutViewModel>();
        services.AddTransient<InstancesViewModel>();
        services.AddTransient<DownloadPageViewModel>();
        services.AddSingleton<HighContrastWatcher>();
        services.AddSingleton(sp => new MainWindow(
            sp.GetRequiredService<IThemeSettings>(),
            sp.GetRequiredService<ISystemFontCatalog>(),
            sp.GetRequiredService<INotificationService>(),
            sp.GetRequiredService<FrameNavigationService>(),
            sp.GetRequiredService<IFilePickerService>(),
            sp.GetRequiredService<IAppInfo>(),
            sp.GetRequiredService<HighContrastWatcher>(),
            sp.GetRequiredService<IBackdropSupport>()
        ));
        return services.BuildServiceProvider();
    }

    private static IDictionary<string, object> CreateSettingsStore() =>
        ApplicationData
            .GetForUnpackaged("StarportAlliance", "StarCrewLauncher")
            .LocalSettings.Values;
}
