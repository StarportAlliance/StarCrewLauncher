// 分析器压制集中登记处：每条压制必须写清理由，禁止无理由压制。
// 新增压制前先确认：是规则不适用（留压制），还是代码真有问题（改代码）。
using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(
    "Design",
    "CA1031:DoNotCatchGeneralExceptionTypes",
    Justification = "启动器走 Try-* 尽力而为语义：进程拉起在缺依赖的机器上抛什么都有可能，必须吞掉并转为 error 回退。",
    Scope = "member",
    Target = "~M:StarCrew.Launcher.Services.ProcessStarter.TryStart(System.String,System.String,System.String@)~System.Boolean"
)]
[assembly: SuppressMessage(
    "Design",
    "CA1031:DoNotCatchGeneralExceptionTypes",
    Justification = "同上：注册表读取失败直接视为未安装，不许影响主流程。",
    Scope = "member",
    Target = "~M:StarCrew.Launcher.Services.WindowsSteamEnvironment.ReadRegistrySteamPath(Microsoft.Win32.RegistryHive,System.String)~System.String"
)]
[assembly: SuppressMessage(
    "Design",
    "CA1031:DoNotCatchGeneralExceptionTypes",
    Justification = "同上：VDF 文件读取失败视为无额外库，直接回退，调用方按 null 处理。",
    Scope = "member",
    Target = "~M:StarCrew.Launcher.Services.WindowsSteamEnvironment.ReadAllTextOrNull(System.String)~System.String"
)]
[assembly: SuppressMessage(
    "Design",
    "CA1031:DoNotCatchGeneralExceptionTypes",
    Justification = "同上：环境接缝的异常实现也必须吞掉并视为无额外库，定位失败只回退不崩溃。",
    Scope = "member",
    Target = "~M:StarCrew.Launcher.Services.SteamGameLocator.TryReadAllText(System.String)~System.String"
)]
[assembly: SuppressMessage(
    "Performance",
    "CA1822:MarkMembersAsStatic",
    Justification = "刻意保留实例成员：承载 ISteamGameLocator / IProcessStarter 依赖注入状态，静态化会破坏接缝。",
    Scope = "member",
    Target = "~M:StarCrew.Launcher.Services.GameLauncher.TryLaunchExe(System.String,System.String@)~System.Boolean"
)]
[assembly: SuppressMessage(
    "Performance",
    "CA1822:MarkMembersAsStatic",
    Justification = "同上：保留实例形态以使用注入的 IProcessStarter。",
    Scope = "member",
    Target = "~M:StarCrew.Launcher.Services.GameLauncher.TryLaunchBySteamProtocol(System.String@)~System.Boolean"
)]
[assembly: SuppressMessage(
    "Performance",
    "CA1822:MarkMembersAsStatic",
    Justification = "同上：保留实例形态以使用注入的 ISteamEnvironment。",
    Scope = "member",
    Target = "~M:StarCrew.Launcher.Services.SteamGameLocator.FindGameExe~System.String"
)]
