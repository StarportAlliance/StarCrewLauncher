using System.Runtime.InteropServices;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using StarCrew.Launcher.Services;
using Windows.Graphics;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace StarCrew.Launcher;

public sealed partial class MainWindow : Window
{
    // 最小窗口尺寸为物理像素，不随 DPI 缩放。
    private const int MinWidthPx = 1280;
    private const int MinHeightPx = 720;

    private readonly GameLauncher _launcher = new();
    private readonly SubclassProc _subclassProc;
    private readonly nint _hwnd;
    private DispatcherTimer? _toastTimer;

    public MainWindow()
    {
        InitializeComponent();
        Title = "StarCrew Launcher";
        SystemBackdrop = new MicaBackdrop();
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(NavView);
        AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
        AppWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;
        AppWindow.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;

        _hwnd = WindowNative.GetWindowHandle(this);
        PlaceWindow();
        _subclassProc = WndSubclassProc;
        SetWindowSubclass(_hwnd, _subclassProc, 0, 0);

        // 构造期尺寸可能被系统覆盖，首次激活时按同一规则再放置一次（幂等）。
        Activated += OnFirstActivated;
    }

    private void OnFirstActivated(object sender, WindowActivatedEventArgs args)
    {
        Activated -= OnFirstActivated;
        PlaceWindow();
    }

    /// <summary>按主显示器工作区自适应的 16:9 默认尺寸，并将窗口居中。</summary>
    private void PlaceWindow()
    {
        double scale = GetDpiForWindow(_hwnd) / 96.0;
        RectInt32 work = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Primary).WorkArea;
        double workW = work.Width / scale;
        double workH = work.Height / scale;

        // 最小尺寸是物理像素，换算为 DIP 后参与默认尺寸钳制，
        // 保证默认尺寸的物理像素不小于最小尺寸。
        double minW = MinWidthPx / scale;
        double minH = MinHeightPx / scale;

        // 默认宽度取工作区宽减去两侧边距，高度按 16:9 推导；超高时改按高度推导。
        double defaultW = workW - 240;
        double defaultH = defaultW * 9.0 / 16.0;
        if (defaultH > workH - 120)
        {
            defaultH = workH - 120;
            defaultW = defaultH * 16.0 / 9.0;
        }

        defaultW = Math.Clamp(defaultW, Math.Min(minW, workW), workW);
        defaultH = Math.Clamp(defaultH, Math.Min(minH, workH), workH);

