namespace StarCrew.Launcher.Models;

/// <summary>版本目录。</summary>
internal sealed record VersionCatalog(decimal Version, IReadOnlyList<GameVersionEntry> Versions);
