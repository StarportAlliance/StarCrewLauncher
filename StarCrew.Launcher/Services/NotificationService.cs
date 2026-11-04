using StarCrew.Launcher.Models;

namespace StarCrew.Launcher.Services;

internal sealed class NotificationService : INotificationService
{
    public event EventHandler<NotificationMessage>? Requested;

    public void Notify(
        string title,
        string? message,
        NotificationSeverity severity,
        NotificationAction? action = null
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        Requested?.Invoke(this, new NotificationMessage(title, message, severity, action));
    }
}
