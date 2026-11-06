namespace StarportAlliance.StarCrewLauncher.Services;

/// <summary>Steam 客户端相关服务。</summary>
internal interface ISteamClient
{
    /// <summary>Steam 客户端是否正在运行。</summary>
    bool IsSteamRunning();

    /// <summary>拉起 Steam 客户端。</summary>
    /// <returns>是否成功拉起。</returns>
    bool TryStartSteam();
}
