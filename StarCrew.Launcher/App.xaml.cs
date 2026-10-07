using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using StarCrew.Launcher.Services;
using StarCrew.Launcher.ViewModels;
using Velopack;
using Windows.Storage;

namespace StarCrew.Launcher;

public sealed partial class App : Application
{
    private Window? _window;

    /// <summary>组合根：页面代码隐藏只经此取界面层基础件，业务依赖一律走构造注入。</summary>
    internal static IServiceProvider Services { get; private set; } = null!;

    public App()
    {
        VelopackApp.Build().Run();
        InitializeComponent();
        Services = BuildServices();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _window = Services.GetRequiredService<MainWindow>();
        _window.Activate();
    }

    private static ServiceProvider BuildServices()
    {
        ServiceCollection services = [];
        services.AddSingleton<IDictionary<string, object>>(_ => CreateSettingsStore());
        services.AddSingleton<IThemeSettings>(sp => new ThemeSettings(
            sp.GetRequiredService<IDictionary<string, object>>()
        ));
        services.AddSingleton<IAppInfo, AppInfo>();
        services.AddSingleton<IExternalLauncher, ExternalLauncher>();
        services.AddSingleton<IFilePickerService, FilePickerService>();
        services.AddSingleton<INotificationService, NotificationService>();
        services.AddSingleton<FrameNavigationService>();
        services.AddSingleton<INavigationService>(sp =>
            sp.GetRequiredService<FrameNavigationService>()
        );
        services.AddSingleton<IUpdateClient>(_ => new VelopackUpdateClient(
            VelopackUpdateClient.DefaultFeedUrl
        ));
        services.AddSingleton<AppUpdater>();
        services.AddSingleton<IProcessStarter, ProcessStarter>();
        services.AddSingleton<ISteamGameLocator>(_ => new SteamGameLocator(
            new WindowsSteamEnvironment()
        ));
        services.AddSingleton<IGameLauncher, GameLauncher>();
        services.AddTransient<HomeViewModel>();
        services.AddTransient<AppearanceViewModel>();
        services.AddTransient<AboutViewModel>();
        services.AddSingleton<MainWindow>(sp => new MainWindow(
            sp.GetRequiredService<IThemeSettings>(),
            sp.GetRequiredService<INotificationService>(),
            sp.GetRequiredService<FrameNavigationService>(),
            sp.GetRequiredService<IFilePickerService>()
        ));
        return services.BuildServiceProvider();
    }

    // LocalSettings.Values 即 IDictionary，开发期取不到时用内存回退。
    private static IDictionary<string, object> CreateSettingsStore()
    {
        try
        {
            if (ApplicationData.Current?.LocalSettings.Values is IDictionary<string, object> store)
            {
                return store;
            }
        }
        catch (InvalidOperationException) { }

        return new Dictionary<string, object>();
    }
}
