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
        // 返回键回退分区时也会走到这里：把内层侧栏选中态对齐到实际所在分区。
        _navigation.SectionFrameNavigated += SyncSectionSelection;
        // 从页外直接跳某个分区时，先进设置主页再补导航到目标分区；侧栏选中态一并同步。
        NavigationTarget target = _navigation.TakePendingSection() ?? NavigationTarget.Appearance;
        _navigation.NavigateTo(target, new SuppressNavigationTransitionInfo());
        SyncSectionSelection(target);
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _navigation.SectionFrameNavigated -= SyncSectionSelection;
        _navigation.DetachSectionFrame();
    }

    /// <summary>把内层侧栏选中项对齐到当前分区；事件里的重复导航由导航服务按帧去重。</summary>
    private void SyncSectionSelection(NavigationTarget target)
    {
        string? tag = target switch
        {
            NavigationTarget.Appearance => "Appearance",
            NavigationTarget.Network => "Network",
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
            "Network" => NavigationTarget.Network,
            "About" => NavigationTarget.About,
            _ => null,
        };
        if (target is not null)
        {
            _navigation.NavigateTo(target.Value, args.RecommendedNavigationTransitionInfo);
        }
    }
}
