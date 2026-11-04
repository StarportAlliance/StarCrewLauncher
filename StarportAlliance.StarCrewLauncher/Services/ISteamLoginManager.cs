using StarportAlliance.StarCrewLauncher.Models;

namespace StarportAlliance.StarCrewLauncher.Services;

/// <summary>Steam 账号登录管理器。</summary>
internal interface ISteamLoginManager
{
    /// <summary>请求登录。</summary>
    Task<SteamLoginOutcome> LoginAsync(
        string username,
        string password,
        string? steamGuardCode,
        CancellationToken cancellationToken = default
    );

    /// <summary>扫码登录。</summary>
    Task<SteamLoginOutcome> LoginWithQrAsync(
        IProgress<string>? output = null,
        CancellationToken cancellationToken = default
    );
}
