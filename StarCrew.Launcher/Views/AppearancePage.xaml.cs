using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using StarCrew.Launcher.Models;
using StarCrew.Launcher.Services;
using Windows.Storage;

namespace StarCrew.Launcher.Views;

public sealed partial class AppearancePage : Page
{
    private IThemeSettings? _themeSettings;
    private bool _syncingTheme;

    public AppearancePage()
    {
        InitializeComponent();
    }

    /// <summary>主题变更时通知上层（由 AboutPage 的 ThemeChanged 透传）。</summary>
    internal event EventHandler<AppTheme>? ThemeChanged;

    internal void BindTheme(IThemeSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        _themeSettings = settings;
        SyncThemeCombo();
    }

    private void SyncThemeCombo()
    {
        if (_themeSettings is null || ThemeCombo is null)
        {
            return;
        }

        _syncingTheme = true;
        try
        {
            ThemeCombo.SelectedIndex = _themeSettings.Theme switch
            {
                AppTheme.Light => 1,
                AppTheme.Dark => 2,
                _ => 0,
            };
        }
        finally
        {
            _syncingTheme = false;
        }
    }

    private void ThemeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_syncingTheme || _themeSettings is null)
        {
            return;
        }

        AppTheme theme = ThemeCombo.SelectedIndex switch
        {
            1 => AppTheme.Light,
            2 => AppTheme.Dark,
            _ => AppTheme.System,
        };
        _themeSettings.Theme = theme;
        ThemeChanged?.Invoke(this, theme);
    }
}
