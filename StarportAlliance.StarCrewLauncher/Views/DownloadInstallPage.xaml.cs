using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using StarportAlliance.StarCrewLauncher.Models;
using StarportAlliance.StarCrewLauncher.Services;
using StarportAlliance.StarCrewLauncher.ViewModels;

namespace StarportAlliance.StarCrewLauncher.Views;

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
            _strings.GetString("Download_Title"),
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

        if (!await viewModel.HasValidSessionAsync())
        {
            await ShowLoginRequiredAsync();
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
        GameInstallResult result;
        try
        {
            result = await viewModel.InstallAsync(
                entry,
                viewModel.InstanceName,
                reporter,
                cts.Token
            );
        }
        finally
        {
            progressDialog.Hide();
        }

        await showing;

        if (result.IsSuccess)
        {
            _navigation.GoBack();
        }
    }

    private async Task ShowLoginRequiredAsync()
    {
        ViewModel?.CancelSessionCheck();
        ContentDialog dialog = ContentDialogFactory.Create(
            XamlRoot,
            d =>
            {
                d.Title = _strings.GetString("DownloadBase_LoginHintTitle");
                d.Content = _strings.GetString("Download_LoginRequired");
                d.PrimaryButtonText = _strings.GetString("DownloadBase_GoLogin");
                d.CloseButtonText = _strings.GetString("DownloadBase_Later");
                d.DefaultButton = ContentDialogButton.Primary;
            }
        );

        if (await dialog.ShowAsync() != ContentDialogResult.Primary)
        {
            return;
        }

        DependencyObject? walker = this;
        while (walker is not null)
        {
            walker = VisualTreeHelper.GetParent(walker);
            if (walker is DownloadPage host)
            {
                host.ShowAccountSection();
                break;
            }
        }

        _navigation.GoBack();
    }
}
