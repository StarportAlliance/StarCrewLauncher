using System.Runtime.InteropServices;
using CommunityToolkit.WinUI.Behaviors;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using StarCrew.Launcher.Models;
using StarCrew.Launcher.Services;
using Windows.Graphics;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace StarCrew.Launcher;

public sealed partial class MainWindow : Window
{
    // 最小窗口尺寸为物理像素，不随 DPI 缩放。
    private const int MinWidthPx = 1280;
    private const int MinHeightPx = 720;

    private readonly GameLauncher _launcher = new(
        new SteamGameLocator(new WindowsSteamEnvironment()),
        new ProcessStarter()
    );
    private readonly SubclassProc _subclassProc;
    private readonly nint _hwnd;

    public MainWindow()
    {
        InitializeComponent();
        Title = "StarCrew Launcher";
        AppWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "Assets", "icon.ico"));
        SystemBackdrop = new MicaBackdrop();
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
        AppWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;
        AppWindow.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;

        _hwnd = WindowNative.GetWindowHandle(this);
        PlaceWindow();
        _subclassProc = WndSubclassProc;
        SetWindowSubclass(_hwnd, _subclassProc, 0, 0);

        Activated += OnFirstActivated;
        ToastBar.RegisterPropertyChangedCallback(InfoBar.IsOpenProperty, OnToastIsOpenChanged);
    }

    private void OnFirstActivated(object sender, WindowActivatedEventArgs args)
    {
        Activated -= OnFirstActivated;
        PlaceWindow();
    }

    private void AppTitleBar_PaneToggleRequested(TitleBar sender, object args)
    {
        NavView.IsPaneOpen = !NavView.IsPaneOpen;
    }

    /// <summary>按主显示器工作区取 16:9 默认尺寸并居中。</summary>
    private void PlaceWindow()
    {
        double scale = GetDpiForWindow(_hwnd) / 96.0;
        RectInt32 work = DisplayArea
            .GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Primary)
            .WorkArea;
        double workW = work.Width / scale;
        double workH = work.Height / scale;

        double minW = MinWidthPx / scale;
        double minH = MinHeightPx / scale;

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
        AppWindow.Move(
            new PointInt32(
                work.X + (work.Width - widthPhys) / 2,
                work.Y + (work.Height - heightPhys) / 2
            )
        );
    }

    private async void LaunchButton_Click(object sender, RoutedEventArgs e)
    {
        LaunchButton.IsEnabled = false;
        LaunchButton.Content = "正在启动...";

        try
        {
            LaunchResult result = await _launcher.LaunchAsync();
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

    /// <summary>自动定位失败时让用户手动选 exe 并启动。</summary>
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
            ShowToast(
                "启动失败",
                $"启动所选文件时发生错误：{error}",
                InfoBarSeverity.Error,
                autoCloseSeconds: 6
            );
        }
    }

    /// <summary>右上角应用内通知，经 Toolkit 排队逐条展示，到期自动关闭（悬停暂停计时，可手动关闭），进退场带滑动淡入淡出。</summary>
    private void ShowToast(
        string title,
        string message,
        InfoBarSeverity severity,
        int autoCloseSeconds = 4
    )
    {
        ToastQueue.Show(
            new Notification
            {
                Title = title,
                Message = message,
                Severity = severity,
                Duration = TimeSpan.FromSeconds(autoCloseSeconds),
            }
        );
    }

    // 退场动画播完前先取消关闭，播完再真正关闭；Toolkit 只监听 Closed，排队逻辑不受影响。
    private bool _isToastExitAnimating;

    private void OnToastIsOpenChanged(DependencyObject sender, DependencyProperty dp)
    {
        // Closing 取消后 InfoBar 会把 IsOpen 同步弹回 true：退场进行中时忽略，否则退场会被进场覆盖。
        if (sender.GetValue(dp) is true && !_isToastExitAnimating)
        {
            ToastExitStoryboard.Stop();
            ToastEnterStoryboard.Begin();
        }
    }

    private void ToastBar_Closing(InfoBar sender, InfoBarClosingEventArgs args)
    {
        if (_isToastExitAnimating)
        {
            _isToastExitAnimating = false;
            return;
        }

        args.Cancel = true;
        _isToastExitAnimating = true;
        ToastEnterStoryboard.Stop();
        ToastExitStoryboard.Begin();
    }

    private void ToastExitStoryboard_Completed(object sender, object e)
    {
        ToastBar.IsOpen = false;
    }

    private async Task<string?> PickGameExeAsync()
    {
        FileOpenPicker picker = new() { SuggestedStartLocation = PickerLocationId.ComputerFolder };
        picker.FileTypeFilter.Add(".exe");
        InitializeWithWindow.Initialize(picker, _hwnd);

        StorageFile? file = await picker.PickSingleFileAsync();
        return file?.Path;
    }

    #region 窗口最小尺寸（WM_GETMINMAXINFO 子类化）

    private delegate nint SubclassProc(
        nint hWnd,
        uint uMsg,
        nint wParam,
        nint lParam,
        nuint uIdSubclass,
        nuint dwRefData
    );

    [DllImport("comctl32.dll", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern bool SetWindowSubclass(
        nint hWnd,
        SubclassProc pfnSubclass,
        nuint uIdSubclass,
        nuint dwRefData
    );

    [DllImport("comctl32.dll")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern nint DefSubclassProc(nint hWnd, uint uMsg, nint wParam, nint lParam);

    [DllImport("user32.dll")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
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
        nint hWnd,
        uint uMsg,
        nint wParam,
        nint lParam,
        nuint uIdSubclass,
        nuint dwRefData
    )
    {
        if (uMsg == WM_GETMINMAXINFO)
        {
            MinMaxInfo mmi = Marshal.PtrToStructure<MinMaxInfo>(lParam);
            mmi.MinTrackSize = new NativePoint { X = MinWidthPx, Y = MinHeightPx };
            Marshal.StructureToPtr(mmi, lParam, false);
            return nint.Zero;
        }

        return DefSubclassProc(hWnd, uMsg, wParam, lParam);
    }

    #endregion
}
