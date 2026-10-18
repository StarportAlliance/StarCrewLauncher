using StarCrew.Launcher.Models;

namespace StarCrew.Launcher.Services;

internal sealed class ThemeSettings : IThemeSettings
{
    public const string ThemeKey = "AppTheme";

    private readonly IDictionary<string, object> _store;

    public ThemeSettings(IDictionary<string, object> store)
    {
        ArgumentNullException.ThrowIfNull(store);
        _store = store;
    }

    public event EventHandler<AppTheme>? ThemeChanged;

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
}
