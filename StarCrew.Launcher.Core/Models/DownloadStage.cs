namespace StarCrew.Launcher.Models;

/// <summary>游戏下载各阶段状态。</summary>
internal enum DownloadStage
{
    /// <summary>正在安装/校验下载工具。</summary>
    PreparingTool,

    /// <summary>正在拉取游戏文件。</summary>
    DownloadingGame,

    /// <summary>正在校验游戏文件。</summary>
    Verifying,

    /// <summary>下载完成。</summary>
    Completed,

    /// <summary>下载失败。</summary>
    Failed,
}
