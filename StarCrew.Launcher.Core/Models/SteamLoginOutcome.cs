namespace StarCrew.Launcher.Models;

/// <summary>Steam 账号登录结果。</summary>
internal sealed record SteamLoginOutcome(bool IsSuccess, string? AccountName, string? ErrorMessage);
