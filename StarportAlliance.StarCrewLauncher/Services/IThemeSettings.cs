using StarportAlliance.StarCrewLauncher.Models;

namespace StarportAlliance.StarCrewLauncher.Services;

/// <summary>主题设置接口。</summary>
internal interface IThemeSettings
{
    /// <summary>主题偏好发生实际变更事件。</summary>
    event EventHandler<AppTheme>? ThemeChanged;

    /// <summary>强调色发生实际变更事件。</summary>
    event EventHandler? AccentColorsChanged;

    /// <summary>显示字体发生实际变更事件。</summary>
    event EventHandler? DisplayFontChanged;

    /// <summary>背景遮罩不透明度发生实际变更事件。</summary>
    event EventHandler? ScrimOpacityChanged;

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

    /// <summary>当前背景类型。</summary>
    BackgroundKind Background { get; set; }

    /// <summary>纯色背景的自定义颜色（<c>#RRGGBB</c>）；<see langword="null"/> 表示尚未自定义过。</summary>
    string? SolidBackgroundColor { get; set; }

    /// <summary>
    /// 纯色背景是否跟随当前主题；<see langword="false"/> 时使用 <see cref="SolidBackgroundColor"/>。
    /// </summary>
    bool SolidBackgroundFollowsTheme { get; set; }

    /// <summary>自定义背景图片路径；<see langword="null"/> 表示尚未导入过自定义图片。</summary>
    string? BackgroundImagePath { get; set; }

    /// <summary>
    /// 当前选中的内置背景图标识；<see langword="null"/> 表示使用默认内置背景图。
    /// </summary>
    string? BuiltInBackgroundId { get; set; }

    /// <summary>图片背景是否使用自定义图片；<see langword="false"/> 时使用内置背景图。</summary>
    bool UseCustomBackgroundImage { get; set; }

    /// <summary>
    /// 图片背景在非主页的遮罩不透明度，取值 <c>0</c>（不显示遮罩）到 <c>1</c>（完全不透明）。
    /// </summary>
    double ScrimOpacity { get; set; }
}
