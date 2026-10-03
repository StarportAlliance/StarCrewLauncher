namespace StarCrew.Launcher.Models;

/// <summary>更新检查结果：有更新带版本号，失败带原因。</summary>
internal sealed record UpdateCheckResult(
    UpdateState State,
    string? AvailableVersion,
    string Message
);
