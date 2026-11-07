using StarportAlliance.StarCrewLauncher.Models;

namespace StarportAlliance.StarCrewLauncher.Services;

/// <summary>StarDepotDownloader 的结构化调用客户端。</summary>
internal interface ISddClient
{
    /// <summary>查询本地缓存凭据状态。</summary>
    /// <param name="accountName">限定账号；为 null 时查询任意已缓存账号。</param>
    /// <returns>凭据不可用或工具缺失时返回 <see cref="SddAuthStatus.HasCredentials" /> 为 false。</returns>
    Task<SddAuthStatus> GetAuthStatusAsync(
        string? accountName,
        CancellationToken cancellationToken = default
    );

    /// <summary>扫码登录，仅在拿到凭据后返回。</summary>
    /// <param name="qr">二维码矩阵刷新回调。</param>
    Task<SddLoginResult> LoginWithQrAsync(
        IProgress<SddQrCode>? qr,
        CancellationToken cancellationToken = default
    );

    /// <summary>清除缓存的登录凭据。</summary>
    Task<SddLogoutResult> LogoutAsync(CancellationToken cancellationToken = default);

    /// <summary>下载指定清单。</summary>
    /// <param name="progress">进度回调；start 事件会先报告一次 0% 与总大小。</param>
    Task<SddDownloadResult> DownloadAsync(
        SddDownloadRequest request,
        IProgress<SddProgress>? progress,
        CancellationToken cancellationToken = default
    );
}
