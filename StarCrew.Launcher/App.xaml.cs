using System.Globalization;
using System.Runtime.InteropServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Serilog;
using StarCrew.Launcher.Services;
using StarCrew.Launcher.ViewModels;
using Velopack;
using Windows.Storage;

namespace StarCrew.Launcher;

public sealed partial class App : Application
{
    private Window? _window;

    internal static IServiceProvider Services { get; private set; } = null!;

    public App()
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

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
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
        services.AddSingleton<IAppStrings, AppStrings>();
        services.AddSingleton<IThemeSettings>(sp => new ThemeSettings(
            sp.GetRequiredService<IDictionary<string, object>>()
        ));
        services.AddSingleton<IAppInfo, AppInfo>();
        services.AddSingleton<IExternalLauncher, ExternalLauncher>();
        services.AddSingleton<IFilePickerService, FilePickerService>();
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
        services.AddTransient<AboutViewModel>();
        services.AddTransient<InstancesViewModel>();
        services.AddTransient<DownloadPageViewModel>();
        services.AddSingleton(sp => new MainWindow(
            sp.GetRequiredService<IThemeSettings>(),
            sp.GetRequiredService<INotificationService>(),
            sp.GetRequiredService<FrameNavigationService>(),
            sp.GetRequiredService<IFilePickerService>()
        ));
        return services.BuildServiceProvider();
    }

    private static IDictionary<string, object> CreateSettingsStore()
    {
        try
        {
            // LocalSettings.Values 即 IDictionary，开发期取不到时用内存回退
            if (ApplicationData.Current?.LocalSettings.Values is IDictionary<string, object> store)
            {
                return store;
            }
        }
        catch (InvalidOperationException) { }

        return new Dictionary<string, object>();
    }
}
