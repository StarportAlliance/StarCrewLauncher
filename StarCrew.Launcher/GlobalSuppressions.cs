// 分析器压制集中登记处。
// 新增压制前先确认：是规则不适用还是代码真有问题。
using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(
    "Maintainability",
    "CA1515:TypesCanBeMadeInternal",
    Justification = "XAML 代码隐藏类按 WinUI 约定保持 public。",
    Scope = "type",
    Target = "~T:StarCrew.Launcher.App"
)]
[assembly: SuppressMessage(
    "Maintainability",
    "CA1515:TypesCanBeMadeInternal",
    Justification = "同上：XAML 代码隐藏类保持 public。",
    Scope = "type",
    Target = "~T:StarCrew.Launcher.MainWindow"
)]
[assembly: SuppressMessage(
    "Maintainability",
    "CA1515:TypesCanBeMadeInternal",
    Justification = "同上：XAML 代码隐藏类保持 public。",
    Scope = "type",
    Target = "~T:StarCrew.Launcher.Views.SettingsPage"
)]
[assembly: SuppressMessage(
    "Maintainability",
    "CA1515:TypesCanBeMadeInternal",
    Justification = "同上：XAML 代码隐藏类保持 public。",
    Scope = "type",
    Target = "~T:StarCrew.Launcher.Views.AppearancePage"
)]
[assembly: SuppressMessage(
    "Maintainability",
    "CA1515:TypesCanBeMadeInternal",
    Justification = "同上：XAML 代码隐藏类保持 public。",
    Scope = "type",
    Target = "~T:StarCrew.Launcher.Views.AdvancedPage"
)]
[assembly: SuppressMessage(
    "Maintainability",
    "CA1515:TypesCanBeMadeInternal",
    Justification = "同上：XAML 代码隐藏类保持 public。",
    Scope = "type",
    Target = "~T:StarCrew.Launcher.Views.AboutPage"
)]
[assembly: SuppressMessage(
    "Maintainability",
    "CA1515:TypesCanBeMadeInternal",
    Justification = "同上：XAML 代码隐藏类保持 public。",
    Scope = "type",
    Target = "~T:StarCrew.Launcher.Views.HomePage"
)]
[assembly: SuppressMessage(
    "Maintainability",
    "CA1515:TypesCanBeMadeInternal",
    Justification = "同上：XAML 代码隐藏类保持 public。",
    Scope = "type",
    Target = "~T:StarCrew.Launcher.Views.InstancesPage"
)]
[assembly: SuppressMessage(
    "Maintainability",
    "CA1515:TypesCanBeMadeInternal",
    Justification = "同上：XAML 代码隐藏类保持 public。",
    Scope = "type",
    Target = "~T:StarCrew.Launcher.Views.DownloadPage"
)]
[assembly: SuppressMessage(
    "Maintainability",
    "CA1515:TypesCanBeMadeInternal",
    Justification = "同上：XAML 代码隐藏类保持 public。",
    Scope = "type",
    Target = "~T:StarCrew.Launcher.Views.DownloadBasePage"
)]
[assembly: SuppressMessage(
    "Maintainability",
    "CA1515:TypesCanBeMadeInternal",
    Justification = "同上：XAML 代码隐藏类保持 public。",
    Scope = "type",
    Target = "~T:StarCrew.Launcher.Views.DownloadAccountPage"
)]
[assembly: SuppressMessage(
    "Maintainability",
    "CA1515:TypesCanBeMadeInternal",
    Justification = "同上：XAML 代码隐藏类保持 public。",
    Scope = "type",
    Target = "~T:StarCrew.Launcher.Views.DownloadInstallPage"
)]
[assembly: SuppressMessage(
    "Maintainability",
    "CA1515:TypesCanBeMadeInternal",
    Justification = "XAML 资源需要实例化，类型必须 public。",
    Scope = "type",
    Target = "~T:StarCrew.Launcher.Views.HexColorConverter"
)]
[assembly: SuppressMessage(
    "Maintainability",
    "CA1515:TypesCanBeMadeInternal",
    Justification = "XAML 资源需要实例化，类型必须 public。",
    Scope = "type",
    Target = "~T:StarCrew.Launcher.Views.ScrimOpacityPercentConverter"
)]
[assembly: SuppressMessage(
    "Design",
    "CA1031:DoNotCatchGeneralExceptionTypes",
    Justification = "外部背景图片可能损坏或权限变化，解码失败统一回退到其他背景。",
    Scope = "member",
    Target = "~M:StarCrew.Launcher.MainWindow.ApplyBackgroundImageAsync(System.Int32)"
)]
[assembly: SuppressMessage(
    "Design",
    "CA1031:DoNotCatchGeneralExceptionTypes",
    Justification = "缩略图加载失败时留空，不影响背景本身。",
    Scope = "member",
    Target = "~M:StarCrew.Launcher.Views.AppearancePage.UpdateCustomBackgroundThumbnailAsync"
)]
[assembly: SuppressMessage(
    "Design",
    "CA1031:DoNotCatchGeneralExceptionTypes",
    Justification = "可选资源缺失时退回默认样式/空图标，不影响卡片生成。",
    Scope = "member",
    Target = "~M:StarCrew.Launcher.Views.DownloadBasePage.BuildVersionCard(StarCrew.Launcher.Models.GameVersionEntry)"
)]
[assembly: SuppressMessage(
    "Design",
    "CA1031:DoNotCatchGeneralExceptionTypes",
    Justification = "可选资源缺失时退回默认样式/空图标，不影响卡片生成。",
    Scope = "member",
    Target = "~M:StarCrew.Launcher.Views.InstancesPage.BuildCard(StarCrew.Launcher.ViewModels.InstanceItemViewModel)"
)]
[assembly: SuppressMessage(
    "Design",
    "CA1031:DoNotCatchGeneralExceptionTypes",
    Justification = "启动期任何异常都必须弹窗告知并退出，此时日志尚未初始化。",
    Scope = "member",
    Target = "~M:StarCrew.Launcher.App.#ctor"
)]
[assembly: SuppressMessage(
    "Security",
    "CA5392:UseDefaultDllImportSearchPathsAttributeForPInvokes",
    Justification = "第三方包自带文件，无权修改；自有 P/Invoke 已全加 DefaultDllImportSearchPaths。",
    Scope = "member",
    Target = "~M:Microsoft.Windows.Foundation.UndockedRegFreeWinRTCS.NativeMethods.WindowsAppRuntime_EnsureIsLoaded~System.Int32"
)]
[assembly: SuppressMessage(
    "Design",
    "CA1031:DoNotCatchGeneralExceptionTypes",
    Justification = "字体枚举失败等同于没有可选的系统字体，退回默认字体，不影响启动。",
    Scope = "member",
    Target = "~M:StarCrew.Launcher.Services.SystemFontCatalog.GetFonts~System.Collections.Generic.IReadOnlyList{StarCrew.Launcher.Models.FontOption}"
)]
