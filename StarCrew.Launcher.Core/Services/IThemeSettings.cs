using StarCrew.Launcher.Models;

namespace StarCrew.Launcher.Services;

/// <summary>主题设置接缝：读写持久化的主题偏好。</summary>
internal interface IThemeSettings
{
    /// <summary>当前主题偏好；缺失或非法值时读回 System。</summary>
    AppTheme Theme { get; set; }
}
