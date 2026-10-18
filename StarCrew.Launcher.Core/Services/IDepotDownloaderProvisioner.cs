using StarCrew.Launcher.Models;

namespace StarCrew.Launcher.Services;

/// <summary>DepotDownloader 分发员。</summary>
internal interface IDepotDownloaderProvisioner
{
    /// <summary>组件是否已就绪。</summary>
    bool IsInstalled();

    /// <summary>确保 DepotDownloader 可用。</summary>
    /// <remarks>当 DepotDownloader 不可用时尝试自动下载并安装它。</remarks>
    /// <returns>成功时带工具路径。</returns>
    Task<DepotDownloaderEnsureResult> EnsureAsync(
        IProgress<DownloadProgressInfo>? progress = null,
        CancellationToken cancellationToken = default
    );
}

/// <summary>DepotDownloader 确保安装的结果。</summary>
internal sealed record DepotDownloaderEnsureResult(
    bool IsSuccess,
    string Message,
    string? ExecutablePath
);
