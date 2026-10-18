using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using StarCrew.Launcher.Models;
using StarCrew.Launcher.Services;

namespace StarCrew.Launcher.Views;

/// <summary>
/// 下载状态跟踪组件。
/// </summary>
internal sealed class DownloadProgressFooter
{
    private readonly IAppStrings _strings;
    private readonly TextBlock _speed;
    private readonly TextBlock _totals;
    private double _lastBytes;
    private long _lastMeasuredAt;

    internal DownloadProgressFooter(IAppStrings strings)
    {
        ArgumentNullException.ThrowIfNull(strings);
        _strings = strings;
        _speed = new TextBlock { Text = strings.GetString("Progress_MeasuringSpeed") };
        _totals = new TextBlock { Text = strings.GetString("Progress_CheckingSize") };
        Grid grid = new() { Margin = new Thickness(0, 4, 0, 0) };
        grid.ColumnDefinitions.Add(
            new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
        );
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.Children.Add(_speed);
        grid.Children.Add(_totals);
        Grid.SetColumn(_totals, 1);
        Root = grid;
    }

    /// <summary>要放进对话框内容的元素。</summary>
    internal FrameworkElement Root { get; }

    internal void Apply(DownloadProgressInfo info)
    {
        if (info.TotalBytes is not long total || total <= 0)
        {
            return;
        }

        double downloaded = total * info.Percent / 100.0;
        long now = Environment.TickCount64;
        if (_lastMeasuredAt > 0 && now > _lastMeasuredAt)
        {
            double bytesPerSecond = (downloaded - _lastBytes) * 1000.0 / (now - _lastMeasuredAt);
            _speed.Text = ByteFormat.Rate(bytesPerSecond);
        }

        _lastBytes = downloaded;
        _lastMeasuredAt = now;
        _totals.Text = _strings.Format(
            "Progress_TotalsFormat",
            ByteFormat.Describe(downloaded),
            ByteFormat.Describe(total)
        );
    }
}
