using StarCrew.Launcher.Models;

namespace StarCrew.Launcher.Services;

/// <summary>系统字体目录。</summary>
internal interface ISystemFontCatalog
{
    /// <summary>枚举系统已安装、且包含中文字形的字体族，按显示名排序。</summary>
    IReadOnlyList<FontOption> GetFonts();
}
