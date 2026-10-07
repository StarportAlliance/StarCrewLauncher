using StarCrew.Launcher.Models;

namespace StarCrew.Launcher.Services;

/// <summary>主题设置接缝：读写持久化的主题偏好，并在实际变更时广播。</summary>
internal interface IThemeSettings
{
    /// <summary>主题偏好发生实际变更（新值不同于旧值）时触发，参数为新偏好。</summary>
    event EventHandler<AppTheme>? ThemeChanged;

    /// <summary>当前主题偏好；缺失或非法值时读回 System。</summary>
    AppTheme Theme { get; set; }
}
