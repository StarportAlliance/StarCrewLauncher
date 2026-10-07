namespace StarCrew.Launcher.Models;

/// <summary>一条应用内通知：标题、正文与级别。</summary>
internal sealed record NotificationMessage(
    string Title,
    string Message,
    NotificationSeverity Severity
);
