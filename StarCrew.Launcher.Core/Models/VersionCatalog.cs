namespace StarCrew.Launcher.Models;

/// <summary>版本目录。</summary>
internal sealed record VersionCatalog(decimal Version, IReadOnlyList<GameVersionEntry> Versions);

/// <summary>版本目录加载结果。</summary>
/// <param name="WrittenAt">数据对应的缓存时间（UTC），远端获取成功时为写入时刻。</param>
internal sealed record VersionCatalogLoadResult(VersionCatalog Catalog, DateTimeOffset WrittenAt);
