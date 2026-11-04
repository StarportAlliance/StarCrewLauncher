using System.Runtime.InteropServices;
using CommunityToolkit.WinUI.Behaviors;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;
using StarportAlliance.StarCrewLauncher.Models;
using StarportAlliance.StarCrewLauncher.Services;
using Windows.Graphics;
using Windows.System;
using WinRT.Interop;

namespace StarportAlliance.StarCrewLauncher;

public sealed partial class MainWindow : Window
{
    // 最小窗口尺寸为物理像素，不随 DPI 缩放
    private const int MinWidthPx = 1280;
    private const int MinHeightPx = 720;

    private static readonly Windows.UI.Color SolidBackgroundLight = Windows.UI.Color.FromArgb(
        255,
        243,
        243,
        243
    );
    private static readonly Windows.UI.Color SolidBackgroundDark = Windows.UI.Color.FromArgb(
        255,
        32,
        32,
        32
    );
    private static readonly string[] AccentResourceKeys =
    [
        "SystemAccentColor",
        "SystemAccentColorLight1",
        "SystemAccentColorLight2",
        "SystemAccentColorLight3",
        "SystemAccentColorDark1",
        "SystemAccentColorDark2",
        "SystemAccentColorDark3",
    ];

    private readonly IThemeSettings _themeSettings;
    private readonly ISystemFontCatalog _fontCatalog;
    private readonly IBackdropSupport _backdropSupport;
    private readonly FrameNavigationService _navigation;
    private readonly SubclassProc _subclassProc;
    private readonly nint _hwnd;
    private readonly HighContrastWatcher _highContrast;
    private bool _refreshingAccent;
    private bool _accentRefreshPending;
    private NavigationTarget _currentTarget = NavigationTarget.Home;
    private BackgroundKind _appliedBackground;
    private int _backgroundApplyVersion;

