using StarCrew.Launcher.Models;

namespace StarCrew.Launcher.Services;

/// <summary>Toast 通知服务。</summary>
internal interface INotificationService
{
    /// <summary>界面层订阅此事件弹 Toast。</summary>
    event EventHandler<NotificationMessage>? Requested;

    /// <summary>发送一条 Toast 通知。</summary>
    void Notify(string title, string? message, NotificationSeverity severity);
}
