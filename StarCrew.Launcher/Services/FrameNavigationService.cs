using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using StarCrew.Launcher.Models;
using StarCrew.Launcher.ViewModels;
using StarCrew.Launcher.Views;

namespace StarCrew.Launcher.Services;

/// <summary>基于 Frame 的导航实现：主内容帧与设置分区帧共用一套目标映射。</summary>
internal sealed class FrameNavigationService : INavigationService
{
    /// <summary>主内容帧实际发生导航后触发；宿主窗口据此同步侧栏选中态（程序化导航与返回都不经过点击）。</summary>
    internal event Action<NavigationTarget>? MainFrameNavigated;

    /// <summary>设置分区帧实际发生导航后触发；设置页据此同步内层侧栏选中态（返回键改选中态时不经过点击）。</summary>
    internal event Action<NavigationTarget>? SectionFrameNavigated;

    private readonly IServiceProvider _services;
    private Frame? _mainFrame;
    private Frame? _sectionFrame;
    private NavigationTarget? _mainTarget;
    private NavigationTarget? _sectionTarget;
    private NavigationTarget? _pendingSection;

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
        _mainFrame.Navigated += OnMainFrameNavigated;
    }

    /// <summary>挂接设置分区帧；重新挂接会重置分区去重状态（每次进入设置都是新页面）。</summary>
    public void AttachSectionFrame(Frame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);
        if (!ReferenceEquals(_sectionFrame, frame))
        {
            if (_sectionFrame is not null)
            {
                _sectionFrame.Navigated -= OnSectionFrameNavigated;
            }

            _sectionFrame = frame;
            _sectionFrame.Navigated += OnSectionFrameNavigated;
        }

        _sectionTarget = null;
    }

    /// <summary>分区帧离开视觉树后解除挂接，避免持有已废弃页面。</summary>
    public void DetachSectionFrame()
    {
        if (_sectionFrame is not null)
        {
            _sectionFrame.Navigated -= OnSectionFrameNavigated;
        }

        _sectionFrame = null;
        _sectionTarget = null;
    }

    /// <summary>取出并清除待进入的设置分区；由设置页加载时消费，没有待进入目标时返回 null。</summary>
    public NavigationTarget? TakePendingSection()
    {
        NavigationTarget? pending = _pendingSection;
        _pendingSection = null;
        return pending;
    }

    /// <inheritdoc />
    public void NavigateTo(NavigationTarget target) => NavigateTo(target, null, null);

    /// <summary>带过渡信息的导航，仅供界面层在 NavigationView 选中事件里使用。</summary>
    public void NavigateTo(NavigationTarget target, NavigationTransitionInfo? transitionInfo) =>
        NavigateTo(target, null, transitionInfo);

    /// <summary>带页面参数的导航；parameter 只对无自己视图模型的目标生效（安装详情页复用下载页视图模型）。</summary>
    public void NavigateTo(
        NavigationTarget target,
        object? parameter,
        NavigationTransitionInfo? transitionInfo
    )
    {
        // 同目标重复导航会把进行中的进场过渡顶掉，内容区 visually 卡空白，故按帧去重。
        if (
            target
            is NavigationTarget.Appearance
                or NavigationTarget.Network
                or NavigationTarget.About
        )
        {
            if (_sectionFrame is null)
            {
                // 人还在设置页外：记住目标分区并先落到设置主页，由设置页加载后补导航。
                _pendingSection = target;
                if (_mainTarget != NavigationTarget.Settings)
                {
                    NavigateTo(NavigationTarget.Settings, null, transitionInfo);
                }

                return;
            }

            if (_sectionTarget == target)
            {
                return;
            }

            NavigateSection(target, transitionInfo);
            return;
        }

        if (_mainTarget == target)
        {
            return;
        }

        NavigateMain(target, parameter, transitionInfo);
    }

    /// <summary>TitleBar 返回：设置分区内先退分区，退到底才退主页面；无历史可退时什么都不做。</summary>
    public void GoBack()
    {
        if (_sectionFrame?.CanGoBack == true)
        {
            _sectionFrame.GoBack();
            return;
        }

        if (_mainFrame?.CanGoBack == true)
        {
            _mainFrame.GoBack();
        }
    }

    /// <summary>可返回历史是否非空：TitleBar 返回按钮据此切换可用态。</summary>
    public bool CanGoBack => _sectionFrame?.CanGoBack == true || _mainFrame?.CanGoBack == true;

    // 动画方向不归应用侧：Frame.GoBack 自动取该页进场用过的过渡反向播放，返回只需选退哪一帧。
    // Navigated 在 Navigate/GoBack 内同步触发，是前进与返回共用的落点：目标态只在这里更新。
    private void OnMainFrameNavigated(object sender, NavigationEventArgs e) =>
        ApplyNavigated(e, ref _mainTarget, MainFrameNavigated);

    private void OnSectionFrameNavigated(object sender, NavigationEventArgs e) =>
        ApplyNavigated(e, ref _sectionTarget, SectionFrameNavigated);

    private static void ApplyNavigated(
        NavigationEventArgs e,
        ref NavigationTarget? current,
        Action<NavigationTarget>? navigated
    )
    {
        NavigationTarget target = TargetOf(e.SourcePageType);
        current = target;
        navigated?.Invoke(target);
    }

    private void NavigateMain(
        NavigationTarget target,
        object? parameter,
        NavigationTransitionInfo? transitionInfo
    )
    {
        // 待进入分区只在离开设置时作废：跳分区链路正是先 NavigateMain(Settings)，无条件清会把自己刚记下的目标擦掉。
        if (target != NavigationTarget.Settings)
        {
            _pendingSection = null;
        }

        Navigate(Require(_mainFrame, target), target, parameter, transitionInfo);
    }

    private void NavigateSection(NavigationTarget target, NavigationTransitionInfo? transitionInfo)
    {
        Navigate(Require(_sectionFrame, target), target, null, transitionInfo);
    }

    private void Navigate(
        Frame frame,
        NavigationTarget target,
        object? parameter,
        NavigationTransitionInfo? transitionInfo
    )
    {
        (Type pageType, Type? viewModelType) = Map(target);
        // 无自己视图模型的目标（安装详情页）直接收调用方传进来的参数，其余仍由 DI 解析。
        object? viewModel = viewModelType is null
            ? parameter
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
            NavigationTarget.Instances => (typeof(InstancesPage), typeof(InstancesViewModel)),
            NavigationTarget.Download => (typeof(DownloadPage), typeof(DownloadPageViewModel)),
            NavigationTarget.Install => (typeof(DownloadInstallPage), null),
            NavigationTarget.Appearance => (typeof(AppearancePage), typeof(AppearanceViewModel)),
            NavigationTarget.Network => (typeof(NetworkPage), typeof(NetworkViewModel)),
            NavigationTarget.About => (typeof(AboutPage), typeof(AboutViewModel)),
            _ => throw new ArgumentOutOfRangeException(nameof(target), target, "未知的导航目标。"),
        };

    // 页面类型反查目标；Frame.Navigated 只给页面类型，返回后所在页靠它分辨。
    // 与 Map 一一对应，加页面时两处都要补（漏了这里会直接抛）。
    private static NavigationTarget TargetOf(Type pageType) =>
        pageType switch
        {
            _ when pageType == typeof(HomePage) => NavigationTarget.Home,
            _ when pageType == typeof(SettingsPage) => NavigationTarget.Settings,
            _ when pageType == typeof(InstancesPage) => NavigationTarget.Instances,
            _ when pageType == typeof(DownloadPage) => NavigationTarget.Download,
            _ when pageType == typeof(DownloadInstallPage) => NavigationTarget.Install,
            _ when pageType == typeof(AppearancePage) => NavigationTarget.Appearance,
            _ when pageType == typeof(NetworkPage) => NavigationTarget.Network,
            _ when pageType == typeof(AboutPage) => NavigationTarget.About,
            _ => throw new ArgumentOutOfRangeException(
                nameof(pageType),
                pageType,
                "未知的页面类型。"
            ),
        };
}
