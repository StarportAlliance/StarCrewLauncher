using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using StarCrew.Launcher.Services;

namespace StarCrew.Launcher.Views;

/// <summary>十六进制颜色字符串与 <see cref="Windows.UI.Color"/> 的互转；无法解析时视为透明。</summary>
public sealed class HexColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        _ = targetType;
        _ = parameter;
        _ = language;
        return value is string text && AccentPalette.TryParse(text, out AccentRgb rgb)
            ? Windows.UI.Color.FromArgb(255, rgb.R, rgb.G, rgb.B)
            : Microsoft.UI.Colors.Transparent;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        _ = targetType;
        _ = parameter;
        _ = language;
        return value is Windows.UI.Color color
            ? AccentPalette.ToHex(new AccentRgb(color.R, color.G, color.B))
            : DependencyProperty.UnsetValue;
    }
}
