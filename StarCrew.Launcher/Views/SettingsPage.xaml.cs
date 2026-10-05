using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using StarCrew.Launcher.Models;
using StarCrew.Launcher.Services;
using Windows.Storage;

namespace StarCrew.Launcher.Views;

public sealed partial class SettingsPage : Page
{
    private IThemeSettings? _themeSettings;
    private Type? _currentSectionPageType;
    private bool _initialNavigated;

    public SettingsPage()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    /// <summary>主题变更时通知宿主窗口应用到整窗，参数为新偏好。</summary>
    internal event EventHandler<AppTheme>? ThemeChanged;

    /// <summary>需要右上 Toast 时通知宿主窗口，参数依次为标题、内容、级别。</summary>
    internal event Action<string, string, InfoBarSeverity>? NotifyRequested;

    internal void BindTheme(IThemeSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        _themeSettings = settings;
        if (SectionFrame?.Content is AppearancePage appearance)
        {
            appearance.BindTheme(settings);
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _themeSettings ??= new ThemeSettings(GetSharedStore());
        _initialNavigated = true;
        SectionFrame_Navigate(typeof(AppearancePage), new SuppressNavigationTransitionInfo());
    }

    private void SectionNav_SelectionChanged(
        NavigationView sender,
        NavigationViewSelectionChangedEventArgs args
    )
    {
        if (!_initialNavigated)
        {
            return;
        }

        Type? pageType = args.SelectedItem is NavigationViewItem selected
            ? (Equals(selected.Tag, "About") ? typeof(AboutPage) : typeof(AppearancePage))
            : null;
        if (pageType is not null)
        {
            SectionFrame_Navigate(pageType, args.RecommendedNavigationTransitionInfo);
        }
    }

    private void SectionFrame_Navigate(Type pageType, NavigationTransitionInfo transitionInfo)
    {
        if (!Type.Equals(_currentSectionPageType, pageType))
        {
            _currentSectionPageType = pageType;
            SectionFrame.Navigate(pageType, null, transitionInfo);
        }
    }

    private void SectionFrame_Navigated(object sender, NavigationEventArgs e)
    {
        switch (e.Content)
        {
            case AppearancePage appearance:
                appearance.BindTheme(_themeSettings ?? new ThemeSettings(GetSharedStore()));
                appearance.ThemeChanged += OnAppearanceThemeChanged;
                break;
            case AboutPage about:
                about.NotifyRequested += OnAboutNotifyRequested;
                break;
        }
    }

    private void OnAppearanceThemeChanged(object? sender, AppTheme theme)
    {
        ThemeChanged?.Invoke(this, theme);
    }

    private void OnAboutNotifyRequested(string title, string message, InfoBarSeverity severity)
    {
        NotifyRequested?.Invoke(title, message, severity);
    }

    // LocalSettings.Values 即 IDictionary，测试替身用内存字典。
    private static IDictionary<string, object> GetSharedStore()
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
