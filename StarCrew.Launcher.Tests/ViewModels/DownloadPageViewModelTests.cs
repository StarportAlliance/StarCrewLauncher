using NSubstitute;
using StarCrew.Launcher.Models;
using StarCrew.Launcher.Services;
using StarCrew.Launcher.ViewModels;

namespace StarCrew.Launcher.Tests.ViewModels;

public sealed class DownloadPageViewModelTests
{
    [Fact]
    public void Ctor_WithNullStore_ThrowsArgumentNullException() =>
        Assert.Throws<ArgumentNullException>(() =>
            new DownloadPageViewModel(
                null!,
                Substitute.For<IGameVersionCatalog>(),
                Substitute.For<ISteamLoginManager>(),
                Substitute.For<IDownloadCoordinator>(),
                Substitute.For<INotificationService>(),
                TestStrings.Instance
            )
        );

    [Fact]
    public void Ctor_WithNullCatalog_ThrowsArgumentNullException() =>
        Assert.Throws<ArgumentNullException>(() =>
            new DownloadPageViewModel(
                Substitute.For<IInstanceStore>(),
                null!,
                Substitute.For<ISteamLoginManager>(),
                Substitute.For<IDownloadCoordinator>(),
                Substitute.For<INotificationService>(),
                TestStrings.Instance
            )
        );

    [Fact]
    public void Ctor_WithNullLoginManager_ThrowsArgumentNullException() =>
        Assert.Throws<ArgumentNullException>(() =>
            new DownloadPageViewModel(
                Substitute.For<IInstanceStore>(),
                Substitute.For<IGameVersionCatalog>(),
                null!,
                Substitute.For<IDownloadCoordinator>(),
                Substitute.For<INotificationService>(),
                TestStrings.Instance
            )
        );

    [Fact]
    public void Ctor_WithNullCoordinator_ThrowsArgumentNullException() =>
        Assert.Throws<ArgumentNullException>(() =>
            new DownloadPageViewModel(
                Substitute.For<IInstanceStore>(),
                Substitute.For<IGameVersionCatalog>(),
                Substitute.For<ISteamLoginManager>(),
                null!,
                Substitute.For<INotificationService>(),
                TestStrings.Instance
            )
        );

    [Fact]
    public void Ctor_WithNullNotifications_ThrowsArgumentNullException() =>
        Assert.Throws<ArgumentNullException>(() =>
            new DownloadPageViewModel(
                Substitute.For<IInstanceStore>(),
                Substitute.For<IGameVersionCatalog>(),
                Substitute.For<ISteamLoginManager>(),
                Substitute.For<IDownloadCoordinator>(),
                null!,
                TestStrings.Instance
            )
        );

    [Fact]
    public void Ctor_ReadsAccountNameFromStore()
    {
        (DownloadPageViewModel vm, _, _, _, _, _) = Create(
            state: new LauncherState(1, null, "gamer", [])
        );

        Assert.Equal("gamer", vm.AccountName);
        Assert.Equal("gamer", vm.AccountLabel);
    }

    [Fact]
    public void AccountLabel_WithoutAccount_ShowsNotLoggedIn()
    {
        (DownloadPageViewModel vm, _, _, _, _, _) = Create();

        Assert.Null(vm.AccountName);
        Assert.Equal("未登录", vm.AccountLabel);
    }

    [Fact]
    public void NeedsLogin_WithoutAccount_BlocksInstall()
    {
        (DownloadPageViewModel vm, _, _, _, _, _) = Create();

        Assert.True(vm.NeedsLogin);
        Assert.False(vm.CanStartInstall);
    }

    [Fact]
    public void NeedsLogin_WithAccount_AllowsInstall()
    {
        (DownloadPageViewModel vm, _, _, _, _, _) = Create(
            state: new LauncherState(1, null, "gamer", [])
        );

        Assert.False(vm.NeedsLogin);
        Assert.True(vm.CanStartInstall);
    }

