namespace StarCrew.Launcher.Models;

/// <summary>实例启动结果。</summary>
internal sealed record LaunchOutcome(bool IsSuccess, string Message);
