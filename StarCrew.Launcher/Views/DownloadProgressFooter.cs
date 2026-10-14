using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using StarCrew.Launcher.Models;
using StarCrew.Launcher.Services;

namespace StarCrew.Launcher.Views;

/// <summary>
/// 下载进度对话框底部的一行：左侧实时速度，右侧「已下载/总大小」。
/// 放进 <see cref="ContentDialog.Content" /> 后，每次进度上报调 <see cref="Apply" /> 即可；
/// 总大小未知时保持占位文案，退回只显示百分比。
/// </summary>
internal sealed class DownloadProgressFooter
{
    private readonly TextBlock _speed = new() { Text = "测速中..." };
    private readonly TextBlock _totals = new() { Text = "资源大小核对中..." };
    private double _lastBytes;
    private long _lastMeasuredAt;

    internal DownloadProgressFooter()
    {
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

    /// <summary>
    /// 速率按相邻两次上报的字节增量除以时间差估算：进度只在文件完成时跳一次，
    /// 所以这里取瞬时值，不做平滑（平滑会让首个数晚几秒才出现）。
    /// </summary>
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
        _totals.Text = $"{ByteFormat.Describe(downloaded)} / {ByteFormat.Describe(total)}";
    }
}
