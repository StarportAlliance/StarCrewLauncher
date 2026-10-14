namespace StarCrew.Launcher.Models;

/// <summary>下载进度片段：阶段、百分比、一行描述与总字节数（null = 总大小还没算出来，界面此后退回只显示百分比）。</summary>
internal sealed record DownloadProgressInfo(
    DownloadStage Stage,
    double Percent,
    string Message,
    long? TotalBytes = null
);
