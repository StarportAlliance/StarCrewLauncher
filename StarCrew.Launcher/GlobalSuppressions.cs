// 分析器压制集中登记处：每条压制必须写清理由，禁止无理由压制。
// 新增压制前先确认：是规则不适用（留压制），还是代码真有问题（改代码）。
// 业务类型的压制（CA1031/CA1822）已随类型搬入 StarCrew.Launcher.Core/GlobalSuppressions.cs，
// 这里只保留 XAML 外壳与第三方包相关的压制。
using System.Diagnostics.CodeAnalysis;

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