    internal MainWindow(
        IThemeSettings themeSettings,
        ISystemFontCatalog fontCatalog,
        INotificationService notifications,
        FrameNavigationService navigation,
        IFilePickerService filePicker,
        IAppInfo appInfo,
        HighContrastWatcher highContrast,
        IBackdropSupport backdropSupport
    )
    {
        ArgumentNullException.ThrowIfNull(themeSettings);
        ArgumentNullException.ThrowIfNull(fontCatalog);
        ArgumentNullException.ThrowIfNull(notifications);
        ArgumentNullException.ThrowIfNull(navigation);
        ArgumentNullException.ThrowIfNull(filePicker);
        ArgumentNullException.ThrowIfNull(appInfo);
        ArgumentNullException.ThrowIfNull(highContrast);
        ArgumentNullException.ThrowIfNull(backdropSupport);

        InitializeComponent();
        Title = "StarCrew Launcher";
        AppTitleBar.Subtitle = appInfo.IsDevelopment ? "Alpha" : null;
        AppWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "Assets", "icon.ico"));
        if (backdropSupport.IsMicaSupported)
        {
            SystemBackdrop = new MicaBackdrop();
        }

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
        AppWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;
        AppWindow.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;

        _themeSettings = themeSettings;
        _backdropSupport = backdropSupport;

        // Mica 需要 Win11；不支持时回退图片。自定义图片文件失联时按约定回退 Mica
        if (_themeSettings.Background == BackgroundKind.Mica && !_backdropSupport.IsMicaSupported)
        {
            _themeSettings.Background = BackgroundKind.Image;
        }
        if (
            _themeSettings.Background == BackgroundKind.Image
            && _themeSettings.UseCustomBackgroundImage
            && _themeSettings.BackgroundImagePath is { } backgroundPath
            && !File.Exists(backgroundPath)
        )
        {
            if (_backdropSupport.IsMicaSupported)
            {
                _themeSettings.Background = BackgroundKind.Mica;
            }
            else
            {
                _themeSettings.UseCustomBackgroundImage = false;
            }
        }

        _themeSettings.ThemeChanged += OnThemeChanged;
        _themeSettings.AccentColorsChanged += OnAccentColorsChanged;
        _themeSettings.ScrimOpacityChanged += OnScrimOpacityChanged;
        ApplyTheme(_themeSettings.Theme);
        ApplyBackground();

        _fontCatalog = fontCatalog;
        string? displayFont = _themeSettings.DisplayFont;
        if (
            displayFont is not null
            && !fontCatalog
                .GetFonts()
                .Any(font =>
                    string.Equals(font.FamilyName, displayFont, StringComparison.OrdinalIgnoreCase)
                )
        )
        {
            // 字体已卸载：回到默认并清除记录
            _themeSettings.DisplayFont = null;
        }

        _themeSettings.DisplayFontChanged += OnDisplayFontChanged;
        ApplyDisplayFont();

        _highContrast = highContrast;
        _highContrast.Changed += OnHighContrastChanged;
        if (Content is FrameworkElement accentRoot)
        {
            accentRoot.ActualThemeChanged += OnActualThemeChanged;
        }

        ApplyAccentColors(refreshThemeResources: false);

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

        ScrimLayer.OpacityTransition = new ScalarTransition
        {
            Duration = TimeSpan.FromMilliseconds(150),
        };

        Activated += OnFirstActivated;
        ToastBar.RegisterPropertyChangedCallback(InfoBar.IsOpenProperty, OnToastIsOpenChanged);
        AttachInputUnfocus();
    }

    // 输入框失焦统一在窗口根节点处理（逐页挂 KeyDown 的方案已多次回归）；handledEventsToo 兜住控件内部已标记处理的情况
    private void AttachInputUnfocus()
    {
        if (Content is UIElement root)
        {
            root.AddHandler(
                UIElement.PointerPressedEvent,
                new PointerEventHandler(Root_PointerPressed),
                true
            );
            // 松开时 TextBox 会经 TSF/RichEdit 把焦点抢回去，需再兜一次
            root.AddHandler(
                UIElement.PointerReleasedEvent,
                new PointerEventHandler(Root_PointerReleased),
                true
            );
            root.AddHandler(UIElement.KeyDownEvent, new KeyEventHandler(Root_KeyDown), true);
        }
    }

    private void Root_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        _ = sender;
        if (e.Key != VirtualKey.Escape || e.OriginalSource is not (TextBox or PasswordBox))
        {
            return;
        }

        e.Handled = true;
        Defocus();
    }

    private void Root_PointerPressed(object sender, PointerRoutedEventArgs e) =>
        TryDefocusFromPointer(sender, e.OriginalSource);

    private void Root_PointerReleased(object sender, PointerRoutedEventArgs e) =>
        TryDefocusFromPointer(sender, e.OriginalSource);

    private void TryDefocusFromPointer(object sender, object? source)
    {
        // 桌面端没有 CoreWindow，参数less 的 GetFocusedElement 恒为 null，必须带 XamlRoot
        if (
            (sender as UIElement)?.XamlRoot is not { } xamlRoot
            || FocusManager.GetFocusedElement(xamlRoot) is not Control focused
            || focused is not (TextBox or PasswordBox)
        )
        {
            return;
        }

        // 点击位置仍属于该输入控件自身时不抢焦点
        for (
            DependencyObject? node = source as DependencyObject;
            node is not null;
            node = VisualTreeHelper.GetParent(node)
        )
        {
            if (node == focused)
            {
                return;
            }
        }

        Defocus();
    }

    private void Defocus()
    {
        // Programmatic 在键盘输入后会被强制改写成 Keyboard，故使用 Pointer
        Content?.Focus(FocusState.Pointer);
    }

    private void OnThemeChanged(object? sender, AppTheme theme)
    {
        ApplyTheme(theme);
        ApplyAccentColors(refreshThemeResources: true);
    }

    private void OnScrimOpacityChanged(object? sender, EventArgs e)
    {
        _ = sender;
        _ = e;
        UpdateScrimLayer(animate: false);
    }

    private void ApplyBackground()
    {
        int version = ++_backgroundApplyVersion;
        BackgroundKind background = _themeSettings.Background;
        _appliedBackground = background;

        bool solid = background == BackgroundKind.SolidColor;
        SolidBackgroundLayer.Visibility = solid ? Visibility.Visible : Visibility.Collapsed;
        BackgroundImage.Visibility =
            background == BackgroundKind.Image ? Visibility.Visible : Visibility.Collapsed;

        if (solid)
        {
            ApplySolidBackgroundColor();
        }
        else
        {
            CustomSolidBackgroundLayer.Visibility = Visibility.Collapsed;
        }

        if (background == BackgroundKind.Image)
        {
            _ = ApplyBackgroundImageAsync(version);
        }

        UpdateScrimLayer(animate: false);
    }

    private void ApplySolidBackgroundColor()
    {
        string? color = _themeSettings.SolidBackgroundFollowsTheme
            ? null
            : _themeSettings.SolidBackgroundColor;
        if (color is not null && AccentPalette.TryParse(color, out AccentRgb rgb))
        {
            CustomSolidBackgroundLayer.Background = new SolidColorBrush(ToColor(rgb));
            CustomSolidBackgroundLayer.Visibility = Visibility.Visible;
            return;
        }

        CustomSolidBackgroundLayer.Visibility = Visibility.Collapsed;
        ElementTheme actualTheme = Content is FrameworkElement root
            ? root.ActualTheme
            : ElementTheme.Dark;
        SolidBackgroundLayer.Background = new SolidColorBrush(
            actualTheme == ElementTheme.Light ? SolidBackgroundLight : SolidBackgroundDark
        );
    }

    private async Task ApplyBackgroundImageAsync(int version)
    {
        string? customPath = _themeSettings.UseCustomBackgroundImage
            ? _themeSettings.BackgroundImagePath
            : null;
        if (customPath is null)
        {
            BackgroundImage.Source = CreateBuiltInBackgroundSource();
            return;
        }

        try
        {
            using FileStream stream = File.OpenRead(customPath);
            BitmapImage bitmap = new();
            await bitmap.SetSourceAsync(stream.AsRandomAccessStream());
            if (
                version == _backgroundApplyVersion
                && _themeSettings.Background == BackgroundKind.Image
            )
            {
                BackgroundImage.Source = bitmap;
            }
        }
        catch (Exception ex)
        {
            // 外部图片可能损坏或权限变化，解码失败统一按约定回退
            Serilog.Log.Warning(ex, "背景图片加载失败：{Path}", customPath);
            if (version != _backgroundApplyVersion)
            {
                return;
            }

            if (_backdropSupport.IsMicaSupported)
            {
                _themeSettings.Background = BackgroundKind.Mica;
                ApplyBackground();
            }
            else
            {
                _themeSettings.UseCustomBackgroundImage = false;
                BackgroundImage.Source = CreateBuiltInBackgroundSource();
            }
        }
    }

    private BitmapImage CreateBuiltInBackgroundSource() =>
        new(
            new Uri(
                BuiltInBackgroundCatalog.Resolve(_themeSettings.BuiltInBackgroundId).ImageSource
            )
        );

    private void UpdateScrimLayer(bool animate = true)
    {
        // 遮罩只为图片背景压住内容页；Mica 与纯色直接显示背景
        double opacity =
            _appliedBackground == BackgroundKind.Image && _currentTarget != NavigationTarget.Home
                ? _themeSettings.ScrimOpacity
                : 0;

        if (animate)
        {
            ScrimLayer.Opacity = opacity;
            return;
        }

        // 背景设置变化时跳过淡入，避免切换瞬间暴露原图
        ScalarTransition? transition = ScrimLayer.OpacityTransition;
        ScrimLayer.OpacityTransition = null;
        ScrimLayer.Opacity = opacity;
        ScrimLayer.OpacityTransition = transition;
    }

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
            },
            message.Action
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
        _currentTarget = target;
        UpdateScrimLayer();
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

    private void OnDisplayFontChanged(object? sender, EventArgs e)
    {
        _ = sender;
        _ = e;
        ApplyDisplayFont();
        RefreshThemeResources();
    }

    // 字体写入应用级资源，并同步覆盖已存在元素的本地字体：ThemeResource 不会刷新已创建的文字
    private void ApplyDisplayFont()
    {
        string? name = _themeSettings.DisplayFont;
        FontFamily family = name is null ? FontFamily.XamlAutoFontFamily : new FontFamily(name);
        Application.Current.Resources["AppDisplayFontFamily"] = family;
        if (name is null)
        {
            Application.Current.Resources.Remove("ContentControlThemeFontFamily");
        }
        else
        {
            Application.Current.Resources["ContentControlThemeFontFamily"] = family;
        }

        if (Content is FrameworkElement root)
        {
            ApplyFontToTree(root, family);
        }
    }

    // 图标控件的模板内部也有 TextBlock，覆盖字体会让图标变成文字
    private static void ApplyFontToTree(DependencyObject node, FontFamily family)
    {
        if (node is IconElement)
        {
            return;
        }

        switch (node)
        {
            case TextBlock textBlock:
                if (!IsIconFont(textBlock.FontFamily))
                {
                    textBlock.FontFamily = family;
                }

                break;
            case Control control:
                if (!IsIconFont(control.FontFamily))
                {
                    control.FontFamily = family;
                }

                break;
            case ContentPresenter presenter:
                if (!IsIconFont(presenter.FontFamily))
                {
                    presenter.FontFamily = family;
                }

                break;
            case RichTextBlock richTextBlock:
                if (!IsIconFont(richTextBlock.FontFamily))
                {
                    richTextBlock.FontFamily = family;
                }

                break;
        }

        int count = VisualTreeHelper.GetChildrenCount(node);
        for (int index = 0; index < count; index++)
        {
            ApplyFontToTree(VisualTreeHelper.GetChild(node, index), family);
        }
    }

    // InfoBar 等模板用 TextBlock 画图标，字形只存在于符号字体
    private static bool IsIconFont(FontFamily? fontFamily) =>
        fontFamily?.Source is { } source
        && (
            source.Contains("Segoe Fluent Icons", StringComparison.OrdinalIgnoreCase)
            || source.Contains("Segoe MDL2 Assets", StringComparison.OrdinalIgnoreCase)
        );

    private void OnAccentColorsChanged(object? sender, EventArgs e)
    {
        _ = sender;
        _ = e;
        ApplyAccentColors(refreshThemeResources: true);
    }

    private void OnActualThemeChanged(FrameworkElement sender, object args)
    {
        _ = sender;
        _ = args;
        // 强制刷新期间的主题抖动不应再写资源
        if (_refreshingAccent)
        {
            return;
        }

        ApplyAccentColors(refreshThemeResources: false);
        if (_appliedBackground == BackgroundKind.SolidColor)
        {
            ApplySolidBackgroundColor();
        }
    }

    private void OnHighContrastChanged(object? sender, EventArgs e)
    {
        _ = sender;
        _ = e;
        ApplyAccentColors(refreshThemeResources: false);
    }

    // 强调色写在 Application.Resources 顶层键，覆盖主题字典；只对当前生效的主题写，切换主题时重算
    private void ApplyAccentColors(bool refreshThemeResources)
    {
        ResourceDictionary resources = Application.Current.Resources;
        string? custom = null;
        if (!_highContrast.IsEnabled && Content is FrameworkElement root)
        {
            custom =
                root.ActualTheme == ElementTheme.Dark
                    ? _themeSettings.DarkAccentColor
                    : _themeSettings.LightAccentColor;
        }

        if (custom is null)
        {
            foreach (string key in AccentResourceKeys)
            {
                resources.Remove(key);
            }
        }
        else
        {
            AccentShades shades = AccentPalette.Derive(custom);
            resources["SystemAccentColor"] = ToColor(shades.Base);
            resources["SystemAccentColorLight1"] = ToColor(shades.Light1);
            resources["SystemAccentColorLight2"] = ToColor(shades.Light2);
            resources["SystemAccentColorLight3"] = ToColor(shades.Light3);
            resources["SystemAccentColorDark1"] = ToColor(shades.Dark1);
            resources["SystemAccentColorDark2"] = ToColor(shades.Dark2);
            resources["SystemAccentColorDark3"] = ToColor(shades.Dark3);
        }

        if (refreshThemeResources)
        {
            QueueAccentRefresh();
        }
    }

    // 改写资源不会通知已解析的 ThemeResource，只能靠切换一次 RequestedTheme 让子树重新解析；
    // 同一轮内切回不会产生中间渲染，按帧合并避免拖拽取色时反复全树重解析
    private void QueueAccentRefresh()
    {
        if (_accentRefreshPending)
        {
            return;
        }

        _accentRefreshPending = true;
        DispatcherQueue.TryEnqueue(() =>
        {
            _accentRefreshPending = false;
            RefreshThemeResources();
        });
    }

    private void RefreshThemeResources()
    {
        if (Content is not FrameworkElement root)
        {
            return;
        }

        ElementTheme requested = root.RequestedTheme;
        _refreshingAccent = true;
        try
        {
            root.RequestedTheme =
                requested == ElementTheme.Light ? ElementTheme.Dark : ElementTheme.Light;
            root.RequestedTheme = requested;
        }
        finally
        {
            _refreshingAccent = false;
        }
    }

    private static Windows.UI.Color ToColor(AccentRgb rgb) =>
        Windows.UI.Color.FromArgb(255, rgb.R, rgb.G, rgb.B);

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
        NotificationAction? action = null,
        int autoCloseSeconds = 4
    )
    {
        Notification notification = new()
        {
            Title = title,
            Message = message ?? string.Empty,
            Severity = severity,
            Duration = TimeSpan.FromSeconds(autoCloseSeconds),
        };

        if (action is not null)
        {
            Button button = new() { Content = action.Label };
            button.Click += (_, _) => action.Callback();
            notification.ActionButton = button;
        }

        ToastQueue.Show(notification);
    }

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
    private const uint WM_THEMECHANGED = 0x031A;

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

        if (uMsg == WM_THEMECHANGED)
        {
            _highContrast.NotifyThemeChanged();
        }

        return DefSubclassProc(hWnd, uMsg, wParam, lParam);
    }
}
