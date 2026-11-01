using System.Globalization;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using StarCrew.Launcher.Services;

namespace StarCrew.Launcher.Views;

/// <summary>强调色十六进制字符串与 <see cref="Windows.UI.Color"/> 的互转。</summary>
public sealed class AccentHexColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        _ = targetType;
        _ = parameter;
        _ = language;
        return value is string text && AccentPalette.TryParse(text, out AccentRgb rgb)
            ? Windows.UI.Color.FromArgb(255, rgb.R, rgb.G, rgb.B)
            : DependencyProperty.UnsetValue;
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
