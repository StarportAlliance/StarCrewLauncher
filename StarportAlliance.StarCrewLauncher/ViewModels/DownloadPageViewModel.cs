using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StarportAlliance.StarCrewLauncher.Models;
using StarportAlliance.StarCrewLauncher.Services;

namespace StarportAlliance.StarCrewLauncher.ViewModels;

internal sealed partial class DownloadPageViewModel : ObservableObject
{
    private readonly IInstanceStore _store;
    private readonly IGameVersionCatalog _catalog;
    private readonly ISteamLoginManager _loginManager;
    private readonly IDownloadCoordinator _coordinator;
    private readonly ISddClient _sdd;
    private readonly INotificationService _notifications;
    private readonly IAppStrings _strings;

    public DownloadPageViewModel(
        IInstanceStore store,
        IGameVersionCatalog catalog,
        ISteamLoginManager loginManager,
        IDownloadCoordinator coordinator,
        ISddClient sdd,
        INotificationService notifications,
        IAppStrings strings
    )
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(loginManager);
        ArgumentNullException.ThrowIfNull(coordinator);
        ArgumentNullException.ThrowIfNull(sdd);
        ArgumentNullException.ThrowIfNull(notifications);
        ArgumentNullException.ThrowIfNull(strings);
        _store = store;
        _catalog = catalog;
        _loginManager = loginManager;
        _coordinator = coordinator;
        _sdd = sdd;
        _notifications = notifications;
        _strings = strings;

        AccountName = _store.Load().SteamAccountName;
        Items = [];
        _ = LoadVersionsAsync(forceRefresh: false);
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FilteredItems))]
    public partial IReadOnlyList<GameVersionEntry> Items { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FilteredItems))]
    public partial string? SearchText { get; set; }

    public IReadOnlyList<VersionItemViewModel> FilteredItems =>
        [
            .. Items
                .Where(v =>
                    string.IsNullOrWhiteSpace(SearchText)
                    || v.VersionLabel.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
                )
                .Select(v => new VersionItemViewModel(
                    v,
                    _strings.Format(
                        "DownloadBase_PublishedAtFormat",
                        v.PublishedAt.ToLocalTime()
                            .ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)
                    ),
                    _strings.GetString("Common_Install")
                )),
        ];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AccountLabel))]
    [NotifyPropertyChangedFor(nameof(NeedsLogin))]
    [NotifyPropertyChangedFor(nameof(CanStartInstall))]
    public partial string? AccountName { get; set; }

    public string AccountLabel => AccountName ?? _strings.GetString("Download_NotLoggedIn");

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasCatalogUpdatedAt))]
    public partial string? CatalogUpdatedAtText { get; set; }

    public bool HasCatalogUpdatedAt => CatalogUpdatedAtText is not null;

    [ObservableProperty]
    public partial GameVersionEntry? SelectedVersion { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanStartInstall))]
    public partial string InstanceName { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasInstanceNameError))]
    [NotifyPropertyChangedFor(nameof(CanStartInstall))]
    public partial string? InstanceNameError { get; set; }

    public bool HasInstanceNameError => InstanceNameError is not null;

    public bool NeedsLogin => string.IsNullOrWhiteSpace(AccountName);

    public bool CanStartInstall =>
        !NeedsLogin && !string.IsNullOrWhiteSpace(InstanceName) && !HasInstanceNameError;

    /// <summary>选中版本并准备安装参数。</summary>
    /// <returns>未登录时不准备参数并返回 false，调用方应引导登录。</returns>
    public bool SelectVersion(VersionItemViewModel item)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (NeedsLogin)
        {
            return false;
        }

        SelectedVersion = item.Entry;
        InstanceName = _strings.Format("Install_DefaultInstanceNameFormat", item.VersionLabel);
        return true;
    }

    partial void OnInstanceNameChanged(string value) =>
        InstanceNameError = ValidateInstanceName(value);

    private string? ValidateInstanceName(string value)
    {
        string candidate = value.Trim();
        if (candidate.Length == 0)
        {
            return _strings.GetString("Download_InstanceNameEmpty");
        }

        bool duplicate = _store
            .Load()
            .Instances.Any(i =>
                string.Equals(i.Name, candidate, StringComparison.OrdinalIgnoreCase)
            );
        return duplicate ? _strings.GetString("Install_InstanceNameDuplicate") : null;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotRefreshing))]
    public partial bool IsRefreshing { get; set; }

    public bool IsNotRefreshing => !IsRefreshing;

    [RelayCommand]
    private async Task RefreshAsync() => await LoadVersionsAsync(forceRefresh: true);

    private async Task LoadVersionsAsync(bool forceRefresh)
    {
        IsRefreshing = true;
        try
        {
            VersionCatalogLoadResult result = await _catalog.LoadAsync(forceRefresh: forceRefresh);
            Items = result.Catalog.Versions;
            CatalogUpdatedAtText = _strings.Format(
                "DownloadBase_UpdatedAtFormat",
                result
                    .WrittenAt.ToLocalTime()
                    .ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)
            );
        }
        catch (Exception)
        {
            Items = [];
            CatalogUpdatedAtText = null;
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    public async Task<SteamLoginOutcome> LoginWithQrAsync(
        IProgress<SddQrCode>? qr = null,
        CancellationToken cancellationToken = default
    )
    {
        IsBusy = true;
        try
        {
            SteamLoginOutcome outcome = await _loginManager.LoginWithQrAsync(qr, cancellationToken);
            if (outcome.IsSuccess)
            {
                AccountName = outcome.AccountName;
                _notifications.Notify(
                    _strings.Format("Download_LoggedInFormat", outcome.AccountName),
                    null,
                    NotificationSeverity.Success
                );
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
            }

            return outcome;
        }
        catch (OperationCanceledException)
        {
            return new SteamLoginOutcome(false, null, _strings.GetString("Download_Cancelled"));
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task<bool> HasValidSessionAsync(CancellationToken cancellationToken = default)
    {
        SddAuthStatus status = await _sdd.GetAuthStatusAsync(AccountName, cancellationToken);
        if (!status.Checked)
        {
            return true;
        }

        if (status is { HasCredentials: true, AccountName: not null })
        {
            AccountName = status.AccountName;
            return true;
        }

        AccountName = null;
        _store.Save(_store.Load() with { SteamAccountName = null });
        return false;
    }

    public async Task LogoutAsync()
    {
        await _sdd.LogoutAsync();
        LauncherState state = _store.Load();
        _store.Save(state with { SteamAccountName = null });
        AccountName = null;
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

        string? nameError = ValidateInstanceName(normalizedName);
        if (nameError is not null)
        {
            return new GameInstallResult(false, nameError);
        }

        IsBusy = true;

        try
        {
            GameInstallResult result = await _coordinator.InstallAsync(
                normalizedName,
                version,
                externalProgress,
                cancellationToken
            );

            if (result.IsSuccess)
            {
                _notifications.Notify(result.Message, null, NotificationSeverity.Success);
            }
            else
            {
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
