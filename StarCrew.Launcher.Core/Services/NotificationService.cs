using StarCrew.Launcher.Models;

namespace StarCrew.Launcher.Services;

/// <summary>默认通知服务：把通知原样转发给订阅者（窗口外壳）。</summary>
internal sealed class NotificationService : INotificationService
{
    public event EventHandler<NotificationMessage>? Requested;

    public void Notify(string title, string? message, NotificationSeverity severity)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        Requested?.Invoke(this, new NotificationMessage(title, message, severity));
    }
}
