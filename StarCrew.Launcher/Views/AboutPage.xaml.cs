using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using StarCrew.Launcher.ViewModels;

namespace StarCrew.Launcher.Views;

public sealed partial class AboutPage : Page
{
    public AboutPage()
    {
        InitializeComponent();
    }

    internal AboutViewModel? ViewModel { get; private set; }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        ViewModel = e.Parameter as AboutViewModel;
        Bindings.Update();
        if (ViewModel is not null)
        {
            ViewModel.UpdateAvailable += OnUpdateAvailable;
        }
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        if (ViewModel is not null)
        {
            ViewModel.UpdateAvailable -= OnUpdateAvailable;
        }
    }

    private async void WebsiteCard_Click(object sender, RoutedEventArgs e) =>
        await OpenUriAsync("https://stcrew.app");

    private async void FeedbackCard_Click(object sender, RoutedEventArgs e) =>
        await OpenUriAsync("https://github.com/StarportAlliance/StarCrewLauncher/issues");

    private async void LicenseCard_Click(object sender, RoutedEventArgs e) =>
        await OpenUriAsync(
            "https://github.com/StarportAlliance/StarCrewLauncher/blob/main/LICENSE"
        );

    private async void WindowsAppSdkLicenseCard_Click(object sender, RoutedEventArgs e) =>
        await OpenUriAsync("https://github.com/microsoft/WindowsAppSDK/blob/main/LICENSE");

    private async void CommunityToolkitLicenseCard_Click(object sender, RoutedEventArgs e) =>
        await OpenUriAsync("https://github.com/CommunityToolkit/Windows/blob/main/License.md");

    private async void VelopackLicenseCard_Click(object sender, RoutedEventArgs e) =>
        await OpenUriAsync("https://github.com/velopack/velopack/blob/develop/LICENSE");

    private async void DepotDownloaderLicenseCard_Click(object sender, RoutedEventArgs e) =>
        await OpenUriAsync("https://github.com/SteamRE/DepotDownloader/blob/master/LICENSE");

    private Task OpenUriAsync(string uri) => ViewModel?.OpenUriAsync(uri) ?? Task.CompletedTask;

    /// <summary>命中新版本时先弹窗确认，确认后弹进度框下载，成功后直接退出触发自动重启。</summary>
    private async void OnUpdateAvailable(object? sender, string version)
    {
        if (ViewModel is null)
        {
            return;
        }

        ContentDialog confirm = ContentDialogFactory.Create(
            XamlRoot,
            d =>
            {
                d.Title = "发现新版本";
                d.Content = $"检测到新版本 {version}，是否下载？";
                d.PrimaryButtonText = "下载";
                d.CloseButtonText = "稍后";
                d.DefaultButton = ContentDialogButton.Primary;
            }
        );
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
        ContentDialog downloading = ContentDialogFactory.Create(
            XamlRoot,
            d =>
            {
                d.Title = $"正在下载新版本 {version}";
                d.Content = bar;
                d.CloseButtonText = "取消";
            }
        );
        using CancellationTokenSource cts = new();
        downloading.Closing += (_, _) => cts.Cancel();
        Task<ContentDialogResult> showing = downloading.ShowAsync().AsTask();

        bool readyToRestart = await ViewModel.DownloadUpdateAsync(
            version,
            new Progress<int>(percent => bar.Value = percent),
            cts.Token
        );

        downloading.Hide();
        await showing;
        if (readyToRestart)
        {
            Application.Current.Exit();
        }
    }
}
