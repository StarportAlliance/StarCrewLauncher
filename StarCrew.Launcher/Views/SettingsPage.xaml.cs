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
        _ = sender;
        _ = e;
        _initialNavigated = true;
        _navigation.AttachSectionFrame(SectionFrame);
        _navigation.SectionFrameNavigated += SyncSectionSelection;
        NavigationTarget target = _navigation.TakePendingSection() ?? NavigationTarget.Appearance;
        _navigation.NavigateTo(target, new SuppressNavigationTransitionInfo());
        SyncSectionSelection(target);
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _navigation.SectionFrameNavigated -= SyncSectionSelection;
        _navigation.DetachSectionFrame();
    }

    private void SyncSectionSelection(NavigationTarget target)
    {
        string? tag = target switch
        {
            NavigationTarget.Appearance => "Appearance",
            NavigationTarget.About => "About",
            _ => null,
        };
        foreach (object item in SectionNav.MenuItems)
        {
            if (
                item is NavigationViewItem { Tag: string itemTag }
                && string.Equals(itemTag, tag, StringComparison.Ordinal)
            )
            {
                SectionNav.SelectedItem = item;
                return;
            }
        }
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
