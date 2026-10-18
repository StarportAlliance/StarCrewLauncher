namespace StarCrew.Launcher.Models;

/// <summary>游戏安装结果。</summary>
internal sealed record GameInstallResult(
    bool IsSuccess,
    string Message,
    GameInstance? Instance = null
);
