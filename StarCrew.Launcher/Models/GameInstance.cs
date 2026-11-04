namespace StarCrew.Launcher.Models;

/// <summary>游戏实例信息。</summary>
internal sealed record GameInstance(
    Guid Id,
    string Name,
    string InstallDirectory,
    int AppId,
    int DepotId,
    long ManifestId,
    string GameVersionLabel,
    InstanceState State,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastLaunchedAt
);
