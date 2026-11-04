namespace StarCrew.Launcher.Services;

/// <summary>Steam 客户端相关服务。</summary>
internal interface ISteamClient
{
    /// <summary>Steam 客户端是否正在运行。</summary>
    bool IsSteamRunning();

    /// <summary>拉起 Steam 客户端。</summary>
    /// <returns>是否成功拉起。</returns>
    /// <param name="error">失败时输出的错误原因。</param>
    bool TryStartSteam(out string? error);
}
