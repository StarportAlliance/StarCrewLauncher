namespace StarCrew.Launcher.Models;

/// <summary>游戏实例：一个自管目录里的 Among Us 原版安装及其元数据。</summary>
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
