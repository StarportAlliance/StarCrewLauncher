using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using StarCrew.Launcher.Models;
using StarCrew.Launcher.Services;

namespace StarCrew.Launcher.Views;

public sealed partial class SettingsPage : Page
{
    private readonly FrameNavigationService _navigation;
    private bool _initialNavigated;

    public SettingsPage()
    {
        InitializeComponent();
        _navigation = App.Services.GetRequiredService<FrameNavigationService>();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _initialNavigated = true;
        _navigation.AttachSectionFrame(SectionFrame);
        _navigation.NavigateTo(NavigationTarget.Appearance, new SuppressNavigationTransitionInfo());
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _navigation.DetachSectionFrame();
    }

    private void SectionNav_SelectionChanged(
        NavigationView sender,
        NavigationViewSelectionChangedEventArgs args
    )
    {
        if (!_initialNavigated || args.SelectedItem is not NavigationViewItem selected)
        {
            return;
        }

        NavigationTarget? target = selected.Tag switch
        {
            "Appearance" => NavigationTarget.Appearance,
            "About" => NavigationTarget.About,
            _ => null,
        };
        if (target is not null)
        {
            _navigation.NavigateTo(target.Value, args.RecommendedNavigationTransitionInfo);
        }
    }
}
