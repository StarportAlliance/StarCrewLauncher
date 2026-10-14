using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using StarCrew.Launcher.Models;
using StarCrew.Launcher.Services;
using StarCrew.Launcher.ViewModels;

namespace StarCrew.Launcher.Views;

public sealed partial class DownloadAccountPage : Page
{
    public DownloadAccountPage()
    {
        InitializeComponent();
    }

    internal DownloadPageViewModel? ViewModel { get; private set; }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        ViewModel = e.Parameter as DownloadPageViewModel;
        Bindings.Update();

        if (ViewModel is not null)
        {
            ViewModel.PropertyChanged += OnViewModelPropertyChanged;
            UpdateAccountToggle();
        }
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        if (ViewModel is not null)
        {
            ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }
    }

    // 账号或登录状态变化时同步开关文案、选中态与可用性。
    private void OnViewModelPropertyChanged(
        object? sender,
        System.ComponentModel.PropertyChangedEventArgs e
    )
    {
        _ = sender;
        if (e.PropertyName == nameof(DownloadPageViewModel.AccountName))
        {
            UpdateAccountToggle();
        }
        else if (e.PropertyName == nameof(DownloadPageViewModel.IsBusy))
        {
            AccountToggle.IsEnabled = ViewModel is { IsBusy: false };
        }
    }

    private void UpdateAccountToggle()
    {
        bool loggedIn = !string.IsNullOrWhiteSpace(ViewModel?.AccountName);
        AccountToggleText.Text = loggedIn ? "登出" : "登录";
        // 已登录用默认 subtle 样式，未登录用 accent。
        AccountToggle.Style = loggedIn ? null : (Style)Resources["AccountButtonAccentStyle"];
    }

    private async void AccountToggle_Click(object sender, RoutedEventArgs e)
    {
        _ = sender;
        _ = e;
        if (ViewModel is null || ViewModel.IsBusy)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(ViewModel.AccountName))
        {
            // 已登录：点击只弹确认，是否登出由确认按钮决定。
            LogoutFlyout.ShowAt(AccountToggle);
            return;
        }

        // 未登录：DD 未安装时先拦截，弹安装确认；装完由用户再点登录。
        if (!ViewModel.IsDepotDownloaderInstalled)
        {
            ContentDialog confirm = ContentDialogFactory.Create(
                XamlRoot,
                d =>
                {
                    d.Title = "需要先安装 DepotDownloader";
                    d.Content =
                        "登录 Steam 账号依赖 DepotDownloader 组件，当前尚未安装。是否现在安装？";
                    d.PrimaryButtonText = "安装";
                    d.CloseButtonText = "取消";
                    d.DefaultButton = ContentDialogButton.Primary;
                }
            );
            if (await confirm.ShowAsync() != ContentDialogResult.Primary)
            {
                return;
            }

            await RunDepotDownloaderInstallAsync();
            return;
        }

        // 未登录：执行登录；结束时按结果同步开关状态。
        await OnLoginButtonClickedAsync();
        UpdateAccountToggle();
    }

    private void LogoutConfirm_Click(object sender, RoutedEventArgs e)
    {
        _ = sender;
        _ = e;
        LogoutFlyout.Hide();
        ViewModel?.Logout();
        UpdateAccountToggle();
    }

    private void LogoutFlyout_Closed(object? sender, object e)
    {
        _ = sender;
        _ = e;
        // 取消确认时把开关同步回已登录选中态。
        UpdateAccountToggle();
    }

    private async void InstallDepotDownloader_Click(object sender, RoutedEventArgs e)
    {
        _ = sender;
        _ = e;
        await RunDepotDownloaderInstallAsync();
    }

    /// <summary>组件安装流程：进度对话框 → 安装 → 结果走 Toast，对话框立即关闭。</summary>
    private async Task RunDepotDownloaderInstallAsync()
    {
        if (ViewModel is null || ViewModel.IsBusy)
        {
            return;
        }

        ProgressBar bar = new() { Minimum = 0, Maximum = 100 };
        TextBlock status = new() { Text = "准备下载..." };
        DownloadProgressFooter footer = new();
        ContentDialog dialog = ContentDialogFactory.Create(
            XamlRoot,
            d =>
            {
                d.Title = "安装 DepotDownloader 组件";
                d.Content = new StackPanel { Spacing = 8, Children = { status, bar, footer.Root } };
                d.CloseButtonText = "取消";
            }
        );

        using CancellationTokenSource cts = new();
        dialog.Closing += (_, _) => cts.Cancel();
        IProgress<DownloadProgressInfo> reporter = new Progress<DownloadProgressInfo>(info =>
        {
            bar.Value = info.Percent;
            status.Text = info.Message;
            footer.Apply(info);
        });

        Task<ContentDialogResult> showing = dialog.ShowAsync().AsTask();
        await ViewModel.InstallDepotDownloaderAsync(reporter, cts.Token);
        // 结果走 Toast，对话框立即关闭；取消同样直接关闭。
        dialog.Hide();
        await showing;
    }

    private void OpenDownloadSourceSettings_Click(object sender, RoutedEventArgs e)
    {
        _ = sender;
        _ = e;
        // 人在设置页外时由导航服务记住目标分区，进入设置后补导航到「网络」。
        App.Services.GetRequiredService<INavigationService>().NavigateTo(NavigationTarget.Network);
    }

    private async Task OnLoginButtonClickedAsync()
    {
        if (ViewModel is null)
        {
            return;
        }

        // 二维码容器固定尺寸，Dialog 大小不随加载状态变化；白底等出码后再显示，加载时只有 ProgressRing。
        Image qrImage = new()
        {
            Visibility = Visibility.Collapsed,
            MaxWidth = 208,
            MaxHeight = 208,
        };
        ProgressRing qrRing = new()
        {
            IsActive = true,
            Width = 40,
            Height = 40,
        };
        Grid qrHost = new();
        qrHost.Children.Add(qrImage);
        qrHost.Children.Add(qrRing);
        Border qrContainer = new()
        {
            Width = 240,
            Height = 240,
            Background = new SolidColorBrush(Colors.Transparent),
            Child = qrHost,
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        TextBlock status = new()
        {
            Text = "正在获取登录二维码...",
            TextWrapping = TextWrapping.Wrap,
            HorizontalAlignment = HorizontalAlignment.Center,
            MaxWidth = 280,
        };
        StackPanel panel = new() { Spacing = 12, HorizontalAlignment = HorizontalAlignment.Center };
        // 状态文字放在图片上方：ContentDialog 高度不足时裁掉的是底部，文字必须保持可见。
        panel.Children.Add(status);
        panel.Children.Add(qrContainer);

        ContentDialog dialog = ContentDialogFactory.Create(
            XamlRoot,
            d =>
            {
                d.Title = "扫码登录 Steam 账号";
                d.Content = panel;
                d.CloseButtonText = "取消";
            }
        );

        // DD 刷新二维码时直接输出新方块，这里流式收集方块行，凑成正方形就重绘图片。
        List<string> qrBuffer = [];
        IProgress<string> reporter = new Progress<string>(line =>
        {
            if (IsQrRow(line))
            {
                qrBuffer.Add(line);
                if (QrAsciiParser.TryParse(qrBuffer, out bool[][]? matrix))
                {
                    // 按物理像素渲染再除以缩放比，避免高 DPI 下被插值放大产生毛边。
                    double raster = Math.Max(1.0, XamlRoot.RasterizationScale);
                    int scale = Math.Max(2, (int)Math.Round(8 * raster));
                    (WriteableBitmap bitmap, int pixelSize) = RenderMatrix(matrix, scale);
                    qrImage.Source = bitmap;
                    qrImage.Width = pixelSize / raster;
                    qrImage.Height = pixelSize / raster;
                    qrImage.Visibility = Visibility.Visible;
                    qrRing.IsActive = false;
                    qrRing.Visibility = Visibility.Collapsed;
                    qrContainer.Background = new SolidColorBrush(Colors.White);
                    status.Text = "请用 Steam 手机 App 扫描二维码。";
                }
            }
            else
            {
                qrBuffer.Clear();
            }
        });

        using CancellationTokenSource cts = new();
        dialog.Closing += (_, _) => cts.Cancel();
        Task<ContentDialogResult> showing = dialog.ShowAsync().AsTask();
        SteamLoginOutcome outcome = await ViewModel.LoginWithQrAsync(reporter, cts.Token);

        if (outcome.IsSuccess)
        {
            status.Text = $"登录成功：{outcome.AccountName}";
            await Task.Delay(800);
        }
        else
        {
            status.Text = outcome.ErrorMessage ?? "登录失败，请稍后重试。";
            await Task.Delay(1200);
        }

        dialog.Hide();
        await showing;
    }

    private static bool IsQrRow(string line)
    {
        return line.Length > 0 && line.AsSpan().IndexOfAnyExcept('█', ' ') < 0;
    }

    /// <summary>把模块矩阵渲染成位图；矩阵自带的 4 模块静区被裁掉，由外层白边代替。</summary>
    private static (WriteableBitmap Bitmap, int PixelSize) RenderMatrix(bool[][] matrix, int scale)
    {
        const int quietZone = 4;
        int fullSide = matrix.Length;
        int coreSide = Math.Max(1, fullSide - quietZone * 2);
        int size = coreSide * scale;
        WriteableBitmap bitmap = new(size, size);
        using Stream pixels = bitmap.PixelBuffer.AsStream();
        byte[] rowBytes = new byte[size * 4];
        for (int y = quietZone; y < fullSide - quietZone; y++)
        {
            for (int x = quietZone; x < fullSide - quietZone; x++)
            {
                byte channel = matrix[y][x] ? (byte)0 : byte.MaxValue;
                for (int s = 0; s < scale; s++)
                {
                    int offset = ((x - quietZone) * scale + s) * 4;
                    rowBytes[offset] = channel;
                    rowBytes[offset + 1] = channel;
                    rowBytes[offset + 2] = channel;
                    rowBytes[offset + 3] = byte.MaxValue;
                }
            }

            // 竖向同样放大 scale 倍：同一模块行连续写入 scale 次。
            for (int s = 0; s < scale; s++)
            {
                pixels.Write(rowBytes, 0, rowBytes.Length);
            }
        }

        bitmap.Invalidate();
        return (bitmap, size);
    }
}
