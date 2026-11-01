using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StarCrew.Launcher.Models;
using StarCrew.Launcher.Services;

namespace StarCrew.Launcher.ViewModels;

internal sealed partial class AppearanceViewModel : ObservableObject
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

    public string LightAccentColor
    {
        get => _themeSettings.LightAccentColor ?? AccentPalette.DefaultLight;
        set
        {
            if (AccentPalette.Normalize(value) is not { } normalized)
            {
                return;
            }

            // 选回默认色等同于未自定义，避免“颜色没变但重置按钮亮着”
            string? stored = normalized == AccentPalette.DefaultLight ? null : normalized;
            if (stored == _themeSettings.LightAccentColor)
            {
                return;
            }

            _themeSettings.LightAccentColor = stored;
            OnPropertyChanged(nameof(LightAccentColor));
            OnPropertyChanged(nameof(CanResetLightAccent));
        }
    }

    public string DarkAccentColor
    {
        get => _themeSettings.DarkAccentColor ?? AccentPalette.DefaultDark;
        set
        {
            if (AccentPalette.Normalize(value) is not { } normalized)
            {
                return;
            }

            string? stored = normalized == AccentPalette.DefaultDark ? null : normalized;
            if (stored == _themeSettings.DarkAccentColor)
            {
                return;
            }

            _themeSettings.DarkAccentColor = stored;
            OnPropertyChanged(nameof(DarkAccentColor));
            OnPropertyChanged(nameof(CanResetDarkAccent));
        }
    }

    public bool CanResetLightAccent => _themeSettings.LightAccentColor is not null;

    public bool CanResetDarkAccent => _themeSettings.DarkAccentColor is not null;

    [RelayCommand]
    private void ResetLightAccent()
    {
        if (_themeSettings.LightAccentColor is null)
        {
            return;
        }

        _themeSettings.LightAccentColor = null;
        OnPropertyChanged(nameof(LightAccentColor));
        OnPropertyChanged(nameof(CanResetLightAccent));
    }

    [RelayCommand]
    private void ResetDarkAccent()
    {
        if (_themeSettings.DarkAccentColor is null)
        {
            return;
        }

        _themeSettings.DarkAccentColor = null;
        OnPropertyChanged(nameof(DarkAccentColor));
        OnPropertyChanged(nameof(CanResetDarkAccent));
    }
}
