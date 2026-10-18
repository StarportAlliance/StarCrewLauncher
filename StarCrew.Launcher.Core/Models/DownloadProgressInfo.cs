namespace StarCrew.Launcher.Models;

/// <summary>下载进度片段。</summary>
/// <param name="Percent">进度百分比。</param>
internal sealed record DownloadProgressInfo(
    DownloadStage Stage,
    double Percent,
    string Message,
    long? TotalBytes = null
);
