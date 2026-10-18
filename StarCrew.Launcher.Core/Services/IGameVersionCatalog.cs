using StarCrew.Launcher.Models;

namespace StarCrew.Launcher.Services;

/// <summary>版本目录读取器。</summary>
internal interface IGameVersionCatalog
{
    /// <summary>加载版本目录。</summary>
    Task<VersionCatalog> LoadAsync(CancellationToken cancellationToken = default);
}
