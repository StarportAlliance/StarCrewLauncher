using System.Reflection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using StarCrew.Launcher.Models;
using StarCrew.Launcher.Services;
using Windows.Storage;

namespace StarCrew.Launcher.Views;

public sealed partial class SettingsPage : Page
{
    private IThemeSettings? _themeSettings;
    private bool _syncingTheme;

    public SettingsPage()
    {
        InitializeComponent();
        // 默认面板先挂载：后继 BindTheme/OnLoaded 要摸里面的 ThemeCombo。
        FindName(nameof(AppearancePanel));
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
        SyncThemeCombo();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _themeSettings ??= new ThemeSettings(GetSharedStore());
        SyncThemeCombo();
        if (SectionNav.SelectedItem is null && SectionNav.MenuItems.Count > 0)
        {
            SectionNav.SelectedItem = SectionNav.MenuItems[0];
        }

        SyncPanels(scrollToTop: false);
    }

    private void SectionNav_SelectionChanged(
        NavigationView sender,
        NavigationViewSelectionChangedEventArgs args
    )
    {
        SyncPanels(scrollToTop: true);
    }

    // 面板用 x:Load 按需挂载：重新挂载触发 Load，面板自身 Transitions（Entrance 整板齐滑）自动重播，无需手写 Storyboard。
    // SelectionChanged 在 InitializeComponent 期间就会触发，此时滚动器仍为 null，直接返回等 Loaded 再同步。
    private void SyncPanels(bool scrollToTop)
    {
        if (SettingsScroll is null || SectionNav is null)
        {
            return;
        }

        bool showAbout =
            SectionNav.SelectedItem is NavigationViewItem selected && Equals(selected.Tag, "About");
        if (showAbout)
        {
            if (AppearancePanel is not null)
            {
                UnloadObject(AppearancePanel);
            }

            if (FindName(nameof(AboutPanel)) is StackPanel about)
            {
                about.Visibility = Visibility.Visible;
                ShowVersion();
            }
        }
        else
        {
            if (AboutPanel is not null)
            {
                UnloadObject(AboutPanel);
            }

            if (FindName(nameof(AppearancePanel)) is StackPanel appearance)
            {
                appearance.Visibility = Visibility.Visible;
                // 重挂会重置 ComboBox 等交互状态，用已存主题重同步（守卫内不回写）。
                SyncThemeCombo();
            }
        }

        if (scrollToTop)
        {
            SettingsScroll.ChangeView(null, 0, null);
        }
    }

    private void SyncThemeCombo()
    {
        if (_themeSettings is null || ThemeCombo is null)
        {
            return;
        }

        _syncingTheme = true;
        try
        {
            ThemeCombo.SelectedIndex = _themeSettings.Theme switch
            {
                AppTheme.Light => 1,
                AppTheme.Dark => 2,
                _ => 0,
            };
        }
        finally
        {
            _syncingTheme = false;
        }
    }

    private void ShowVersion()
    {
        Version? version = Assembly.GetExecutingAssembly().GetName().Version;
        AboutVersionCard.Description = version is null
            ? "版本 未知"
            : $"版本 {version.Major}.{version.Minor}.{version.Build}";
    }

    private void ThemeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_syncingTheme || _themeSettings is null)
        {
            return;
        }

        AppTheme theme = ThemeCombo.SelectedIndex switch
        {
            1 => AppTheme.Light,
            2 => AppTheme.Dark,
            _ => AppTheme.System,
        };
        _themeSettings.Theme = theme;
        ThemeChanged?.Invoke(this, theme);
    }

    private async void WebsiteCard_Click(object sender, RoutedEventArgs e)
    {
        await Windows.System.Launcher.LaunchUriAsync(new Uri("https://stcrew.app"));
    }

    private async void FeedbackCard_Click(object sender, RoutedEventArgs e)
    {
        await Windows.System.Launcher.LaunchUriAsync(
            new Uri("https://github.com/StarportAlliance/StarCrewLauncher/issues")
        );
    }

    private async void CheckUpdateButton_Click(object sender, RoutedEventArgs e)
    {
        CheckUpdateButton.IsEnabled = false;

        try
        {
            AppUpdater updater = new(new VelopackUpdateClient(VelopackUpdateClient.DefaultFeedUrl));
            UpdateCheckResult check = await updater.CheckOnlyAsync();
            if (check.State != UpdateState.Available || check.AvailableVersion is null)
            {
                (string title, InfoBarSeverity severity) = check.State switch
                {
                    UpdateState.UpToDate => ("已是最新", InfoBarSeverity.Success),
                    UpdateState.NotInstalled => ("无法检查更新", InfoBarSeverity.Informational),
                    _ => ("检查更新失败", InfoBarSeverity.Error),
                };
                NotifyRequested?.Invoke(title, check.Message, severity);
                return;
            }

            await DownloadWithConfirmAsync(updater, check.AvailableVersion);
        }
        finally
        {
            CheckUpdateButton.IsEnabled = true;
        }
    }

    /// <summary>命中新版本时先弹窗确认，确认后弹进度框下载，成功后直接退出触发自动重启。</summary>
    private async Task DownloadWithConfirmAsync(AppUpdater updater, string version)
    {
        ContentDialog confirm = new()
        {
            XamlRoot = XamlRoot,
            Title = "发现新版本",
            Content = $"检测到新版本 {version}，是否下载？",
            PrimaryButtonText = "下载",
            CloseButtonText = "稍后",
            DefaultButton = ContentDialogButton.Primary,
        };
        if (await confirm.ShowAsync() != ContentDialogResult.Primary)
        {
            return;
        }

        ProgressBar bar = new()
        {
            Minimum = 0,
            Maximum = 100,
            Width = 280,
        };
        ContentDialog downloading = new()
        {
            XamlRoot = XamlRoot,
            Title = $"正在下载新版本 {version}",
            Content = bar,
            CloseButtonText = "取消",
        };
        using CancellationTokenSource cts = new();
        downloading.Closing += (_, _) => cts.Cancel();
        Task<ContentDialogResult> showing = downloading.ShowAsync().AsTask();

        UpdateCheckResult downloaded;
        try
        {
            downloaded = await updater.DownloadAndPrepareAsync(
                version,
                new Progress<int>(percent => bar.Value = percent),
                cts.Token
            );
        }
        catch (OperationCanceledException)
        {
            await showing;
            NotifyRequested?.Invoke(
                "已取消下载",
                "已取消新版本下载。",
                InfoBarSeverity.Informational
            );
            return;
        }

        downloading.Hide();
        await showing;
        if (downloaded.State == UpdateState.ReadyToRestart)
        {
            Application.Current.Exit();
            return;
        }

        NotifyRequested?.Invoke("下载更新失败", downloaded.Message, InfoBarSeverity.Error);
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
