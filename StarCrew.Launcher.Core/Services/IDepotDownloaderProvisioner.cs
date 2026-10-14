using StarCrew.Launcher.Models;

namespace StarCrew.Launcher.Services;

/// <summary>DepotDownloader 分发员：负责首次可用化（下载+校验+解压）。</summary>
internal interface IDepotDownloaderProvisioner
{
    /// <summary>组件是否已就绪（可执行文件在位）；用于界面层展示状态，不触发下载。</summary>
    bool IsInstalled();

    /// <summary>确保 DepotDownloader 可用；成功返回工具路径，失败返回 false。下载进度经 progress 上报。</summary>
    Task<DepotDownloaderEnsureResult> EnsureAsync(
        IProgress<DownloadProgressInfo>? progress = null,
        CancellationToken cancellationToken = default
    );
}

/// <summary>EnsureAsync 的结果：成功与否、提示文本与最终可执行路径。</summary>
internal sealed record DepotDownloaderEnsureResult(
    bool IsSuccess,
    string Message,
    string? ExecutablePath
);
