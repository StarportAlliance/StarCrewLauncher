using StarCrew.Launcher.Models;

namespace StarCrew.Launcher.Services;

/// <summary>Steam 账号登录接缝：需要登录后才能下载付费游戏。</summary>
internal interface ISteamLoginManager
{
    /// <summary>执行登录（含交互式 Steam Guard 验证码），成功返回账号名。</summary>
    Task<SteamLoginOutcome> LoginAsync(
        string username,
        string password,
        string? steamGuardCode,
        CancellationToken cancellationToken = default
    );

    /// <summary>扫码登录：DD 输出 QR 图的文本输出透传给调用方，成功后自动存储账号。</summary>
    Task<SteamLoginOutcome> LoginWithQrAsync(
        IProgress<string>? output = null,
        CancellationToken cancellationToken = default
    );
}
