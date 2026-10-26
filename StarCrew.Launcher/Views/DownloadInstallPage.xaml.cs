using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using StarCrew.Launcher.Models;
using StarCrew.Launcher.Services;
using StarCrew.Launcher.ViewModels;

namespace StarCrew.Launcher.Views;

public sealed partial class DownloadInstallPage : Page
{
    private readonly IAppStrings _strings = App.Services.GetRequiredService<IAppStrings>();
    private readonly FrameNavigationService _navigation =
        App.Services.GetRequiredService<FrameNavigationService>();

    public DownloadInstallPage()
    {
        InitializeComponent();
    }

    internal DownloadPageViewModel? ViewModel { get; private set; }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        ViewModel = e.Parameter as DownloadPageViewModel;
        TitleBreadcrumb.ItemsSource = new[]
        {
            // XAML 的 x:Uid 后缀在 PRI 里是层级路径，ResourceLoader 需用斜杠形式
            _strings.GetString("Download_SectionTitle/Text"),
            ViewModel?.SelectedVersion?.VersionLabel ?? string.Empty,
        };
        Bindings.Update();
    }

    private void TitleBreadcrumb_ItemClicked(
        BreadcrumbBar sender,
        BreadcrumbBarItemClickedEventArgs args
    )
    {
        _ = sender;
        if (args.Index == 0)
        {
            _navigation.GoBack();
        }
    }

    private void InstallButton_Click(object sender, RoutedEventArgs e)
    {
        _ = sender;
        _ = e;
        _ = RunInstallAsync();
    }

    private async Task RunInstallAsync()
    {
        if (
            ViewModel is not { IsBusy: false } viewModel
            || viewModel.SelectedVersion is not GameVersionEntry entry
        )
        {
            return;
        }

        ProgressBar bar = new() { Minimum = 0, Maximum = 100 };
        TextBlock status = new() { Text = _strings.GetString("Account_PreparingDownload") };
        DownloadProgressFooter footer = new(_strings);
        ContentDialog progressDialog = ContentDialogFactory.Create(
            XamlRoot,
            d =>
            {
                d.Title = _strings.Format("Install_DialogTitleFormat", entry.VersionLabel);
                d.Content = new StackPanel { Spacing = 8, Children = { status, bar, footer.Root } };
                d.CloseButtonText = _strings.GetString("Common_Cancel");
            }
        );

        using CancellationTokenSource cts = new();
        progressDialog.Closing += (_, _) => cts.Cancel();
        IProgress<DownloadProgressInfo> reporter = new Progress<DownloadProgressInfo>(info =>
        {
            bar.Value = info.Percent;
            status.Text = info.Message;
            footer.Apply(info);
        });

        Task<ContentDialogResult> showing = progressDialog.ShowAsync().AsTask();
        GameInstallResult result = await viewModel.InstallAsync(
            entry,
            viewModel.InstanceName,
            reporter,
            cts.Token
        );
        progressDialog.Hide();
        await showing;

        if (result.IsSuccess)
        {
            _navigation.GoBack();
        }
    }
}
