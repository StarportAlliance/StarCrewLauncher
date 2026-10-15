using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StarCrew.Launcher.Models;
using StarCrew.Launcher.Services;

namespace StarCrew.Launcher.ViewModels;

/// <summary>首页视图模型：列出/选择实例，启动选中的实例；无实例时引导去下载。</summary>
internal sealed partial class HomeViewModel : ObservableObject
{
    private readonly IInstanceStore _store;
    private readonly IInstanceLauncher _launcher;
    private readonly INavigationService _navigation;
    private readonly INotificationService _notifications;
    private readonly IAppStrings _strings;

    public HomeViewModel(
        IInstanceStore store,
        IInstanceLauncher launcher,
        INavigationService navigation,
        INotificationService notifications,
        IAppStrings strings
    )
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(launcher);
        ArgumentNullException.ThrowIfNull(navigation);
        ArgumentNullException.ThrowIfNull(notifications);
        ArgumentNullException.ThrowIfNull(strings);
        _store = store;
        _launcher = launcher;
        _navigation = navigation;
        _notifications = notifications;
        _strings = strings;

        LauncherState state = _store.Load();
        Instances = state.Instances;
        GameInstance? lastUsed = null;
        if (state.Instances.Count > 0)
        {
            lastUsed =
                state.Instances.FirstOrDefault(i => i.Id == state.LastInstanceId)
                ?? state.Instances[0];
        }

        SelectedInstance = lastUsed;
    }

    /// <summary>全部实例。</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasInstances))]
    [NotifyPropertyChangedFor(nameof(LaunchButtonText))]
    public partial IReadOnlyList<GameInstance> Instances { get; set; }

    /// <summary>当前选中的实例（实例切换器选中的项）。</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CurrentInstanceName))]
    public partial GameInstance? SelectedInstance { get; set; }

    /// <summary>是否有任何实例（决定按钮文案与启动方式）。</summary>
    public bool HasInstances => Instances.Count > 0;

    /// <summary>启动按钮文案。</summary>
    public string LaunchButtonText =>
        HasInstances
            ? _strings.GetString("Home_LaunchGame")
            : _strings.GetString("Home_DownloadGame");

    /// <summary>首页左下角的当前实例名，没有实例时为空串（该处标题不显示）。</summary>
    public string CurrentInstanceName => SelectedInstance?.Name ?? string.Empty;

    [RelayCommand]
    private async Task LaunchAsync()
    {
        if (!HasInstances)
        {
            _navigation.NavigateTo(NavigationTarget.Download);
            return;
        }

        GameInstance target = SelectedInstance ?? Instances[0];
        SelectedInstance = target;

        LaunchOutcome outcome = await _launcher.LaunchAsync(target);
        if (outcome.IsSuccess)
        {
            _notifications.Notify(outcome.Message, null, NotificationSeverity.Success);
            PersistLaunch(target.Id);
        }
        else
        {
            _notifications.Notify(outcome.Message, null, NotificationSeverity.Error);
        }
    }

    partial void OnSelectedInstanceChanged(GameInstance? value)
    {
        // 切换选中项时持久化，下次打开启动器默认选中同一个实例
        if (value is not null)
        {
            LauncherState state = _store.Load();
            if (state.LastInstanceId != value.Id)
            {
                _store.Save(state with { LastInstanceId = value.Id });
            }
        }
    }

    // 启动成功后刷新该实例的最后启动时间并回写状态
    private void PersistLaunch(Guid instanceId)
    {
        LauncherState state = _store.Load();
        List<GameInstance> updated = state
            .Instances.Select(i =>
                i.Id == instanceId ? i with { LastLaunchedAt = DateTimeOffset.UtcNow } : i
            )
            .ToList();
        _store.Save(state with { LastInstanceId = instanceId, Instances = updated });

        Instances = updated;
        SelectedInstance = updated.FirstOrDefault(i => i.Id == instanceId);
    }
}
