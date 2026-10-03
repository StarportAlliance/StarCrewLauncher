namespace StarCrew.Launcher.Models;

/// <summary>更新检查结果，有更新时携带新版本号，失败时携带原因。</summary>
/// <param name="State">更新状态。</param>
/// <param name="AvailableVersion">发现的新版本号，无更新或失败时为 null。</param>
/// <param name="Message">面向用户展示的结果描述。</param>
internal sealed record UpdateCheckResult(
    UpdateState State,
    string? AvailableVersion,
    string Message
);
