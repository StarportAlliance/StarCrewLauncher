using StarCrew.Launcher.Models;
using StarCrew.Launcher.Services;

namespace StarCrew.Launcher.ViewModels;

/// <summary>通用设置视图模型：主题下拉直接读写主题设置。</summary>
internal sealed class AppearanceViewModel
{
    private readonly IThemeSettings _settings;

    public AppearanceViewModel(IThemeSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        _settings = settings;
    }

    /// <summary>下拉框选中下标：0 跟随系统、1 浅色、2 深色。</summary>
    public int SelectedThemeIndex
    {
        get =>
            _settings.Theme switch
            {
                AppTheme.Light => 1,
                AppTheme.Dark => 2,
                _ => 0,
            };
        set =>
            _settings.Theme = value switch
            {
                1 => AppTheme.Light,
                2 => AppTheme.Dark,
                _ => AppTheme.System,
            };
    }
}
