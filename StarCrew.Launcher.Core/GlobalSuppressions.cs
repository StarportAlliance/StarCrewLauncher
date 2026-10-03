// 分析器压制集中登记处：每条压制必须写清理由，禁止无理由压制。
// 新增压制前先确认：是规则不适用（留压制），还是代码真有问题（改代码）。
using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(
    "Design",
    "CA1031:DoNotCatchGeneralExceptionTypes",
    Justification = "Try-* 尽力语义：拉起异常转 error 回退，不崩。",
    Scope = "member",
    Target = "~M:StarCrew.Launcher.Services.ProcessStarter.TryStart(System.String,System.String,System.String@)~System.Boolean"
)]
[assembly: SuppressMessage(
    "Design",
    "CA1031:DoNotCatchGeneralExceptionTypes",
    Justification = "注册表失败视为未安装，不影响主流程。",
    Scope = "member",
    Target = "~M:StarCrew.Launcher.Services.WindowsSteamEnvironment.ReadRegistrySteamPath(Microsoft.Win32.RegistryHive,System.String)~System.String"
)]
[assembly: SuppressMessage(
    "Design",
    "CA1031:DoNotCatchGeneralExceptionTypes",
    Justification = "VDF 读取失败视为无额外库，调用方按 null 处理。",
    Scope = "member",
    Target = "~M:StarCrew.Launcher.Services.WindowsSteamEnvironment.ReadAllTextOrNull(System.String)~System.String"
)]
[assembly: SuppressMessage(
    "Design",
    "CA1031:DoNotCatchGeneralExceptionTypes",
    Justification = "接缝异常同样视为无额外库，只回退不崩。",
    Scope = "member",
    Target = "~M:StarCrew.Launcher.Services.SteamGameLocator.TryReadAllText(System.String)~System.String"
)]
[assembly: SuppressMessage(
    "Performance",
    "CA1822:MarkMembersAsStatic",
    Justification = "保留实例形态：承载注入依赖，静态化破坏接缝。",
    Scope = "member",
    Target = "~M:StarCrew.Launcher.Services.GameLauncher.TryLaunchExe(System.String,System.String@)~System.Boolean"
)]
[assembly: SuppressMessage(
    "Performance",
    "CA1822:MarkMembersAsStatic",
    Justification = "保留实例形态：用注入的 IProcessStarter。",
    Scope = "member",
    Target = "~M:StarCrew.Launcher.Services.GameLauncher.TryLaunchBySteamProtocol(System.String@)~System.Boolean"
)]
[assembly: SuppressMessage(
    "Performance",
    "CA1822:MarkMembersAsStatic",
    Justification = "保留实例形态：用注入的 ISteamEnvironment。",
    Scope = "member",
    Target = "~M:StarCrew.Launcher.Services.SteamGameLocator.FindGameExe~System.String"
)]
[assembly: SuppressMessage(
    "Design",
    "CA1031:DoNotCatchGeneralExceptionTypes",
    Justification = "更新走尽力语义：异常转 Failed 回显，不崩 UI。",
    Scope = "member",
    Target = "~M:StarCrew.Launcher.Services.AppUpdater.CheckAndPrepareUpdateAsync(System.IProgress{System.Int32},System.Threading.CancellationToken)"
)]
