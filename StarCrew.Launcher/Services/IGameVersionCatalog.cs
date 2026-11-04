using StarCrew.Launcher.Models;

namespace StarCrew.Launcher.Services;

/// <summary>版本目录读取器。</summary>
internal interface IGameVersionCatalog
{
    /// <summary>加载版本目录。</summary>
    /// <param name="forceRefresh">为 <see langword="true"/> 时忽略缓存时效直接请求远端。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>目录及数据对应的缓存时间；远端失败时会回退到过期缓存。</returns>
    /// <remarks>
    /// 缓存未过期时不发起网络请求；<paramref name="forceRefresh"/> 下远端失败同样回退到过期缓存；
    /// 缓存时长为 0 时禁用缓存，始终请求远端且不再读取或写入缓存文件；
    /// 既无缓存又无远端数据时抛出 <see cref="InvalidOperationException"/>。
    /// </remarks>
    Task<VersionCatalogLoadResult> LoadAsync(
        bool forceRefresh = false,
        CancellationToken cancellationToken = default
    );

    /// <summary>删除本地版本目录缓存文件。</summary>
    void ClearCache();
}
