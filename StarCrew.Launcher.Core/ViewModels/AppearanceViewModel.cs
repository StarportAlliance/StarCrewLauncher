using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StarCrew.Launcher.Models;
using StarCrew.Launcher.Services;

namespace StarCrew.Launcher.ViewModels;

internal sealed partial class AppearanceViewModel : ObservableObject
{
    private readonly IThemeSettings _themeSettings;
    private readonly FontOption _defaultFont;
    private FontOption _selectedFontOption;

    public AppearanceViewModel(
        IThemeSettings themeSettings,
        ISystemFontCatalog fontCatalog,
        IAppStrings strings
    )
    {
        ArgumentNullException.ThrowIfNull(themeSettings);
        ArgumentNullException.ThrowIfNull(fontCatalog);
        ArgumentNullException.ThrowIfNull(strings);

        _themeSettings = themeSettings;
        _defaultFont = new FontOption(
            strings.GetString("Appearance_FontDefault"),
            string.Empty,
            null
        );
        List<FontOption> options = [_defaultFont, .. fontCatalog.GetFonts()];
        FontOptions = options;
        _selectedFontOption =
            options.Find(option =>
                option.FamilyName is { } family
                && string.Equals(
                    family,
                    themeSettings.DisplayFont,
                    StringComparison.OrdinalIgnoreCase
                )
            ) ?? _defaultFont;
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

            // 选回默认色等同于未自定义，避免“颜色没变但重置按钮亮着”
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

    public IReadOnlyList<FontOption> FontOptions { get; }

    public FontOption SelectedFontOption
    {
        get => _selectedFontOption;
        set
        {
            if (value is null || ReferenceEquals(value, _selectedFontOption))
            {
                return;
            }

            _selectedFontOption = value;
            _themeSettings.DisplayFont = value.FamilyName;
            OnPropertyChanged(nameof(SelectedFontOption));
            OnPropertyChanged(nameof(CanResetFont));
        }
    }

    public bool CanResetFont => _themeSettings.DisplayFont is not null;

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

    [RelayCommand]
    private void ResetFont()
    {
        if (_themeSettings.DisplayFont is null)
        {
            return;
        }

        _themeSettings.DisplayFont = null;
        _selectedFontOption = _defaultFont;
        OnPropertyChanged(nameof(SelectedFontOption));
        OnPropertyChanged(nameof(CanResetFont));
    }
}
