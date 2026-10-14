namespace StarCrew.Launcher.Models;

/// <summary>版本目录（远端 JSON 的根对象）。</summary>
internal sealed record VersionCatalog(int Version, IReadOnlyList<GameVersionEntry> Versions);
