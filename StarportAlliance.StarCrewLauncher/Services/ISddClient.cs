using StarportAlliance.StarCrewLauncher.Models;

namespace StarportAlliance.StarCrewLauncher.Services;

/// <summary>StarDepotDownloader 的结构化调用客户端。</summary>
internal interface ISddClient
{
    /// <summary>查询缓存凭据状态。</summary>
    /// <param name="accountName">限定账号；为 null 时查询任意已缓存账号。</param>
    /// <param name="online">
    /// 为 true 时通过 Steam 在线校验凭据，结论按设置缓存。超时、连接失败等无结论情况返回
    /// <see cref="SddAuthStatus.Checked" /> 为 false；明确鉴权失败返回 Checked 为 true、HasCredentials 为 false。
    /// </param>
    Task<SddAuthStatus> GetAuthStatusAsync(
        string? accountName,
        bool online = false,
        CancellationToken cancellationToken = default
    );

    /// <summary>清除在线登录校验缓存。</summary>
    void InvalidateAuthCache();

    /// <summary>扫码登录，仅在拿到凭据后返回。</summary>
    /// <param name="qr">二维码矩阵刷新回调；事件到达即报告，不等待登录结束。</param>
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
