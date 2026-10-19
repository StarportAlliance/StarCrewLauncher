using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using StarCrew.Launcher.ViewModels;

namespace StarCrew.Launcher.Views;

public sealed partial class AboutPage : Page
{
    private readonly IAppStrings _strings = App.Services.GetRequiredService<IAppStrings>();

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
        ViewModel?.UpdateAvailable += OnUpdateAvailable;
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

    private async void FluentlauncherLicenseCard_Click(object sender, RoutedEventArgs e) =>
        await OpenUriAsync("https://github.com/Xcube-Studio/Natsurainko.FluentLauncher/blob/main/LICENSE");

    private Task OpenUriAsync(string uri) => ViewModel?.OpenUriAsync(uri) ?? Task.CompletedTask;

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
                d.Title = _strings.GetString("About_UpdateAvailableTitle");
                d.Content = _strings.Format("About_UpdateAvailableContentFormat", version);
                d.PrimaryButtonText = _strings.GetString("Common_Download");
                d.CloseButtonText = _strings.GetString("Common_Later");
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
                d.Title = _strings.Format("About_DownloadingFormat", version);
                d.Content = bar;
                d.CloseButtonText = _strings.GetString("Common_Cancel");
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
