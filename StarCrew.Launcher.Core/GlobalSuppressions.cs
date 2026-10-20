// 分析器压制集中登记处。
// 新增压制前先确认：是规则不适用还是代码真有问题。
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
    Target = "~M:StarCrew.Launcher.Services.WindowsSteamEnvironment.ReadAllTextOrNull(System.String)~System.String"
)]
[assembly: SuppressMessage(
    "Design",
    "CA1031:DoNotCatchGeneralExceptionTypes",
    Justification = "更新走尽力语义：异常转 Failed 回显，不崩 UI。",
    Scope = "member",
    Target = "~M:StarCrew.Launcher.Services.AppUpdater.CheckOnlyAsync(System.Threading.CancellationToken)"
)]
[assembly: SuppressMessage(
    "Design",
    "CA1031:DoNotCatchGeneralExceptionTypes",
    Justification = "同上：下载异常同样转 Failed 回显。",
    Scope = "member",
    Target = "~M:StarCrew.Launcher.Services.AppUpdater.DownloadAndPrepareAsync(System.String,System.IProgress{System.Int32},System.Threading.CancellationToken)"
)]
[assembly: SuppressMessage(
    "Design",
    "CA1031:DoNotCatchGeneralExceptionTypes",
    Justification = "状态文件损坏时按空态处理，不让启动器崩。",
    Scope = "member",
    Target = "~M:StarCrew.Launcher.Services.InstanceStore.Load"
)]
[assembly: SuppressMessage(
    "Design",
    "CA1031:DoNotCatchGeneralExceptionTypes",
    Justification = "副本备份失败也无所谓。",
    Scope = "member",
    Target = "~M:StarCrew.Launcher.Services.InstanceStore.TryBackupCorruptFile"
)]
[assembly: SuppressMessage(
    "Design",
    "CA1031:DoNotCatchGeneralExceptionTypes",
    Justification = "删除实例目录失败时转通知，不崩。",
    Scope = "member",
    Target = "~M:StarCrew.Launcher.ViewModels.InstancesViewModel.ConfirmDelete(StarCrew.Launcher.Models.GameInstance,System.Boolean)"
)]
[assembly: SuppressMessage(
    "Design",
    "CA1031:DoNotCatchGeneralExceptionTypes",
    Justification = "打开文件夹失败时转通知，不崩。",
    Scope = "member",
    Target = "~M:StarCrew.Launcher.ViewModels.InstanceItemViewModel.OpenFolderAsync"
)]
[assembly: SuppressMessage(
    "Design",
    "CA1031:DoNotCatchGeneralExceptionTypes",
    Justification = "启动 exe 出错时转为失败结果，不崩。",
    Scope = "member",
    Target = "~M:StarCrew.Launcher.Services.InstanceLauncher.LaunchAsync(StarCrew.Launcher.Models.GameInstance,System.Threading.CancellationToken)"
)]
[assembly: SuppressMessage(
    "Design",
    "CA1031:DoNotCatchGeneralExceptionTypes",
    Justification = "远端目录拉取失败时回退缓存，全失败由 LoadAsync 抛异常提示。",
    Scope = "member",
    Target = "~M:StarCrew.Launcher.Services.GameVersionCatalog.TryFetchRemoteAsync(System.Threading.CancellationToken)"
)]
[assembly: SuppressMessage(
    "Design",
    "CA1031:DoNotCatchGeneralExceptionTypes",
    Justification = "缓存读取失败视为无缓存，由 LoadAsync 抛异常提示。",
    Scope = "member",
    Target = "~M:StarCrew.Launcher.Services.GameVersionCatalog.TryReadCacheAsync(System.Threading.CancellationToken)"
)]
[assembly: SuppressMessage(
    "Design",
    "CA1031:DoNotCatchGeneralExceptionTypes",
    Justification = "缓存写入失败时静默忽略，下次照常拉取。",
    Scope = "member",
    Target = "~M:StarCrew.Launcher.Services.GameVersionCatalog.TryWriteCacheAsync(System.String,System.Threading.CancellationToken)"
)]
[assembly: SuppressMessage(
    "Design",
    "CA1031:DoNotCatchGeneralExceptionTypes",
    Justification = "下载失败时转失败结果回显，不崩。",
    Scope = "member",
    Target = "~M:StarCrew.Launcher.Services.DownloadCoordinator.InstallAsync(System.String,StarCrew.Launcher.Models.GameVersionEntry,System.IProgress{StarCrew.Launcher.Models.DownloadProgressInfo},System.Threading.CancellationToken)"
)]
[assembly: SuppressMessage(
    "Design",
    "CA1031:DoNotCatchGeneralExceptionTypes",
    Justification = "临时目录清理失败不影响主流程。",
    Scope = "member",
    Target = "~M:StarCrew.Launcher.Services.SteamLoginManager.TryDeleteTempDirectory(System.String)"
)]
[assembly: SuppressMessage(
    "Design",
    "CA1031:DoNotCatchGeneralExceptionTypes",
    Justification = "版本列表加载失败时转空列表与状态提示，不崩。",
    Scope = "member",
    Target = "~M:StarCrew.Launcher.ViewModels.DownloadPageViewModel.LoadVersionsAsync~System.Threading.Tasks.Task"
)]
[assembly: SuppressMessage(
    "Design",
    "CA1031:DoNotCatchGeneralExceptionTypes",
    Justification = "外部进程执行失败时转失败结果，不崩。",
    Scope = "member",
    Target = "~M:StarCrew.Launcher.Services.ProcessRunner.RunAsync(StarCrew.Launcher.Models.ExecutableCommand,System.IProgress{System.String},System.Threading.CancellationToken)"
)]
