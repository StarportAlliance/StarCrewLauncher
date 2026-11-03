using StarCrew.Launcher.Models;

namespace StarCrew.Launcher.Services;

internal sealed class ThemeSettings : IThemeSettings
{
    public const string ThemeKey = "AppTheme";
    public const string LightAccentColorKey = "LightAccentColor";
    public const string DarkAccentColorKey = "DarkAccentColor";
    public const string DisplayFontKey = "DisplayFont";
    public const string BackgroundKey = "BackgroundKind";
    public const string SolidBackgroundColorKey = "SolidBackgroundColor";
    public const string SolidBackgroundFollowsThemeKey = "SolidBackgroundFollowsTheme";
    public const string BackgroundImagePathKey = "BackgroundImagePath";
    public const string UseCustomBackgroundImageKey = "UseCustomBackgroundImage";
    public const string ScrimOpacityKey = "ScrimOpacity";
    public const double DefaultScrimOpacity = 0.6;

    private readonly IDictionary<string, object> _store;

    public ThemeSettings(IDictionary<string, object> store)
    {
        ArgumentNullException.ThrowIfNull(store);
        _store = store;
    }

    public event EventHandler<AppTheme>? ThemeChanged;

    public event EventHandler? AccentColorsChanged;

    public event EventHandler? DisplayFontChanged;

    public event EventHandler? BackgroundChanged;

    public event EventHandler? ScrimOpacityChanged;

    public AppTheme Theme
    {
        get => Load();
        set
        {
            if (Load() == value)
            {
                return;
            }

            Save(value);
            ThemeChanged?.Invoke(this, value);
        }
    }

    public string? LightAccentColor
    {
        get => LoadAccent(LightAccentColorKey);
        set => SaveAccent(LightAccentColorKey, value);
    }

    public string? DarkAccentColor
    {
        get => LoadAccent(DarkAccentColorKey);
        set => SaveAccent(DarkAccentColorKey, value);
    }

    public string? DisplayFont
    {
        get => LoadDisplayFont();
        set => SaveDisplayFont(value);
    }

    public BackgroundKind Background
    {
        get => LoadBackground();
        set => SaveBackground(value);
    }

    public string? SolidBackgroundColor
    {
        get => LoadSolidBackgroundColor();
        set => SaveSolidBackgroundColor(value);
    }

    public bool SolidBackgroundFollowsTheme
    {
        get => LoadSolidBackgroundFollowsTheme();
        set => SaveSolidBackgroundFollowsTheme(value);
    }

    public string? BackgroundImagePath
    {
        get => LoadBackgroundImagePath();
        set => SaveBackgroundImagePath(value);
    }

    public bool UseCustomBackgroundImage
    {
        get => LoadUseCustomBackgroundImage();
        set => SaveUseCustomBackgroundImage(value);
    }

    public double ScrimOpacity
    {
        get => LoadScrimOpacity();
        set => SaveScrimOpacity(value);
    }

    private AppTheme Load()
    {
        if (!_store.TryGetValue(ThemeKey, out object? raw) || raw is null)
        {
            return AppTheme.System;
        }

        if (
            raw is string text
            && Enum.TryParse(text, ignoreCase: true, out AppTheme parsed)
            && Enum.IsDefined(parsed)
        )
        {
            return parsed;
        }

        return AppTheme.System;
    }

    private void Save(AppTheme theme)
    {
        if (!Enum.IsDefined(theme))
        {
            throw new ArgumentOutOfRangeException(nameof(theme), theme, "未知的主题偏好。");
        }

        _store[ThemeKey] = theme.ToString();
    }

    private string? LoadAccent(string key) =>
        _store.TryGetValue(key, out object? raw) && raw is string text
            ? AccentPalette.Normalize(text)
            : null;

    private void SaveAccent(string key, string? value)
    {
        string? normalized = value is null
            ? null
            : AccentPalette.Normalize(value)
                ?? throw new ArgumentException("强调色必须是 #RRGGBB 格式。", nameof(value));

        if (normalized == LoadAccent(key))
        {
            return;
        }

        if (normalized is null)
        {
            _store.Remove(key);
        }
        else
        {
            _store[key] = normalized;
        }

        AccentColorsChanged?.Invoke(this, EventArgs.Empty);
    }

    private string? LoadDisplayFont() =>
        _store.TryGetValue(DisplayFontKey, out object? raw)
        && raw is string text
        && !string.IsNullOrWhiteSpace(text)
            ? text.Trim()
            : null;

    private void SaveDisplayFont(string? value)
    {
        string? normalized =
            value is null ? null
            : string.IsNullOrWhiteSpace(value)
                ? throw new ArgumentException("字体名称不能为空。", nameof(value))
            : value.Trim();

        if (normalized == LoadDisplayFont())
        {
            return;
        }

        if (normalized is null)
        {
            _store.Remove(DisplayFontKey);
        }
        else
        {
            _store[DisplayFontKey] = normalized;
        }

        DisplayFontChanged?.Invoke(this, EventArgs.Empty);
    }

