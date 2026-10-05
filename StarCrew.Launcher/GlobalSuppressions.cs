// 分析器压制集中登记处：每条压制必须写清理由，禁止无理由压制。
// 新增压制前先确认：是规则不适用（留压制），还是代码真有问题（改代码）。
// 业务类型的压制已随类型搬入 StarCrew.Launcher.Core/GlobalSuppressions.cs，
// 这里只保留 XAML 外壳与第三方包相关的压制。
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
    "Security",
    "CA5392:UseDefaultDllImportSearchPathsAttributeForPInvokes",
    Justification = "第三方包自带文件，无权修改；自有 P/Invoke 已全加 DefaultDllImportSearchPaths。",
    Scope = "member",
    Target = "~M:Microsoft.Windows.Foundation.UndockedRegFreeWinRTCS.NativeMethods.WindowsAppRuntime_EnsureIsLoaded~System.Int32"
)]
