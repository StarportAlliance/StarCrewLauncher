using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using StarCrew.Launcher.ViewModels;

namespace StarCrew.Launcher.Views;

public sealed partial class DownloadPage : Page
{
    public DownloadPage()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    internal DownloadPageViewModel? ViewModel { get; private set; }

    private Type? _currentSectionPageType;
    private bool _initialNavigated;

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        ViewModel = e.Parameter as DownloadPageViewModel;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _ = sender;
        _ = e;
        _initialNavigated = true;
        SectionFrame_Navigate(typeof(DownloadBasePage), new SuppressNavigationTransitionInfo());
    }

    private void SectionNav_SelectionChanged(
        NavigationView sender,
        NavigationViewSelectionChangedEventArgs args
    )
    {
        _ = sender;
        if (!_initialNavigated || args.SelectedItem is not NavigationViewItem selected)
        {
            return;
        }

        Type pageType = string.Equals(selected.Tag as string, "Account", StringComparison.Ordinal)
            ? typeof(DownloadAccountPage)
            : typeof(DownloadBasePage);
        SectionFrame_Navigate(pageType, args.RecommendedNavigationTransitionInfo);
    }

    private void SectionFrame_Navigate(Type pageType, NavigationTransitionInfo transitionInfo)
    {
        if (Equals(_currentSectionPageType, pageType))
        {
            return;
        }

        _currentSectionPageType = pageType;
        SectionFrame.Navigate(pageType, ViewModel, transitionInfo);
    }

    internal void ShowAccountSection()
    {
        foreach (object item in SectionNav.FooterMenuItems)
        {
            if (
                item is NavigationViewItem { Tag: string tag }
                && string.Equals(tag, "Account", StringComparison.Ordinal)
            )
            {
                SectionNav.SelectedItem = item;
                return;
            }
        }
    }
}
