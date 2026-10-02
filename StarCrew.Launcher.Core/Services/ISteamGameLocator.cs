namespace StarCrew.Launcher.Services;

/// <summary>Steam 定位器接缝：生产用 <see cref="SteamGameLocator" />，测试用替身。</summary>
internal interface ISteamGameLocator
{
    /// <summary>查找 Among Us.exe，找到返回完整路径，否则返回 null。</summary>
    string? FindGameExe();
}
