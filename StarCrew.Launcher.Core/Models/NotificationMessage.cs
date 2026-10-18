namespace StarCrew.Launcher.Models;

/// <summary>Toast 通知消息。</summary>
internal sealed record NotificationMessage(
    string Title,
    string? Message,
    NotificationSeverity Severity
);
