using StarCrew.Launcher.Models;

namespace StarCrew.Launcher.Services;

/// <summary>主题设置接口。</summary>
internal interface IThemeSettings
{
    /// <summary>主题偏好发生实际变更事件。</summary>
    event EventHandler<AppTheme>? ThemeChanged;

    /// <summary>当前主题偏好。</summary>
    AppTheme Theme { get; set; }
}
