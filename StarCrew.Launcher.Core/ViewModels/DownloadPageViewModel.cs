using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StarCrew.Launcher.Models;
using StarCrew.Launcher.Services;

namespace StarCrew.Launcher.ViewModels;

/// <summary>下载页：版本列表、账号登录、安装流程的协调。</summary>
internal sealed partial class DownloadPageViewModel : ObservableObject
{
    private readonly IInstanceStore _store;
    private readonly IGameVersionCatalog _catalog;
    private readonly ISteamLoginManager _loginManager;
    private readonly IDownloadCoordinator _coordinator;
    private readonly IDepotDownloaderProvisioner _provisioner;
    private readonly INotificationService _notifications;

    public DownloadPageViewModel(
        IInstanceStore store,
        IGameVersionCatalog catalog,
        ISteamLoginManager loginManager,
        IDownloadCoordinator coordinator,
        IDepotDownloaderProvisioner provisioner,
        INotificationService notifications
    )
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(loginManager);
        ArgumentNullException.ThrowIfNull(coordinator);
        ArgumentNullException.ThrowIfNull(provisioner);
        ArgumentNullException.ThrowIfNull(notifications);
        _store = store;
        _catalog = catalog;
        _loginManager = loginManager;
        _coordinator = coordinator;
        _provisioner = provisioner;
        _notifications = notifications;

