using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StarCrew.Launcher.Models;
using StarCrew.Launcher.Services;

namespace StarCrew.Launcher.ViewModels;

/// <summary>关于页视图模型：更新检查与下载的状态提示收敛在这里，对话框由页面掌握。</summary>
internal sealed partial class AboutViewModel : ObservableObject
{
    private readonly AppUpdater _updater;
    private readonly IExternalLauncher _launcher;
    private readonly INotificationService _notifications;
    private readonly IAppInfo _appInfo;
    private readonly IAppStrings _strings;

    public AboutViewModel(
        AppUpdater updater,
        IExternalLauncher launcher,
        INotificationService notifications,
        IAppInfo appInfo,
        IAppStrings strings
    )
    {
        ArgumentNullException.ThrowIfNull(updater);
        ArgumentNullException.ThrowIfNull(launcher);
        ArgumentNullException.ThrowIfNull(notifications);
        ArgumentNullException.ThrowIfNull(appInfo);
        ArgumentNullException.ThrowIfNull(strings);
        _updater = updater;
        _launcher = launcher;
        _notifications = notifications;
        _appInfo = appInfo;
        _strings = strings;
    }

    /// <summary>版本描述，取自启动器自身程序集版本。</summary>
    public string VersionDescription =>
        _appInfo.Version is null
            ? _strings.GetString("About_VersionUnknown")
            : _strings.Format("About_VersionFormat", _appInfo.Version);

    /// <summary>检查到新版本时触发，由页面弹确认框。</summary>
    public event EventHandler<string>? UpdateAvailable;

    /// <summary>用系统默认浏览器打开链接。</summary>
    public Task OpenUriAsync(string uri) => _launcher.OpenUriAsync(uri);

    [RelayCommand]
    private async Task CheckUpdateAsync()
    {
        UpdateCheckResult check = await _updater.CheckOnlyAsync();
        if (check.State != UpdateState.Available || check.AvailableVersion is null)
        {
            NotificationSeverity severity = check.State switch
            {
                UpdateState.UpToDate => NotificationSeverity.Success,
                UpdateState.NotInstalled => NotificationSeverity.Informational,
                _ => NotificationSeverity.Error,
            };
            _notifications.Notify(check.Message, null, severity);
            return;
        }

        UpdateAvailable?.Invoke(this, check.AvailableVersion);
    }

    /// <summary>下载指定版本，成功后返回 true（由页面退出以触发重启）。</summary>
    public async Task<bool> DownloadUpdateAsync(
        string version,
        IProgress<int>? progress,
        CancellationToken cancellationToken
    )
    {
        try
        {
            UpdateCheckResult downloaded = await _updater.DownloadAndPrepareAsync(
                version,
                progress,
                cancellationToken
            );
            if (downloaded.State == UpdateState.ReadyToRestart)
            {
                return true;
            }

            _notifications.Notify(downloaded.Message, null, NotificationSeverity.Error);
            return false;
        }
        catch (OperationCanceledException)
        {
            _notifications.Notify(
                _strings.GetString("About_UpdateDownloadCancelled"),
                null,
                NotificationSeverity.Informational
            );
            return false;
        }
    }
}
