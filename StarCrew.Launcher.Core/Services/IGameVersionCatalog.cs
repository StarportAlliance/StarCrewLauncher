using StarCrew.Launcher.Models;

namespace StarCrew.Launcher.Services;

/// <summary>原版游戏版本目录接缝：读取远端/缓存/内置 JSON。</summary>
internal interface IGameVersionCatalog
{
    /// <summary>加载目录：优先远端，失败回退缓存，再回退到内置 JSON。</summary>
    Task<VersionCatalog> LoadAsync(CancellationToken cancellationToken = default);
}
