using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using StarCrew.Launcher.Models;
using StarCrew.Launcher.ViewModels;
using StarCrew.Launcher.Views;

namespace StarCrew.Launcher.Services;

internal sealed class FrameNavigationService : INavigationService
{
    internal event Action<NavigationTarget>? MainFrameNavigated;
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

    public void AttachMainFrame(Frame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);
        _mainFrame = frame;
        _mainFrame.Navigated += OnMainFrameNavigated;
    }

    public void AttachSectionFrame(Frame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);
        if (!ReferenceEquals(_sectionFrame, frame))
        {
            _sectionFrame?.Navigated -= OnSectionFrameNavigated;

            _sectionFrame = frame;
            _sectionFrame.Navigated += OnSectionFrameNavigated;
        }

        _sectionTarget = null;
    }

    public void DetachSectionFrame()
    {
        _sectionFrame?.Navigated -= OnSectionFrameNavigated;

        _sectionFrame = null;
        _sectionTarget = null;
    }

    /// <summary>取出并清除待进入的设置分区。</summary>
    /// <returns>待进入的设置分区，没有待进入目标时返回 null。</returns>
    public NavigationTarget? TakePendingSection()
    {
        NavigationTarget? pending = _pendingSection;
        _pendingSection = null;
        return pending;
    }

    public void NavigateTo(NavigationTarget target) => NavigateTo(target, null, null);

    /// <inheritdoc cref="NavigateTo(NavigationTarget, object?, NavigationTransitionInfo?)"/>
    public void NavigateTo(NavigationTarget target, NavigationTransitionInfo? transitionInfo) =>
        NavigateTo(target, null, transitionInfo);

    /// <summary>导航到指定 <paramref name="target"/>。</summary>
    /// <param name="target">导航目标。</param>
    /// <param name="parameter">导航参数，只对无自己视图模型的 <paramref name="target"/> 生效。</param>
    /// <param name="transitionInfo">导航过渡信息。</param>
    public void NavigateTo(
        NavigationTarget target,
        object? parameter,
        NavigationTransitionInfo? transitionInfo
    )
    {
        if (
            target
            is NavigationTarget.Appearance
                or NavigationTarget.Network
                or NavigationTarget.About
        )
        {
            if (_sectionFrame is null)
            {
                // 此时人还在设置页外，记住目标分区并先落到设置主页，由设置页加载后补导航
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

    public bool CanGoBack => _sectionFrame?.CanGoBack == true || _mainFrame?.CanGoBack == true;

    // 返回只需选退哪一帧
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
        // 待进入分区只在离开设置时作废
        if (target != NavigationTarget.Settings)
        {
            _pendingSection = null;
        }

        Navigate(Require(_mainFrame, target), target, parameter, transitionInfo);
    }

    private void NavigateSection(
        NavigationTarget target,
        NavigationTransitionInfo? transitionInfo
    ) => Navigate(Require(_sectionFrame, target), target, null, transitionInfo);

    private void Navigate(
        Frame frame,
        NavigationTarget target,
        object? parameter,
        NavigationTransitionInfo? transitionInfo
    )
    {
        (Type pageType, Type? viewModelType) = Map(target);
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
