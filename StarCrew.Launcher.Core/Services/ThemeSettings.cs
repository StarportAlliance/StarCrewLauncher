using StarCrew.Launcher.Models;

namespace StarCrew.Launcher.Services;

/// <summary>主题设置：以字符串持久化到键值存储，缺失或非法值回退 System。</summary>
internal sealed class ThemeSettings : IThemeSettings
{
    public const string ThemeKey = "AppTheme";

    private readonly IDictionary<string, object> _store;

    public ThemeSettings(IDictionary<string, object> store)
    {
        ArgumentNullException.ThrowIfNull(store);
        _store = store;
    }

    /// <inheritdoc />
    public AppTheme Theme
    {
        get => Load();
        set => Save(value);
    }

    private AppTheme Load()
    {
        if (!_store.TryGetValue(ThemeKey, out object? raw) || raw is null)
        {
            return AppTheme.System;
        }

        if (
            raw is string text
            && Enum.TryParse<AppTheme>(text, ignoreCase: true, out AppTheme parsed)
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