        int widthPhys = (int)(defaultW * scale);
        int heightPhys = (int)(defaultH * scale);
        AppWindow.Resize(new SizeInt32(widthPhys, heightPhys));
        AppWindow.Move(new PointInt32(
            work.X + (work.Width - widthPhys) / 2,
            work.Y + (work.Height - heightPhys) / 2));
    }

    private async void LaunchButton_Click(object sender, RoutedEventArgs e)
    {
        LaunchButton.IsEnabled = false;
        LaunchButton.Content = "正在启动...";

        try
        {
            var result = await _launcher.LaunchAsync();
            if (result.IsSuccess)
            {
                ShowToast("启动成功", result.Message, InfoBarSeverity.Success);
                return;
            }

            await LaunchByManualPickAsync();
        }
        finally
        {
            LaunchButton.IsEnabled = true;
            LaunchButton.Content = "启动游戏";
        }
    }

    /// <summary>自动定位失败时弹出文件选择框让用户手动定位游戏并启动。</summary>
    private async Task LaunchByManualPickAsync()
    {
        string? picked = await PickGameExeAsync();
        if (picked is null)
        {
            ShowToast("已取消", "已取消选择，点击启动游戏可重试。", InfoBarSeverity.Informational);
            return;
        }

        if (_launcher.TryLaunchExe(picked, out string? error))
        {
            ShowToast("启动成功", "已启动手动选择的游戏。", InfoBarSeverity.Success);
        }
        else
        {
            ShowToast("启动失败", $"启动所选文件时发生错误：{error}", InfoBarSeverity.Error, autoCloseSeconds: 6);
        }
    }

    /// <summary>在右上角滑入一条 Toast，数秒后自动滑出关闭，也可手动关闭。</summary>
    private void ShowToast(string title, string message, InfoBarSeverity severity, int autoCloseSeconds = 4)
    {
        StopToastTimer();
        ToastPanel.Children.Clear();

        var bar = new InfoBar
        {
            Title = title,
            Message = message,
            Severity = severity,
            IsOpen = true,
            IsClosable = true,
            Opacity = 0,
            RenderTransform = new TranslateTransform(),
        };
        bar.CloseButtonClick += (_, _) =>
        {
            StopToastTimer();
            ToastPanel.Children.Remove(bar);
        };
        ToastPanel.Children.Add(bar);

        var flyIn = new Storyboard();
        var slideIn = new DoubleAnimation
        {
            Duration = TimeSpan.FromMilliseconds(250),
            From = 60,
            To = 0,
            EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut },
        };
        Storyboard.SetTarget(slideIn, bar);
        Storyboard.SetTargetProperty(slideIn, "(UIElement.RenderTransform).(TranslateTransform.X)");
        var fadeIn = new DoubleAnimation
        {
            Duration = TimeSpan.FromMilliseconds(300),
            From = 0,
            To = 1,
        };
        Storyboard.SetTarget(fadeIn, bar);
        Storyboard.SetTargetProperty(fadeIn, "Opacity");
        flyIn.Children.Add(slideIn);
        flyIn.Children.Add(fadeIn);
        flyIn.Begin();

        _toastTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(autoCloseSeconds) };
        _toastTimer.Tick += (_, _) => DismissToast(bar);
        _toastTimer.Start();
    }

    private void DismissToast(InfoBar bar)
    {
        StopToastTimer();

        var flyOut = new Storyboard();
        var slideOut = new DoubleAnimation
        {
            Duration = TimeSpan.FromMilliseconds(250),
            From = 0,
            To = 60,
            EasingFunction = new BackEase { EasingMode = EasingMode.EaseIn },
        };
        Storyboard.SetTarget(slideOut, bar);
        Storyboard.SetTargetProperty(slideOut, "(UIElement.RenderTransform).(TranslateTransform.X)");
        var fadeOut = new DoubleAnimation
        {
            Duration = TimeSpan.FromMilliseconds(250),
            From = 1,
            To = 0,
        };
        Storyboard.SetTarget(fadeOut, bar);
        Storyboard.SetTargetProperty(fadeOut, "Opacity");
        flyOut.Children.Add(slideOut);
        flyOut.Children.Add(fadeOut);
        flyOut.Completed += (_, _) => ToastPanel.Children.Remove(bar);
        flyOut.Begin();
    }

    private void StopToastTimer()
    {
        _toastTimer?.Stop();
        _toastTimer = null;
    }

    private async Task<string?> PickGameExeAsync()
    {
        var picker = new FileOpenPicker
        {
            SuggestedStartLocation = PickerLocationId.ComputerFolder,
        };
        picker.FileTypeFilter.Add(".exe");
        InitializeWithWindow.Initialize(picker, _hwnd);

        var file = await picker.PickSingleFileAsync();
        return file?.Path;
    }

    #region 窗口最小尺寸（WM_GETMINMAXINFO 子类化）

    private delegate nint SubclassProc(
        nint hWnd, uint uMsg, nint wParam, nint lParam, nuint uIdSubclass, nuint dwRefData);

    [DllImport("comctl32.dll", SetLastError = true)]
    private static extern bool SetWindowSubclass(
        nint hWnd, SubclassProc pfnSubclass, nuint uIdSubclass, nuint dwRefData);

    [DllImport("comctl32.dll")]
    private static extern nint DefSubclassProc(nint hWnd, uint uMsg, nint wParam, nint lParam);

    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(nint hWnd);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MinMaxInfo
    {
        public NativePoint Reserved;
        public NativePoint MaxSize;
        public NativePoint MaxPosition;
        public NativePoint MinTrackSize;
        public NativePoint MaxTrackSize;
    }

    private const uint WM_GETMINMAXINFO = 0x24;

    private nint WndSubclassProc(
        nint hWnd, uint uMsg, nint wParam, nint lParam, nuint uIdSubclass, nuint dwRefData)
    {
        if (uMsg == WM_GETMINMAXINFO)
        {
            // 最小尺寸与 MINMAXINFO 均为物理像素，直接使用，不做 DPI 换算。
            var mmi = Marshal.PtrToStructure<MinMaxInfo>(lParam);
            mmi.MinTrackSize = new NativePoint
            {
                X = MinWidthPx,
                Y = MinHeightPx,
            };
            Marshal.StructureToPtr(mmi, lParam, false);
            return nint.Zero;
        }

        return DefSubclassProc(hWnd, uMsg, wParam, lParam);
    }

    #endregion
}
