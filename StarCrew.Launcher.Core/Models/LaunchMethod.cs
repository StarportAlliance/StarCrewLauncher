namespace StarCrew.Launcher.Models;

/// <summary>实际生效的游戏启动方式。</summary>
internal enum LaunchMethod
{
    /// <summary>通过 Steam 协议拉起。</summary>
    SteamProtocol,

    /// <summary>直接启动 Among Us.exe。</summary>
    DirectExe,

    /// <summary>未能启动。</summary>
    None,
}
