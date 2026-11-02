using StarCrew.Launcher.Models;

namespace StarCrew.Launcher.Services;

internal sealed class ThemeSettings : IThemeSettings
{
    public const string ThemeKey = "AppTheme";
    public const string LightAccentColorKey = "LightAccentColor";
    public const string DarkAccentColorKey = "DarkAccentColor";
    public const string DisplayFontKey = "DisplayFont";

    private readonly IDictionary<string, object> _store;

    public ThemeSettings(IDictionary<string, object> store)
    {
        ArgumentNullException.ThrowIfNull(store);
        _store = store;
    }

    public event EventHandler<AppTheme>? ThemeChanged;

    public event EventHandler? AccentColorsChanged;

    public event EventHandler? DisplayFontChanged;

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
}
