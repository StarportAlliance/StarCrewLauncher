namespace StarCrew.Launcher.Models;

/// <summary>版本目录。</summary>
internal sealed record VersionCatalog(int Version, IReadOnlyList<GameVersionEntry> Versions);
