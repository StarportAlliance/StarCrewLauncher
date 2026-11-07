using StarportAlliance.StarCrewLauncher.Models;

namespace StarportAlliance.StarCrewLauncher.Services;

/// <summary>Steam 账号登录管理器。</summary>
internal interface ISteamLoginManager
{
    /// <summary>扫码登录。</summary>
    /// <param name="qr">二维码矩阵刷新回调。</param>
    Task<SteamLoginOutcome> LoginWithQrAsync(
        IProgress<SddQrCode>? qr = null,
        CancellationToken cancellationToken = default
    );
}
