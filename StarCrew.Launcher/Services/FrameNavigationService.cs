using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using StarCrew.Launcher.Models;
using StarCrew.Launcher.ViewModels;
using StarCrew.Launcher.Views;

namespace StarCrew.Launcher.Services;

/// <summary>基于 Frame 的导航实现：主内容帧与设置分区帧共用一套目标映射。</summary>
internal sealed class FrameNavigationService : INavigationService
{
    private readonly IServiceProvider _services;
    private Frame? _mainFrame;
    private Frame? _sectionFrame;
    private NavigationTarget? _mainTarget;
    private NavigationTarget? _sectionTarget;

    public FrameNavigationService(IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);
        _services = services;
    }

    /// <summary>挂接外层内容帧，由主窗口在初始化时设置。</summary>
    public void AttachMainFrame(Frame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);
        _mainFrame = frame;
    }

    /// <summary>挂接设置页分区帧；重新挂接会重置分区去重状态（每次进入设置都是新页面）。</summary>
    public void AttachSectionFrame(Frame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);
        _sectionFrame = frame;
        _sectionTarget = null;
    }

    /// <summary>分区帧离开视觉树后解除挂接，避免持有已废弃页面。</summary>
    public void DetachSectionFrame()
    {
        _sectionFrame = null;
        _sectionTarget = null;
    }

    /// <inheritdoc />
    public void NavigateTo(NavigationTarget target) => NavigateTo(target, null);

    /// <summary>带过渡信息的导航，仅供界面层在 NavigationView 选中事件里使用。</summary>
    public void NavigateTo(NavigationTarget target, NavigationTransitionInfo? transitionInfo)
    {
        // 同目标重复导航会把进行中的进场过渡顶掉，内容区 visually 卡空白，故按帧去重。
        if (target is NavigationTarget.Appearance or NavigationTarget.About)
        {
            if (_sectionTarget == target)
            {
                return;
            }

            _sectionTarget = target;
            Navigate(Require(_sectionFrame, target), target, transitionInfo);
            return;
        }

        if (_mainTarget == target)
        {
            return;
        }

        _mainTarget = target;
        Navigate(Require(_mainFrame, target), target, transitionInfo);
    }

    private void Navigate(
        Frame frame,
        NavigationTarget target,
        NavigationTransitionInfo? transitionInfo
    )
    {
        (Type pageType, Type? viewModelType) = Map(target);
        object? viewModel = viewModelType is null
            ? null
            : _services.GetRequiredService(viewModelType);
        if (transitionInfo is null)
        {
            frame.Navigate(pageType, viewModel);
        }
        else
        {
            frame.Navigate(pageType, viewModel, transitionInfo);
        }
    }

    private static Frame Require(Frame? frame, NavigationTarget target) =>
        frame ?? throw new InvalidOperationException($"导航目标 {target} 的宿主帧尚未挂接。");

    private static (Type PageType, Type? ViewModelType) Map(NavigationTarget target) =>
        target switch
        {
            NavigationTarget.Home => (typeof(HomePage), typeof(HomeViewModel)),
            NavigationTarget.Settings => (typeof(SettingsPage), null),
            NavigationTarget.Appearance => (typeof(AppearancePage), typeof(AppearanceViewModel)),
            NavigationTarget.About => (typeof(AboutPage), typeof(AboutViewModel)),
            _ => throw new ArgumentOutOfRangeException(nameof(target), target, "未知的导航目标。"),
        };
}
