namespace StarCrew.Launcher.Models;

/// <summary>更新检查的 settled 结果状态，UI 按状态决定展示文案与按钮。</summary>
internal enum UpdateState
{
    /// <summary>已是最新版本，无需任何操作。</summary>
    UpToDate,

    /// <summary>新版本已下载并安排应用，重启后生效。</summary>
    ReadyToRestart,

    /// <summary>当前未经 Velopack 安装（如开发调试直跑），无法检查更新。</summary>
    NotInstalled,

    /// <summary>检查或下载失败，详情见结果消息。</summary>
    Failed,
}
