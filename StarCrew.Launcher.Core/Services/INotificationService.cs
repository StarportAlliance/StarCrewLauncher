using StarCrew.Launcher.Models;

namespace StarCrew.Launcher.Services;

/// <summary>Toast 通知服务。</summary>
internal interface INotificationService
{
    /// <summary>界面层订阅此事件弹 Toast。</summary>
    event EventHandler<NotificationMessage>? Requested;

    /// <summary>发送一条 Toast 通知。</summary>
    /// <param name="title">通知标题。</param>
    /// <param name="message">通知正文，可为 null。</param>
    /// <param name="severity">通知级别。</param>
    /// <param name="action">可选的行动按钮，为 null 时不显示按钮。</param>
    void Notify(
        string title,
        string? message,
        NotificationSeverity severity,
        NotificationAction? action = null
    );
}
