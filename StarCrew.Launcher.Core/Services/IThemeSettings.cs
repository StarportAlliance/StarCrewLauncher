using StarCrew.Launcher.Models;

namespace StarCrew.Launcher.Services;

/// <summary>主题设置接口。</summary>
internal interface IThemeSettings
{
    /// <summary>主题偏好发生实际变更事件。</summary>
    event EventHandler<AppTheme>? ThemeChanged;

    /// <summary>强调色发生实际变更事件。</summary>
    event EventHandler? AccentColorsChanged;

    /// <summary>显示字体发生实际变更事件。</summary>
    event EventHandler? DisplayFontChanged;

    /// <summary>当前主题偏好。</summary>
    AppTheme Theme { get; set; }

    /// <summary>
    /// 浅色主题的自定义强调色（<c>#RRGGBB</c>）；<see langword="null"/> 表示使用内置默认色。
    /// </summary>
    string? LightAccentColor { get; set; }

    /// <summary>
    /// 深色主题的自定义强调色（<c>#RRGGBB</c>）；<see langword="null"/> 表示使用内置默认色。
    /// </summary>
    string? DarkAccentColor { get; set; }

    /// <summary>自定义显示字体族名；<see langword="null"/> 表示跟随系统默认字体。</summary>
    string? DisplayFont { get; set; }
}