        AccountName = _store.Load().SteamAccountName;
        Items = [];
        IsDepotDownloaderInstalled = _provisioner.IsInstalled();
        _ = LoadVersionsAsync();
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FilteredItems))]
    public partial IReadOnlyList<GameVersionEntry> Items { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FilteredItems))]
    public partial string? SearchText { get; set; }

    /// <summary>按版本名称过滤后的条目；搜索词空白时等于全部。</summary>
    public IEnumerable<GameVersionEntry> FilteredItems =>
        string.IsNullOrWhiteSpace(SearchText)
            ? Items
            : Items.Where(v =>
                v.VersionLabel.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
            );

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AccountLabel))]
    [NotifyPropertyChangedFor(nameof(NeedsLogin))]
    [NotifyPropertyChangedFor(nameof(CanStartInstall))]
    public partial string? AccountName { get; set; }

    /// <summary>账号状态文本：未登录/已登录 @ 用户名。</summary>
    public string AccountLabel => AccountName ?? "未登录";

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = "准备就绪。";

    [ObservableProperty]
    public partial double Progress { get; set; }

    /// <summary>DepotDownloader 组件是否已安装；控制账号页组件卡的状态与安装按钮。</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DepotDownloaderInstallButtonText))]
    [NotifyPropertyChangedFor(nameof(CanInstallDepotDownloader))]
    public partial bool IsDepotDownloaderInstalled { get; set; }

    /// <summary>安装按钮文案：未安装时「安装」，已安装时「已安装」（禁用）。</summary>
    public string DepotDownloaderInstallButtonText =>
        IsDepotDownloaderInstalled ? "已安装" : "安装";

    /// <summary>未安装时才允许点安装。</summary>
    public bool CanInstallDepotDownloader => !IsDepotDownloaderInstalled;

    /// <summary>版本列表里选中的待安装版本；进入安装详情页前由界面层设置。</summary>
    [ObservableProperty]
    public partial GameVersionEntry? SelectedVersion { get; set; }

    /// <summary>安装详情页的实例名称；初始值随选中版本生成，用户可改。</summary>
    [ObservableProperty]
    public partial string InstanceName { get; set; } = string.Empty;

    /// <summary>是否尚未登录：安装详情页据此提示并禁用安装主按钮。</summary>
    public bool NeedsLogin => string.IsNullOrWhiteSpace(AccountName);

    /// <summary>未登录时不允许开始安装：下载必须有账号凭据。</summary>
    public bool CanStartInstall => !NeedsLogin;

    [RelayCommand]
    private async Task RefreshAsync() => await LoadVersionsAsync();

    private async Task LoadVersionsAsync()
    {
        try
        {
            VersionCatalog catalog = await _catalog.LoadAsync();
            Items = catalog.Versions;
            StatusMessage = $"共 {catalog.Versions.Count} 个可安装版本。";
        }
        catch (Exception ex)
        {
            Items = [];
            StatusMessage = $"版本列表加载失败：{ex.Message}";
        }
    }

    /// <summary>执行登录；成功返回后 AccountName 已更新。</summary>
    public async Task<SteamLoginOutcome> LoginAsync(
        string username,
        string password,
        string? steamGuardCode
    )
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password))
        {
            return new SteamLoginOutcome(false, null, "请输入用户名和密码。");
        }

        IsBusy = true;
        StatusMessage = "正在登录 Steam 账号...";
        try
        {
            SteamLoginOutcome outcome = await _loginManager.LoginAsync(
                username,
                password,
                steamGuardCode
            );
            if (outcome.IsSuccess)
            {
                AccountName = outcome.AccountName;
                _notifications.Notify(
                    $"已登录：{outcome.AccountName}",
                    null,
                    NotificationSeverity.Success
                );
                StatusMessage = $"已登录：{outcome.AccountName}";
            }
            else
            {
                _notifications.Notify(
                    $"登录失败：{outcome.ErrorMessage ?? "未知错误"}",
                    null,
                    NotificationSeverity.Error
                );
                StatusMessage = outcome.ErrorMessage ?? "登录失败";
            }

            return outcome;
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>扫码登录：DD 输出透传到 outputProgress 给页面展示 QR 图。</summary>
    public async Task<SteamLoginOutcome> LoginWithQrAsync(
        IProgress<string>? outputProgress = null,
        CancellationToken cancellationToken = default
    )
    {
        IsBusy = true;
        StatusMessage = "等待扫码登录...";
        try
        {
            SteamLoginOutcome outcome = await _loginManager.LoginWithQrAsync(
                outputProgress,
                cancellationToken
            );
            if (outcome.IsSuccess)
            {
                AccountName = outcome.AccountName;
                _notifications.Notify(
                    $"已登录：{outcome.AccountName}",
                    null,
                    NotificationSeverity.Success
                );
                StatusMessage = $"已登录：{outcome.AccountName}";
            }
            else
            {
                _notifications.Notify(
                    $"登录失败：{outcome.ErrorMessage ?? "未知错误"}",
                    null,
                    NotificationSeverity.Error
                );
                StatusMessage = outcome.ErrorMessage ?? "登录失败";
            }

            return outcome;
        }
        catch (OperationCanceledException)
        {
            // 取消是用户主动行为，登录对话框自行关闭，不再弹通知。
            StatusMessage = "登录已取消。";
            return new SteamLoginOutcome(false, null, "已取消。");
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>清除本地记录的账号名（DD 的本地会话令牌保留）。</summary>
    public void Logout()
    {
        LauncherState state = _store.Load();
        _store.Save(state with { SteamAccountName = null });
        AccountName = null;
        StatusMessage = "已退出登录。下次登录时会复用 DD 的本地会话令牌。";
        _notifications.Notify("已退出登录，账号已清除。", null, NotificationSeverity.Informational);
    }

    /// <summary>显式安装 DepotDownloader 组件；结束（成功/失败）立即弹 Toast，取消静默返回。下载进度经 progress 上报给安装对话框。</summary>
    public async Task<DepotDownloaderEnsureResult> InstallDepotDownloaderAsync(
        IProgress<DownloadProgressInfo>? progress = null,
        CancellationToken cancellationToken = default
    )
    {
        IsBusy = true;
        StatusMessage = "正在下载 DepotDownloader 组件...";
        try
        {
            DepotDownloaderEnsureResult result = await _provisioner.EnsureAsync(
                progress,
                cancellationToken
            );
            // 以磁盘为准刷新状态，避免结果与实际情况不一致。
            IsDepotDownloaderInstalled = _provisioner.IsInstalled();
            StatusMessage = result.Message;
            _notifications.Notify(
                result.Message,
                null,
                result.IsSuccess ? NotificationSeverity.Success : NotificationSeverity.Error
            );
            return result;
        }
        catch (OperationCanceledException)
        {
            // 取消是用户主动行为，安装对话框自行关闭，不再弹通知。
            StatusMessage = "已取消组件安装。";
            return new DepotDownloaderEnsureResult(false, "已取消。", null);
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>启动安装；进度经 externalProgress 上报给安装详情页的进度对话框。</summary>
    public async Task<GameInstallResult> InstallAsync(
        GameVersionEntry version,
        string instanceName,
        IProgress<DownloadProgressInfo>? externalProgress = null,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(AccountName))
        {
            return new GameInstallResult(false, "请先在「游戏下载」页登录 Steam 账号。");
        }

        string normalizedName = instanceName?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(normalizedName))
        {
            return new GameInstallResult(false, "实例名称不能为空。");
        }

        IsBusy = true;
        Progress = 0;
        StatusMessage = $"开始安装「{normalizedName}」...";

        try
        {
            IProgress<DownloadProgressInfo> relay = new Progress<DownloadProgressInfo>(info =>
            {
                Progress = info.Percent;
                StatusMessage = info.Message;
                externalProgress?.Report(info);
            });

            GameInstallResult result = await _coordinator.InstallAsync(
                normalizedName,
                version,
                relay,
                cancellationToken
            );

            if (result.IsSuccess)
            {
                Progress = 100;
                StatusMessage = result.Message;
                _notifications.Notify(result.Message, null, NotificationSeverity.Success);
            }
            else
            {
                StatusMessage = result.Message;
                _notifications.Notify(result.Message, null, NotificationSeverity.Error);
            }

            return result;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
