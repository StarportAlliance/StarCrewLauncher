using StarCrew.Launcher.Models;
using StarCrew.Launcher.Services;

namespace StarCrew.Launcher.ViewModels;

internal sealed class AppearanceViewModel
{
    private readonly IThemeSettings _settings;

    public AppearanceViewModel(IThemeSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        _settings = settings;
    }

    /// <summary>下拉框下标。</summary>
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
