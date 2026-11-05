using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using StarportAlliance.StarCrewLauncher.Models;
using StarportAlliance.StarCrewLauncher.Services;
using StarportAlliance.StarCrewLauncher.ViewModels;

namespace StarportAlliance.StarCrewLauncher.Views;

public sealed partial class DownloadBasePage : Page
{
    private readonly IAppStrings _strings = App.Services.GetRequiredService<IAppStrings>();
    private readonly FrameNavigationService _navigation =
        App.Services.GetRequiredService<FrameNavigationService>();

    public DownloadBasePage()
    {
        InitializeComponent();
    }

    internal DownloadPageViewModel? ViewModel { get; private set; }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        ViewModel = e.Parameter as DownloadPageViewModel;
        Bindings.Update();

        if (ViewModel is not null && string.IsNullOrWhiteSpace(ViewModel.AccountName))
        {
            Loaded += OnLoadedShowLoginHint;
        }
    }

    private async void VersionCard_Click(object sender, RoutedEventArgs e)
    {
        _ = e;
        if (ViewModel is null || sender is not FrameworkElement { Tag: VersionItemViewModel item })
        {
            return;
        }

        if (!ViewModel.SelectVersion(item))
        {
            await ShowLoginHintAsync();
            return;
        }

        _navigation.NavigateTo(
            NavigationTarget.Install,
            ViewModel,
            new SlideNavigationTransitionInfo { Effect = SlideNavigationTransitionEffect.FromRight }
        );
    }

    private void OnLoadedShowLoginHint(object sender, RoutedEventArgs e)
    {
        _ = sender;
        _ = e;
        Loaded -= OnLoadedShowLoginHint;
        _ = ShowLoginHintAfterTransitionAsync();
    }

    private async Task ShowLoginHintAfterTransitionAsync()
    {
        await Task.Delay(400);
        if (ViewModel is not null && string.IsNullOrWhiteSpace(ViewModel.AccountName))
        {
            await ShowLoginHintAsync();
        }
    }

    private async Task ShowLoginHintAsync()
    {
        ContentDialog dialog = ContentDialogFactory.Create(
            XamlRoot,
            d =>
            {
                d.Title = _strings.GetString("DownloadBase_LoginHintTitle");
                d.Content = _strings.GetString("DownloadBase_LoginHintContent");
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
                return;
            }
        }
    }
}
