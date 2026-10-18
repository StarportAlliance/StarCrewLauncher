namespace StarCrew.Launcher.Models;

/// <summary>全局实例状态（instances.json 的持久化结构）。</summary>
/// <param name="Version">持久化结构版本号。</param>
internal sealed record LauncherState(
    int Version,
    Guid? LastInstanceId,
    string? SteamAccountName,
    IReadOnlyList<GameInstance> Instances
)
{
    public static LauncherState Empty { get; } = new(1, null, null, []);
}
