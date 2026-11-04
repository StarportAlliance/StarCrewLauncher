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
    Target = "~M:StarCrew.Launcher.Views.CustomBackgroundImageCardViewModel.UpdateThumbnailAsync"
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
    "Maintainability",
    "CA1515:TypesCanBeMadeInternal",
    Justification = "XAML 的 x:DataType 需要 public。",
    Scope = "type",
    Target = "~T:StarCrew.Launcher.Models.FontOption"
)]
[assembly: SuppressMessage(
    "Maintainability",
    "CA1515:TypesCanBeMadeInternal",
    Justification = "XAML 的 x:DataType 需要 public。",
    Scope = "type",
    Target = "~T:StarCrew.Launcher.ViewModels.BuiltInBackgroundOptionViewModel"
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
    Justification = "缓存删除失败时静默忽略，禁用缓存后不会再读取。",
    Scope = "member",
    Target = "~M:StarCrew.Launcher.Services.GameVersionCatalog.ClearCache"
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
    Target = "~M:StarCrew.Launcher.ViewModels.DownloadPageViewModel.LoadVersionsAsync(System.Boolean)"
)]
[assembly: SuppressMessage(
    "Design",
    "CA1031:DoNotCatchGeneralExceptionTypes",
    Justification = "外部进程执行失败时转失败结果，不崩。",
    Scope = "member",
    Target = "~M:StarCrew.Launcher.Services.ProcessRunner.RunAsync(StarCrew.Launcher.Models.ExecutableCommand,System.IProgress{System.String},System.Threading.CancellationToken)"
)]
