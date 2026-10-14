namespace StarCrew.Launcher.Services;

/// <summary>Steam 客户端接缝：提供“检测是否在运行 / 尝试静默启动”的能力。</summary>
internal interface ISteamClient
{
    /// <summary>Steam 客户端是否正在运行。</summary>
    bool IsSteamRunning();

    /// <summary>静默拉起 Steam 客户端。失败时返回 false 并输出错误原因。</summary>
    bool TryStartSteam(out string? error);
}
