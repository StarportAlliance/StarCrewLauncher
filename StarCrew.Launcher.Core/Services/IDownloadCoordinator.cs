using StarCrew.Launcher.Models;

namespace StarCrew.Launcher.Services;

/// <summary>下载协调者：版本清点 → 创建实例目录 → 执行 DD → 状态落库。</summary>
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
