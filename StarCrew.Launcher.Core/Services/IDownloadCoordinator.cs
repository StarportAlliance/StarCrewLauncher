using StarCrew.Launcher.Models;

namespace StarCrew.Launcher.Services;

/// <summary>游戏安装协调器。</summary>
internal interface IDownloadCoordinator
{
    /// <summary>下载并安装一个新的 Among Us 实例。</summary>
    Task<GameInstallResult> InstallAsync(
        string instanceName,
        GameVersionEntry version,
        IProgress<DownloadProgressInfo>? progress = null,
        CancellationToken cancellationToken = default
    );
}
