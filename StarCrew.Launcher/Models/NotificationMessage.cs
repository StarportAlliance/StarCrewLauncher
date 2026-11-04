namespace StarCrew.Launcher.Models;

/// <summary>Toast 通知消息。</summary>
internal sealed record NotificationMessage(
    string Title,
    string? Message,
    NotificationSeverity Severity,
    NotificationAction? Action = null
);

/// <summary>Toast 上的操作按钮。</summary>
/// <param name="Label">按钮文案。</param>
/// <param name="Callback">点击后执行的操作。</param>
internal sealed record NotificationAction(string Label, Action Callback);
