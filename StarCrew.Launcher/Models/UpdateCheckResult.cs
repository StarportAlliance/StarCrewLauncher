namespace StarCrew.Launcher.Models;

/// <summary>更新检查结果。</summary>
internal sealed record UpdateCheckResult(
    UpdateState State,
    string? AvailableVersion,
    string Message
);
