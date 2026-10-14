namespace StarCrew.Launcher.Models;

/// <summary>全局实例存储的持久化结构（instances.json）。</summary>
internal sealed record LauncherState(
    int Version,
    Guid? LastInstanceId,
    string? SteamAccountName,
    IReadOnlyList<GameInstance> Instances
)
{
    public static LauncherState Empty { get; } = new(1, null, null, []);
}
