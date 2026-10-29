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
    "Security",
    "CA5392:UseDefaultDllImportSearchPathsAttributeForPInvokes",
    Justification = "第三方包自带文件，无权修改；自有 P/Invoke 已全加 DefaultDllImportSearchPaths。",
    Scope = "member",
    Target = "~M:Microsoft.Windows.Foundation.UndockedRegFreeWinRTCS.NativeMethods.WindowsAppRuntime_EnsureIsLoaded~System.Int32"
)]
