namespace StarCrew.Launcher.Models;

/// <summary>应用内通知级别，UI 层映射到 InfoBarSeverity。</summary>
internal enum NotificationSeverity
{
    /// <summary>普通信息提示。</summary>
    Informational,

    /// <summary>操作成功。</summary>
    Success,

    /// <summary>需要留意但不影响继续。</summary>
    Warning,

    /// <summary>操作失败。</summary>
    Error,
}
