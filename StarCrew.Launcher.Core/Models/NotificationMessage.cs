namespace StarCrew.Launcher.Models;

/// <summary>一条应用内通知：标题（加粗单行）、正文（可为空：与标题重复时省略）。</summary>
internal sealed record NotificationMessage(
    string Title,
    string? Message,
    NotificationSeverity Severity
);
