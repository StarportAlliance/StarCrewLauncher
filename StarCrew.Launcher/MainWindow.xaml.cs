using System.Runtime.InteropServices;
using CommunityToolkit.WinUI.Behaviors;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using StarCrew.Launcher.Models;
using StarCrew.Launcher.Services;
using Windows.Graphics;
using WinRT.Interop;

namespace StarCrew.Launcher;

public sealed partial class MainWindow : Window
{
    // 最小窗口尺寸为物理像素，不随 DPI 缩放
    private const int MinWidthPx = 1280;
    private const int MinHeightPx = 720;

    private readonly IThemeSettings _themeSettings;
    private readonly FrameNavigationService _navigation;
    private readonly SubclassProc _subclassProc;
    private readonly nint _hwnd;

    internal MainWindow(
        IThemeSettings themeSettings,
        INotificationService notifications,
        FrameNavigationService navigation,
        IFilePickerService filePicker
    )
    {
        ArgumentNullException.ThrowIfNull(themeSettings);
        ArgumentNullException.ThrowIfNull(notifications);
        ArgumentNullException.ThrowIfNull(navigation);
        ArgumentNullException.ThrowIfNull(filePicker);

        InitializeComponent();
        Title = "StarCrew Launcher";
        AppWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "Assets", "icon.ico"));
        SystemBackdrop = new MicaBackdrop();
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
        AppWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;
        AppWindow.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;

        _themeSettings = themeSettings;
        _themeSettings.ThemeChanged += OnThemeChanged;
        ApplyTheme(_themeSettings.Theme);

        _hwnd = WindowNative.GetWindowHandle(this);
        filePicker.AttachHostWindow(_hwnd);

        _navigation = navigation;
        _navigation.AttachMainFrame(ContentFrame);
        _navigation.MainFrameNavigated += SyncNavSelection;
        _navigation.MainFrameNavigated += OnMainFrameNavigated;

        notifications.Requested += OnNotificationRequested;

        PlaceWindow();
        _subclassProc = WndSubclassProc;
        SetWindowSubclass(_hwnd, _subclassProc, 0, 0);

        AcrylicLayer.OpacityTransition = new ScalarTransition
        {
            Duration = TimeSpan.FromMilliseconds(150),
        };

        Activated += OnFirstActivated;
        ToastBar.RegisterPropertyChangedCallback(InfoBar.IsOpenProperty, OnToastIsOpenChanged);
    }

    private void OnThemeChanged(object? sender, AppTheme theme) => ApplyTheme(theme);

    private void OnNotificationRequested(object? sender, NotificationMessage message) =>
        ShowToast(
            message.Title,
            message.Message,
            message.Severity switch
            {
                NotificationSeverity.Success => InfoBarSeverity.Success,
                NotificationSeverity.Warning => InfoBarSeverity.Warning,
                NotificationSeverity.Error => InfoBarSeverity.Error,
                _ => InfoBarSeverity.Informational,
            }
        );

    private void OnFirstActivated(object sender, WindowActivatedEventArgs args)
    {
        Activated -= OnFirstActivated;
        PlaceWindow();
    }

    private void AppTitleBar_PaneToggleRequested(TitleBar sender, object args) =>
        NavView.IsPaneOpen = !NavView.IsPaneOpen;

    private void AppTitleBar_BackRequested(TitleBar sender, object args) => _navigation.GoBack();

    private void OnMainFrameNavigated(NavigationTarget target)
    {
        AcrylicLayer.Opacity = target == NavigationTarget.Home ? 0 : 1;
        SyncBackButton();
    }

    private void SyncBackButton() => AppTitleBar.IsBackButtonEnabled = _navigation.CanGoBack;

    private bool _initialNavigated;

    private void NavView_Loaded(object sender, RoutedEventArgs e)
    {
        // 首屏等 Frame 进视觉树再导航，在构造器里 Navigate 会被静默丢弃
        _initialNavigated = true;
        _navigation.NavigateTo(NavigationTarget.Home, new SuppressNavigationTransitionInfo());
    }

    private void NavView_SelectionChanged(
        NavigationView sender,
        NavigationViewSelectionChangedEventArgs args
    )
    {
        if (!_initialNavigated)
        {
            return;
        }

        NavigationTarget target = NavigationTarget.Home;
        if (args.IsSettingsSelected)
        {
            target = NavigationTarget.Settings;
        }
        else if (args.SelectedItem is NavigationViewItem selected && selected.Tag is string tag)
        {
            target = tag switch
            {
                "Instances" => NavigationTarget.Instances,
                "Download" => NavigationTarget.Download,
                _ => NavigationTarget.Home,
            };
        }

        _navigation.NavigateTo(target, args.RecommendedNavigationTransitionInfo);
    }

    private void SyncNavSelection(NavigationTarget target)
    {
        if (target == NavigationTarget.Settings)
        {
            NavView.SelectedItem = NavView.SettingsItem;
            return;
        }

        // 安装详情页是下载页的二级页面，侧栏本来就停在上面，再跳反倒会回到上级页面...
        if (target == NavigationTarget.Install)
        {
            // ...故忽略处理
            return;
        }

        string? tag = target switch
        {
            NavigationTarget.Home => "Home",
            NavigationTarget.Instances => "Instances",
            NavigationTarget.Download or NavigationTarget.Install => "Download",
            _ => null,
        };
        foreach (object item in NavView.MenuItems)
        {
            if (
                item is NavigationViewItem { Tag: string itemTag }
                && string.Equals(itemTag, tag, StringComparison.Ordinal)
            )
            {
                NavView.SelectedItem = item;
                return;
            }
        }
    }

    private void ApplyTheme(AppTheme theme)
    {
        if (Content is FrameworkElement root)
        {
            root.RequestedTheme = theme switch
            {
                AppTheme.Light => ElementTheme.Light,
                AppTheme.Dark => ElementTheme.Dark,
                _ => ElementTheme.Default,
            };
        }
    }

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

    private void ShowToast(
        string title,
        string? message,
        InfoBarSeverity severity,
        int autoCloseSeconds = 4
    ) =>
        ToastQueue.Show(
            new Notification
            {
                Title = title,
                Message = message ?? string.Empty,
                Severity = severity,
                Duration = TimeSpan.FromSeconds(autoCloseSeconds),
            }
        );

    private bool _isToastExitAnimating;

    private void OnToastIsOpenChanged(DependencyObject sender, DependencyProperty dp)
    {
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

    private void ToastExitStoryboard_Completed(object sender, object e) => ToastBar.IsOpen = false;

    private delegate nint SubclassProc(
        nint hWnd,
        uint uMsg,
        nint wParam,
        nint lParam,
        nuint uIdSubclass,
        nuint dwRefData
    );

    [LibraryImport("comctl32.dll", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetWindowSubclass(
        nint hWnd,
        SubclassProc pfnSubclass,
        nuint uIdSubclass,
        nuint dwRefData
    );

    [LibraryImport("comctl32.dll")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial nint DefSubclassProc(nint hWnd, uint uMsg, nint wParam, nint lParam);

    [LibraryImport("user32.dll")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial uint GetDpiForWindow(nint hWnd);

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
}
