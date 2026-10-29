using StarCrew.Launcher.Models;
using StarCrew.Launcher.Services;

namespace StarCrew.Launcher.ViewModels;

internal sealed class AppearanceViewModel
{
    private readonly IThemeSettings _themeSettings;

    public AppearanceViewModel(IThemeSettings themeSettings)
    {
        ArgumentNullException.ThrowIfNull(themeSettings);
        _themeSettings = themeSettings;
    }

    /// <summary>下拉框下标。</summary>
    public int SelectedThemeIndex
    {
        get =>
            _themeSettings.Theme switch
            {
                AppTheme.Light => 1,
                AppTheme.Dark => 2,
                _ => 0,
            };
        set =>
            _themeSettings.Theme = value switch
            {
                1 => AppTheme.Light,
                2 => AppTheme.Dark,
                _ => AppTheme.System,
            };
    }
}