    [Fact]
    public async Task RefreshCommand_LoadsVersionsAndStatus()
    {
        GameVersionEntry entry = VersionEntry();
        (DownloadPageViewModel vm, _, _, _, _, _) = Create(catalog: new VersionCatalog(1, [entry]));

        await vm.RefreshCommand.ExecuteAsync(null);

        Assert.Equal([entry], vm.Items);
        Assert.Contains("1", vm.StatusMessage, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RefreshCommand_WhenCatalogThrows_ShowsFailureStatus()
    {
        IGameVersionCatalog catalog = Substitute.For<IGameVersionCatalog>();
        catalog
            .LoadAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException<VersionCatalog>(new InvalidOperationException("net down")));
        (DownloadPageViewModel vm, _, _, _, _, _) = Create(catalogMock: catalog);

        await vm.RefreshCommand.ExecuteAsync(null);

        Assert.Empty(vm.Items);
        Assert.Contains("加载失败", vm.StatusMessage, StringComparison.Ordinal);
    }

    [Fact]
    public async Task FilteredItems_WithBlankSearch_ReturnsAllVersions()
    {
        GameVersionEntry latest = VersionEntry();
        GameVersionEntry older = VersionEntry("2026.8.13");
        (DownloadPageViewModel vm, _, _, _, _, _) = Create(
            catalog: new VersionCatalog(1, [latest, older])
        );

        await vm.RefreshCommand.ExecuteAsync(null);
        vm.SearchText = "   ";

        Assert.Equal([latest, older], vm.FilteredItems);
    }

    [Fact]
    public async Task FilteredItems_WithQuery_MatchesVersionLabelIgnoringCase()
    {
        GameVersionEntry latest = VersionEntry("2026.9.5");
        GameVersionEntry beta = VersionEntry("2026.10.0-Beta");
        (DownloadPageViewModel vm, _, _, _, _, _) = Create(
            catalog: new VersionCatalog(1, [latest, beta])
        );

        await vm.RefreshCommand.ExecuteAsync(null);
        vm.SearchText = "beta";

        Assert.Equal([beta], vm.FilteredItems);
    }

    [Fact]
    public async Task FilteredItems_WithUnmatchedQuery_ReturnsEmpty()
    {
        GameVersionEntry latest = VersionEntry();
        (DownloadPageViewModel vm, _, _, _, _, _) = Create(
            catalog: new VersionCatalog(1, [latest])
        );

        await vm.RefreshCommand.ExecuteAsync(null);
        vm.SearchText = "2020";

        Assert.Empty(vm.FilteredItems);
    }

    [Theory]
    [InlineData("", "pw")]
    [InlineData("   ", "pw")]
    [InlineData("user", "")]
    public async Task LoginAsync_WithBlankCredentials_ReturnsFailure(
        string username,
        string password
    )
    {
        (DownloadPageViewModel vm, _, _, _, _, _) = Create();

        SteamLoginOutcome outcome = await vm.LoginAsync(username, password, null);

        Assert.False(outcome.IsSuccess);
        Assert.False(vm.IsBusy);
    }

    [Fact]
    public async Task LoginAsync_WhenSucceeded_UpdatesAccountAndNotifies()
    {
        ISteamLoginManager login = Substitute.For<ISteamLoginManager>();
        login
            .LoginAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(new SteamLoginOutcome(true, "gamer", null));
        (DownloadPageViewModel vm, _, _, _, INotificationService notifications, _) = Create(
            loginMock: login
        );

        SteamLoginOutcome outcome = await vm.LoginAsync("gamer", "pw", null);

        Assert.True(outcome.IsSuccess);
        Assert.Equal("gamer", vm.AccountName);
        Assert.False(vm.IsBusy);
        notifications.Received(1).Notify("已登录：gamer", null, NotificationSeverity.Success);
    }

    [Fact]
    public async Task LoginAsync_WhenFailed_NotifiesError()
    {
        ISteamLoginManager login = Substitute.For<ISteamLoginManager>();
        login
            .LoginAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(new SteamLoginOutcome(false, null, "bad password"));
        (DownloadPageViewModel vm, _, _, _, INotificationService notifications, _) = Create(
            loginMock: login
        );

        SteamLoginOutcome outcome = await vm.LoginAsync("gamer", "pw", null);

        Assert.False(outcome.IsSuccess);
        Assert.Null(vm.AccountName);
        notifications
            .Received(1)
            .Notify("登录失败：bad password", null, NotificationSeverity.Error);
    }

    [Fact]
    public async Task LoginWithQrAsync_WhenSucceeded_UpdatesAccountAndNotifies()
    {
        ISteamLoginManager login = Substitute.For<ISteamLoginManager>();
        login
            .LoginWithQrAsync(Arg.Any<IProgress<string>?>(), Arg.Any<CancellationToken>())
            .Returns(new SteamLoginOutcome(true, "knight", null));
        (DownloadPageViewModel vm, _, _, _, INotificationService notifications, _) = Create(
            loginMock: login
        );

        SteamLoginOutcome outcome = await vm.LoginWithQrAsync();

        Assert.True(outcome.IsSuccess);
        Assert.Equal("knight", vm.AccountName);
        notifications.Received(1).Notify("已登录：knight", null, NotificationSeverity.Success);
    }

    [Fact]
    public async Task LoginWithQrAsync_WhenCanceled_ReturnsFailureWithoutToast()
    {
        ISteamLoginManager login = Substitute.For<ISteamLoginManager>();
        login
            .LoginWithQrAsync(Arg.Any<IProgress<string>?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<SteamLoginOutcome>(new OperationCanceledException()));
        (DownloadPageViewModel vm, _, _, _, INotificationService notifications, _) = Create(
            loginMock: login
        );

        SteamLoginOutcome outcome = await vm.LoginWithQrAsync();

        Assert.False(outcome.IsSuccess);
        Assert.Equal("已取消。", outcome.ErrorMessage);
        notifications
            .DidNotReceive()
            .Notify(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<NotificationSeverity>());
    }

    [Fact]
    public void Logout_ClearsAccountInStoreAndViewModel()
    {
        FakeStore store = new(new LauncherState(1, null, "gamer", []));
        (DownloadPageViewModel vm, _, _, _, _, _) = Create(store: store);

        vm.Logout();

        Assert.Null(vm.AccountName);
        Assert.Null(store.Current.SteamAccountName);
    }

    [Fact]
    public async Task InstallAsync_WithoutAccount_ReturnsFailure()
    {
        (DownloadPageViewModel vm, _, _, _, _, _) = Create();

        GameInstallResult result = await vm.InstallAsync(VersionEntry(), "A");

        Assert.False(result.IsSuccess);
        Assert.Contains("登录", result.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task InstallAsync_WithBlankName_ReturnsFailure(string name)
    {
        (DownloadPageViewModel vm, _, _, _, _, _) = Create(
            state: new LauncherState(1, null, "gamer", [])
        );

        GameInstallResult result = await vm.InstallAsync(VersionEntry(), name);

        Assert.False(result.IsSuccess);
        Assert.Contains("不能为空", result.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task InstallAsync_WhenSucceeded_NotifiesSuccess()
    {
        GameVersionEntry version = VersionEntry();
        IDownloadCoordinator coordinator = Substitute.For<IDownloadCoordinator>();
        coordinator
            .InstallAsync(
                Arg.Any<string>(),
                Arg.Any<GameVersionEntry>(),
                Arg.Any<IProgress<DownloadProgressInfo>?>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(new GameInstallResult(true, "done"));
        (DownloadPageViewModel vm, _, _, _, INotificationService notifications, _) = Create(
            state: new LauncherState(1, null, "gamer", []),
            coordinatorMock: coordinator
        );

        GameInstallResult result = await vm.InstallAsync(version, "  A  ");

        Assert.True(result.IsSuccess);
        Assert.False(vm.IsBusy);
        notifications.Received(1).Notify("done", null, NotificationSeverity.Success);
        await coordinator
            .Received(1)
            .InstallAsync(
                "A",
                version,
                Arg.Any<IProgress<DownloadProgressInfo>?>(),
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task InstallAsync_WhenFailed_NotifiesError()
    {
        IDownloadCoordinator coordinator = Substitute.For<IDownloadCoordinator>();
        coordinator
            .InstallAsync(
                Arg.Any<string>(),
                Arg.Any<GameVersionEntry>(),
                Arg.Any<IProgress<DownloadProgressInfo>?>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(new GameInstallResult(false, "dl boom"));
        (DownloadPageViewModel vm, _, _, _, INotificationService notifications, _) = Create(
            state: new LauncherState(1, null, "gamer", []),
            coordinatorMock: coordinator
        );

        GameInstallResult result = await vm.InstallAsync(VersionEntry(), "A");

        Assert.False(result.IsSuccess);
        Assert.False(vm.IsBusy);
        notifications.Received(1).Notify("dl boom", null, NotificationSeverity.Error);
    }

    private static (
        DownloadPageViewModel Vm,
        FakeStore Store,
        IGameVersionCatalog Catalog,
        ISteamLoginManager Login,
        INotificationService Notifications,
        IDownloadCoordinator Coordinator
    ) Create(
        VersionCatalog? catalog = null,
        LauncherState? state = null,
        FakeStore? store = null,
        IGameVersionCatalog? catalogMock = null,
        ISteamLoginManager? loginMock = null,
        IDownloadCoordinator? coordinatorMock = null
    )
    {
        FakeStore target = store ?? new FakeStore(state ?? LauncherState.Empty);
        IGameVersionCatalog catalogTarget = catalogMock ?? Substitute.For<IGameVersionCatalog>();
        if (catalogMock is null)
        {
            catalogTarget
                .LoadAsync(Arg.Any<CancellationToken>())
                .Returns(catalog ?? new VersionCatalog(1, []));
        }

        ISteamLoginManager loginTarget = loginMock ?? Substitute.For<ISteamLoginManager>();
        IDownloadCoordinator coordinatorTarget =
            coordinatorMock ?? Substitute.For<IDownloadCoordinator>();
        INotificationService notifications = Substitute.For<INotificationService>();
        DownloadPageViewModel vm = new(
            target,
            catalogTarget,
            loginTarget,
            coordinatorTarget,
            notifications,
            TestStrings.Instance
        );
        return (vm, target, catalogTarget, loginTarget, notifications, coordinatorTarget);
    }

    private static GameVersionEntry VersionEntry(string label = "2026.9.5") =>
        new(label, 990035194164819049L, DateTimeOffset.UtcNow, [], false);

    private sealed class FakeStore(LauncherState state) : IInstanceStore
    {
        public LauncherState Current { get; private set; } = state;

        public LauncherState Load() => Current;

        public void Save(LauncherState state) => Current = state;
    }
}
