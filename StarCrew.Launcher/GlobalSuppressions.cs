// 分析器压制集中登记处：每条压制必须写清理由，禁止无理由压制。
// 新增压制前先确认：是规则不适用（留压制），还是代码真有问题（改代码）。
using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(
    "Design",
    "CA1031:DoNotCatchGeneralExceptionTypes",
    Justification = "启动器走 Try-* 尽力而为语义：定位/拉起失败必须回退到手动选择，任何异常都不能上浮崩溃。",
    Scope = "member",
    Target = "~M:StarCrew.Launcher.Services.GameLauncher.TryLaunchExe(System.String,System.String@)~System.Boolean"
)]
[assembly: SuppressMessage(
    "Design",
    "CA1031:DoNotCatchGeneralExceptionTypes",
    Justification = "同上：steam:// 协议拉起在未安装 Steam 的机器上抛什么都有可能，必须吞掉并回退。",
    Scope = "member",
    Target = "~M:StarCrew.Launcher.Services.GameLauncher.TryLaunchBySteamProtocol(System.String@)~System.Boolean"
)]
[assembly: SuppressMessage(
    "Design",
    "CA1031:DoNotCatchGeneralExceptionTypes",
    Justification = "同上：注册表读取失败直接视为未安装，不许影响主流程。",
    Scope = "member",
    Target = "~M:StarCrew.Launcher.Services.SteamGameLocator.ReadRegistrySteamPath(Microsoft.Win32.RegistryHive,System.String)~System.String"
)]
[assembly: SuppressMessage(
    "Design",
    "CA1031:DoNotCatchGeneralExceptionTypes",
    Justification = "同上：VDF 解析失败视为无额外库，直接回退。",
    Scope = "member",
    Target = "~M:StarCrew.Launcher.Services.SteamGameLocator.ParseLibraryPaths(System.String)~System.Collections.Generic.IEnumerable{System.String}"
)]
[assembly: SuppressMessage(
    "Performance",
    "CA1822:MarkMembersAsStatic",
    Justification = "刻意保留实例成员：后续拆分 IProcessStarter / ISteamLocator 依赖注入接缝时需要实例形态，现在改 static 以后还得改回去。",
    Scope = "member",
    Target = "~M:StarCrew.Launcher.Services.GameLauncher.TryLaunchExe(System.String,System.String@)~System.Boolean"
)]
[assembly: SuppressMessage(
    "Performance",
    "CA1822:MarkMembersAsStatic",
    Justification = "同上：保留实例形态以支持依赖注入接缝。",
    Scope = "member",
    Target = "~M:StarCrew.Launcher.Services.GameLauncher.TryLaunchBySteamProtocol(System.String@)~System.Boolean"
)]
[assembly: SuppressMessage(
    "Performance",
    "CA1822:MarkMembersAsStatic",
    Justification = "同上：保留实例形态以支持依赖注入接缝。",
    Scope = "member",
    Target = "~M:StarCrew.Launcher.Services.SteamGameLocator.FindGameExe~System.String"
)]
[assembly: SuppressMessage(
    "Maintainability",
    "CA1515:TypesCanBeMadeInternal",
    Justification = "XAML 代码隐藏类按 WinUI 约定保持 public（设计器与生成代码期望），业务类型已收敛为 internal。",
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
    "Security",
    "CA5392:UseDefaultDllImportSearchPathsAttributeForPInvokes",
    Justification = "Microsoft.WindowsAppSDK 包内自带文件，非本仓库代码，无权修改；本仓库自有 P/Invoke 已全部加 DefaultDllImportSearchPaths。",
    Scope = "member",
    Target = "~M:Microsoft.Windows.Foundation.UndockedRegFreeWinRTCS.NativeMethods.WindowsAppRuntime_EnsureIsLoaded~System.Int32"
)]
[assembly: SuppressMessage(
    "Usage",
    "IDE0060:RemoveUnusedParameter",
    Justification = "XAML 事件处理器签名由 MainWindow.xaml 的 Click 绑定决定，sender/e 必须保留但确实用不到。",
    Scope = "member",
    Target = "~M:StarCrew.Launcher.MainWindow.LaunchButton_Click(System.Object,Microsoft.UI.Xaml.RoutedEventArgs)~System.Void"
)]
