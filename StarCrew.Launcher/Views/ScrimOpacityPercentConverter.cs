using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace StarCrew.Launcher.Views;

/// <summary>滑块 Thumb ToolTip 用：把 0~1 的透明度格式化为与 ScrimOpacityLabel 一致的整数百分比。</summary>
public sealed class ScrimOpacityPercentConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        _ = targetType;
        _ = parameter;
        _ = language;
        return value is double opacity
            ? string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                "{0}%",
                Math.Round(opacity * 100, MidpointRounding.AwayFromZero)
            )
            : DependencyProperty.UnsetValue;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        _ = value;
        _ = targetType;
        _ = parameter;
        _ = language;
        return DependencyProperty.UnsetValue;
    }
}