    private BackgroundKind LoadBackground()
    {
        if (!_store.TryGetValue(BackgroundKey, out object? raw) || raw is null)
        {
            return BackgroundKind.Image;
        }

        if (
            raw is string text
            && Enum.TryParse(text, ignoreCase: true, out BackgroundKind parsed)
            && Enum.IsDefined(parsed)
        )
        {
            return parsed;
        }

        return BackgroundKind.Image;
    }

    private void SaveBackground(BackgroundKind background)
    {
        if (!Enum.IsDefined(background))
        {
            throw new ArgumentOutOfRangeException(
                nameof(background),
                background,
                "未知的背景类型。"
            );
        }

        if (LoadBackground() == background)
        {
            return;
        }

        _store[BackgroundKey] = background.ToString();
        BackgroundChanged?.Invoke(this, EventArgs.Empty);
    }

    private string? LoadSolidBackgroundColor() =>
        _store.TryGetValue(SolidBackgroundColorKey, out object? raw) && raw is string text
            ? AccentPalette.Normalize(text)
            : null;

    private void SaveSolidBackgroundColor(string? value)
    {
        string? normalized = value is null
            ? null
            : AccentPalette.Normalize(value)
                ?? throw new ArgumentException("背景色必须是 #RRGGBB 格式。", nameof(value));

        if (normalized == LoadSolidBackgroundColor())
        {
            return;
        }

        if (normalized is null)
        {
            _store.Remove(SolidBackgroundColorKey);
        }
        else
        {
            _store[SolidBackgroundColorKey] = normalized;
        }

        BackgroundChanged?.Invoke(this, EventArgs.Empty);
    }

    private bool LoadUseCustomBackgroundImage()
    {
        if (
            _store.TryGetValue(UseCustomBackgroundImageKey, out object? raw)
            && raw is bool useCustom
        )
        {
            return useCustom;
        }

        // 旧数据只有图片路径；有路径时视为正在使用自定义图片
        return LoadBackgroundImagePath() is not null;
    }

    private void SaveUseCustomBackgroundImage(bool useCustom)
    {
        if (LoadUseCustomBackgroundImage() == useCustom)
        {
            return;
        }

        _store[UseCustomBackgroundImageKey] = useCustom;
        BackgroundChanged?.Invoke(this, EventArgs.Empty);
    }

    private bool LoadSolidBackgroundFollowsTheme()
    {
        if (
            _store.TryGetValue(SolidBackgroundFollowsThemeKey, out object? raw)
            && raw is bool follows
        )
        {
            return follows;
        }

        // 旧数据只有自定义色；有自定义色时视为不再跟随主题
        return LoadSolidBackgroundColor() is null;
    }

    private void SaveSolidBackgroundFollowsTheme(bool follows)
    {
        if (LoadSolidBackgroundFollowsTheme() == follows)
        {
            return;
        }

        _store[SolidBackgroundFollowsThemeKey] = follows;
        BackgroundChanged?.Invoke(this, EventArgs.Empty);
    }

    private string? LoadBackgroundImagePath() =>
        _store.TryGetValue(BackgroundImagePathKey, out object? raw)
        && raw is string text
        && !string.IsNullOrWhiteSpace(text)
            ? text.Trim()
            : null;

    private double LoadScrimOpacity()
    {
        if (
            _store.TryGetValue(ScrimOpacityKey, out object? raw)
            && raw is double value
            && double.IsFinite(value)
        )
        {
            return Math.Clamp(value, 0, 1);
        }

        return DefaultScrimOpacity;
    }

    private void SaveScrimOpacity(double value)
    {
        if (!double.IsFinite(value))
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                value,
                "背景遮罩不透明度必须是 0 到 1 之间的数值。"
            );
        }

        double normalized = Math.Round(Math.Clamp(value, 0, 1), 2, MidpointRounding.AwayFromZero);
        if (normalized == LoadScrimOpacity())
        {
            return;
        }

        _store[ScrimOpacityKey] = normalized;
        ScrimOpacityChanged?.Invoke(this, EventArgs.Empty);
    }

    private void SaveBackgroundImagePath(string? value)
    {
        string? normalized =
            value is null ? null
            : string.IsNullOrWhiteSpace(value)
                ? throw new ArgumentException("背景图片路径不能为空。", nameof(value))
            : value.Trim();

        if (normalized == LoadBackgroundImagePath())
        {
            return;
        }

        if (normalized is null)
        {
            _store.Remove(BackgroundImagePathKey);
        }
        else
        {
            _store[BackgroundImagePathKey] = normalized;
        }

        BackgroundChanged?.Invoke(this, EventArgs.Empty);
    }
}
