using CommunityToolkit.Mvvm.Input;
using StarCrew.Launcher.Models;
using StarCrew.Launcher.Services;

namespace StarCrew.Launcher.ViewModels;

/// <summary>列表中每个实例的视图模型：启动、打开目录、重命名/删除/详情请求。</summary>
internal sealed partial class InstanceItemViewModel
{
    private readonly IInstanceLauncher _launcher;
    private readonly IExternalLauncher _externalLauncher;
    private readonly INotificationService _notifications;

    public InstanceItemViewModel(
        GameInstance instance,
        IInstanceLauncher launcher,
        IExternalLauncher externalLauncher,
        INotificationService notifications
    )
    {
        ArgumentNullException.ThrowIfNull(instance);
        ArgumentNullException.ThrowIfNull(launcher);
        ArgumentNullException.ThrowIfNull(externalLauncher);
        ArgumentNullException.ThrowIfNull(notifications);
        Instance = instance;
        _launcher = launcher;
        _externalLauncher = externalLauncher;
        _notifications = notifications;
    }

    public GameInstance Instance { get; private set; }

    public string Name => Instance.Name;

    public string Summary =>
        $"v{Instance.GameVersionLabel} · {(Instance.State == InstanceState.Ready ? "就绪" : "未完成")} · {Instance.InstallDirectory}";

    public bool CanLaunch => Instance.State == InstanceState.Ready;

    public event EventHandler<GameInstance>? RenameRequested;

    public event EventHandler<GameInstance>? DeleteRequested;

    public event EventHandler<GameInstance>? DetailsRequested;

    [RelayCommand]
    private async Task LaunchAsync()
    {
        if (!CanLaunch)
        {
            _notifications.Notify(
                "无法启动：该实例尚未就绪，请先完成安装。",
                null,
                NotificationSeverity.Warning
            );
            return;
        }

        try
        {
            LaunchOutcome outcome = await _launcher.LaunchAsync(Instance);
            _notifications.Notify(
                outcome.Message,
                null,
                outcome.IsSuccess ? NotificationSeverity.Success : NotificationSeverity.Error
            );
        }
        catch (OperationCanceledException)
        {
            throw;
        }
    }

    [RelayCommand]
    private async Task OpenFolderAsync()
    {
        try
        {
            await _externalLauncher.OpenFolderAsync(Instance.InstallDirectory);
        }
        catch (Exception ex)
        {
            _notifications.Notify($"打开失败：{ex.Message}", null, NotificationSeverity.Error);
        }
    }

    [RelayCommand]
    private void RequestRename() => RenameRequested?.Invoke(this, Instance);

    [RelayCommand]
    private void RequestDelete() => DeleteRequested?.Invoke(this, Instance);

    [RelayCommand]
    private void RequestDetails() => DetailsRequested?.Invoke(this, Instance);
}
