using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StarCrew.Launcher.Models;
using StarCrew.Launcher.Services;

namespace StarCrew.Launcher.ViewModels;

/// <summary>首页视图模型：自动启动失败时回退到手动选择 exe。</summary>
internal sealed partial class HomeViewModel : ObservableObject
{
    private readonly IGameLauncher _launcher;
    private readonly IFilePickerService _picker;
    private readonly INotificationService _notifications;

    public HomeViewModel(
        IGameLauncher launcher,
        IFilePickerService picker,
        INotificationService notifications
    )
    {
        ArgumentNullException.ThrowIfNull(launcher);
        ArgumentNullException.ThrowIfNull(picker);
        ArgumentNullException.ThrowIfNull(notifications);
        _launcher = launcher;
        _picker = picker;
        _notifications = notifications;
    }

    /// <summary>启动进行中，按钮文字与命令可用性都跟着它走。</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LaunchButtonText))]
    public partial bool IsLaunching { get; set; }

    /// <summary>启动按钮文案。</summary>
    public string LaunchButtonText => IsLaunching ? "正在启动..." : "启动游戏";

    [RelayCommand]
    private async Task LaunchAsync()
    {
        IsLaunching = true;

        try
        {
            LaunchResult result = await _launcher.LaunchAsync();
            if (result.IsSuccess)
            {
                _notifications.Notify("启动成功", result.Message, NotificationSeverity.Success);
                return;
            }

            await LaunchByManualPickAsync();
        }
        finally
        {
            IsLaunching = false;
        }
    }

    /// <summary>自动定位失败时让用户手动选 exe 并启动。</summary>
    private async Task LaunchByManualPickAsync()
    {
        string? picked = await _picker.PickGameExeAsync();
        if (picked is null)
        {
            _notifications.Notify(
                "已取消",
                "已取消选择，点击启动游戏可重试。",
                NotificationSeverity.Informational
            );
            return;
        }

        if (_launcher.TryLaunchExe(picked, out string? error))
        {
            _notifications.Notify(
                "启动成功",
                "已启动手动选择的游戏。",
                NotificationSeverity.Success
            );
            return;
        }

        _notifications.Notify(
            "启动失败",
            $"启动所选文件时发生错误：{error}",
            NotificationSeverity.Error
        );
    }
}
