namespace StarCrew.Launcher.Models;

/// <summary>更新检查的 settled 状态，UI 按状态提示用户。</summary>
internal enum UpdateState
{
    /// <summary>已是最新，无需操作。</summary>
    UpToDate,

    /// <summary>新版本已下载，重启后生效。</summary>
    ReadyToRestart,

    /// <summary>未经 Velopack 安装（如开发直跑），无法检查更新。</summary>
    NotInstalled,

    /// <summary>检查或下载失败，详情见结果消息。</summary>
    Failed,
}
