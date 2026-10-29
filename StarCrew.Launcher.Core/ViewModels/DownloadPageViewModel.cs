using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StarCrew.Launcher.Models;
using StarCrew.Launcher.Services;

namespace StarCrew.Launcher.ViewModels;

internal sealed partial class DownloadPageViewModel : ObservableObject
{
    private readonly IInstanceStore _store;
    private readonly IGameVersionCatalog _catalog;
    private readonly ISteamLoginManager _loginManager;
    private readonly IDownloadCoordinator _coordinator;
    private readonly INotificationService _notifications;
    private readonly IAppStrings _strings;

    public DownloadPageViewModel(
        IInstanceStore store,
        IGameVersionCatalog catalog,
        ISteamLoginManager loginManager,
        IDownloadCoordinator coordinator,
        INotificationService notifications,
        IAppStrings strings
    )
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(loginManager);
        ArgumentNullException.ThrowIfNull(coordinator);
        ArgumentNullException.ThrowIfNull(notifications);
        ArgumentNullException.ThrowIfNull(strings);
        _store = store;
        _catalog = catalog;
        _loginManager = loginManager;
        _coordinator = coordinator;
        _notifications = notifications;
        _strings = strings;

        AccountName = _store.Load().SteamAccountName;
        Items = [];
        StatusMessage = _strings.GetString("Download_Ready");
        _ = LoadVersionsAsync();
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FilteredItems))]
    public partial IReadOnlyList<GameVersionEntry> Items { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FilteredItems))]
    public partial string? SearchText { get; set; }

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

    public string AccountLabel => AccountName ?? _strings.GetString("Download_NotLoggedIn");

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial string StatusMessage { get; set; }

    /// <summary>进度百分比。</summary>
    [ObservableProperty]
    public partial double Progress { get; set; }

    [ObservableProperty]
    public partial GameVersionEntry? SelectedVersion { get; set; }

    [ObservableProperty]
    public partial string InstanceName { get; set; } = string.Empty;

    public bool NeedsLogin => string.IsNullOrWhiteSpace(AccountName);

    public bool CanStartInstall => !NeedsLogin;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotRefreshing))]
    public partial bool IsRefreshing { get; set; }

    public bool IsNotRefreshing => !IsRefreshing;

    [RelayCommand]
    private async Task RefreshAsync() => await LoadVersionsAsync();

    private async Task LoadVersionsAsync()
    {
        IsRefreshing = true;
        try
        {
            VersionCatalog catalog = await _catalog.LoadAsync();
            Items = catalog.Versions;
            StatusMessage = _strings.Format("Download_VersionsCountFormat", catalog.Versions.Count);
        }
        catch (Exception ex)
        {
            Items = [];
            StatusMessage = _strings.Format("Download_VersionListFailedFormat", ex.Message);
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    public async Task<SteamLoginOutcome> LoginAsync(
        string username,
        string password,
        string? steamGuardCode
    )
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password))
        {
            return new SteamLoginOutcome(
                false,
                null,
                _strings.GetString("Download_CredentialsRequired")
            );
        }

        IsBusy = true;
        StatusMessage = _strings.GetString("Download_LoggingIn");
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
                    _strings.Format("Download_LoggedInFormat", outcome.AccountName),
                    null,
                    NotificationSeverity.Success
                );
                StatusMessage = _strings.Format("Download_LoggedInFormat", outcome.AccountName);
            }
            else
            {
                _notifications.Notify(
                    _strings.Format(
                        "Download_LoginFailedFormat",
                        outcome.ErrorMessage ?? _strings.GetString("Download_UnknownError")
                    ),
                    null,
                    NotificationSeverity.Error
                );
                StatusMessage = outcome.ErrorMessage ?? _strings.GetString("Download_LoginFailed");
            }

            return outcome;
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task<SteamLoginOutcome> LoginWithQrAsync(
        IProgress<string>? outputProgress = null,
        CancellationToken cancellationToken = default
    )
    {
        IsBusy = true;
        StatusMessage = _strings.GetString("Download_WaitingQrLogin");
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
                    _strings.Format("Download_LoggedInFormat", outcome.AccountName),
                    null,
                    NotificationSeverity.Success
                );
                StatusMessage = _strings.Format("Download_LoggedInFormat", outcome.AccountName);
            }
            else
            {
                _notifications.Notify(
                    _strings.Format(
                        "Download_LoginFailedFormat",
                        outcome.ErrorMessage ?? _strings.GetString("Download_UnknownError")
                    ),
                    null,
                    NotificationSeverity.Error
                );
                StatusMessage = outcome.ErrorMessage ?? _strings.GetString("Download_LoginFailed");
            }

            return outcome;
        }
        catch (OperationCanceledException)
        {
            StatusMessage = _strings.GetString("Download_LoginCancelled");
            return new SteamLoginOutcome(false, null, _strings.GetString("Download_Cancelled"));
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void Logout()
    {
        LauncherState state = _store.Load();
        _store.Save(state with { SteamAccountName = null });
        AccountName = null;
        StatusMessage = _strings.GetString("Download_LoggedOutStatus");
        _notifications.Notify(
            _strings.GetString("Download_LoggedOutToast"),
            null,
            NotificationSeverity.Informational
        );
    }

    public async Task<GameInstallResult> InstallAsync(
        GameVersionEntry version,
        string instanceName,
        IProgress<DownloadProgressInfo>? externalProgress = null,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(AccountName))
        {
            return new GameInstallResult(
                false,
                _strings.GetString("Download_LoginRequiredForInstall")
            );
        }

        string normalizedName = instanceName?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(normalizedName))
        {
            return new GameInstallResult(false, _strings.GetString("Download_InstanceNameEmpty"));
        }

        IsBusy = true;
        Progress = 0;
        StatusMessage = _strings.Format("Download_InstallStartFormat", normalizedName);

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
