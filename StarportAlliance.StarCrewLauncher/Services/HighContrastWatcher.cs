using Windows.UI.ViewManagement;

namespace StarportAlliance.StarCrewLauncher.Services;

/// <summary>系统高对比度状态与变更通知。</summary>
/// <remarks><see cref="AccessibilitySettings.HighContrastChanged"/> 在 Win32 下不会触发，改由窗口的 WM_THEMECHANGED 驱动。</remarks>
internal sealed class HighContrastWatcher
{
    private readonly AccessibilitySettings _settings = new();

    public bool IsEnabled => _settings.HighContrast;

    public event EventHandler? Changed;

    public void NotifyThemeChanged() => Changed?.Invoke(this, EventArgs.Empty);
}
