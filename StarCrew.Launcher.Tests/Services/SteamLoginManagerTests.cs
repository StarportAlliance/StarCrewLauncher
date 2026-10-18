using NSubstitute;
using StarCrew.Launcher.Models;
using StarCrew.Launcher.Services;

namespace StarCrew.Launcher.Tests.Services;

public sealed class SteamLoginManagerTests : IDisposable
{
    private readonly string _root;

    public SteamLoginManagerTests()
    {
        _root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    [Fact]
    public void Ctor_WithNullProvisioner_ThrowsArgumentNullException() =>
        Assert.Throws<ArgumentNullException>(() =>
            new SteamLoginManager(
                null!,
                Substitute.For<IDepotDownloaderRunner>(),
                Substitute.For<IInstanceStore>(),
                TestStrings.Instance
            )
        );

    [Fact]
    public void Ctor_WithNullRunner_ThrowsArgumentNullException() =>
        Assert.Throws<ArgumentNullException>(() =>
            new SteamLoginManager(
                Substitute.For<IDepotDownloaderProvisioner>(),
                null!,
                Substitute.For<IInstanceStore>(),
                TestStrings.Instance
            )
        );

    [Fact]
    public void Ctor_WithNullStore_ThrowsArgumentNullException() =>
        Assert.Throws<ArgumentNullException>(() =>
            new SteamLoginManager(
                Substitute.For<IDepotDownloaderProvisioner>(),
                Substitute.For<IDepotDownloaderRunner>(),
                null!,
                TestStrings.Instance
            )
        );

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task LoginAsync_WithBlankUsername_ThrowsArgumentException(string username)
    {
        SteamLoginManager manager = Create();

        await Assert.ThrowsAsync<ArgumentException>(() => manager.LoginAsync(username, "pw", null));
    }

    [Fact]
    public async Task LoginAsync_WithNullPassword_ThrowsArgumentNullException()
    {
        SteamLoginManager manager = Create();

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            manager.LoginAsync("user", null!, null)
        );
    }

    [Fact]
    public async Task LoginAsync_WhenProvisionFails_ReturnsFailure()
    {
        FakeStore store = new(LauncherState.Empty);
        IDepotDownloaderProvisioner provisioner = Substitute.For<IDepotDownloaderProvisioner>();
        provisioner.IsInstalled().Returns(true);
        provisioner
            .EnsureAsync(Arg.Any<IProgress<DownloadProgressInfo>?>(), Arg.Any<CancellationToken>())
            .Returns(new DepotDownloaderEnsureResult(false, "tool boom", null));
        SteamLoginManager manager = new(
            provisioner,
            Substitute.For<IDepotDownloaderRunner>(),
            store,
            TestStrings.Instance
        );

        SteamLoginOutcome outcome = await manager.LoginAsync("user", "pw", null);

        Assert.False(outcome.IsSuccess);
        Assert.Equal("tool boom", outcome.ErrorMessage);
        Assert.Null(store.Current.SteamAccountName);
    }

    [Fact]
    public async Task LoginAsync_WhenDepotDownloaderNotInstalled_ReturnsFailure()
    {
        SteamLoginManager manager = Create(provisionerInstalled: false);

        SteamLoginOutcome outcome = await manager.LoginAsync("user", "pw", null);

        Assert.False(outcome.IsSuccess);
        Assert.Contains("组件", outcome.ErrorMessage, StringComparison.Ordinal);
    }

    [Fact]
    public async Task LoginWithQrAsync_WhenDepotDownloaderNotInstalled_ReturnsFailure()
    {
        SteamLoginManager manager = Create(provisionerInstalled: false);

        SteamLoginOutcome outcome = await manager.LoginWithQrAsync();

        Assert.False(outcome.IsSuccess);
        Assert.Contains("组件", outcome.ErrorMessage, StringComparison.Ordinal);
    }

    [Fact]
    public async Task LoginAsync_WhenRunnerSucceeds_SavesAccountName()
    {
        FakeStore store = new(LauncherState.Empty);
        ExecutableCommand? captured = null;
        IDepotDownloaderRunner runner = Substitute.For<IDepotDownloaderRunner>();
        runner
            .RunAsync(
                Arg.Any<ExecutableCommand>(),
                Arg.Any<IProgress<string>?>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(call =>
            {
                captured = call.Arg<ExecutableCommand>();
                return new ExecutableOutcome(0, ["done"]);
            });
        SteamLoginManager manager = Create(store, runner);

        SteamLoginOutcome outcome = await manager.LoginAsync("gamer", "pw", " 123456 ");

        Assert.True(outcome.IsSuccess);
        Assert.Equal("gamer", outcome.AccountName);
        Assert.Equal("gamer", store.Current.SteamAccountName);
        Assert.NotNull(captured);
        Assert.Contains("-manifest-only", captured.Arguments);
        Assert.Contains("-no-mobile", captured.Arguments);
        Assert.Contains("-remember-password", captured.Arguments);
        Assert.Equal("123456\n", captured.StandardInput);
    }

    [Fact]
    public async Task LoginAsync_WithoutGuardCode_PassesNullStdin()
    {
        ExecutableCommand? captured = null;
        IDepotDownloaderRunner runner = Substitute.For<IDepotDownloaderRunner>();
        runner
            .RunAsync(
                Arg.Any<ExecutableCommand>(),
                Arg.Any<IProgress<string>?>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(call =>
            {
                captured = call.Arg<ExecutableCommand>();
                return new ExecutableOutcome(0, []);
            });
        SteamLoginManager manager = Create(new FakeStore(LauncherState.Empty), runner);

        SteamLoginOutcome outcome = await manager.LoginAsync("gamer", "pw", null);

        Assert.True(outcome.IsSuccess);
        Assert.Null(captured!.StandardInput);
    }

    [Fact]
    public async Task LoginAsync_WhenRunnerFails_ReturnsLastOutputLine()
    {
        SteamLoginManager manager = Create(
            runnerOutcome: new ExecutableOutcome(1, ["first", "", "bad password"])
        );

        SteamLoginOutcome outcome = await manager.LoginAsync("gamer", "pw", null);

        Assert.False(outcome.IsSuccess);
        Assert.Null(outcome.AccountName);
        Assert.Equal("bad password", outcome.ErrorMessage);
    }

    [Fact]
    public async Task LoginAsync_WhenRunnerFailsWithoutOutput_ReturnsExitCode()
    {
        SteamLoginManager manager = Create(runnerOutcome: new ExecutableOutcome(3, []));

        SteamLoginOutcome outcome = await manager.LoginAsync("gamer", "pw", null);

        Assert.False(outcome.IsSuccess);
        Assert.Contains("3", outcome.ErrorMessage, StringComparison.Ordinal);
    }

    [Fact]
    public async Task LoginWithQrAsync_WhenSucceeded_ExtractsAccountAndSaves()
    {
        FakeStore store = new(LauncherState.Empty);
        ExecutableCommand? captured = null;
        IDepotDownloaderRunner runner = Substitute.For<IDepotDownloaderRunner>();
        runner
            .RunAsync(
                Arg.Any<ExecutableCommand>(),
                Arg.Any<IProgress<string>?>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(call =>
            {
                captured = call.Arg<ExecutableCommand>();
                return new ExecutableOutcome(
                    0,
                    [
                        "Use the Steam Mobile App to sign in with this QR code:",
                        "████████████",
                        "Success! Next time you can login with -username gamer1 -remember-password instead of -qr.",
                    ]
                );
            });
        SteamLoginManager manager = Create(store, runner);

        SteamLoginOutcome outcome = await manager.LoginWithQrAsync();

        Assert.True(outcome.IsSuccess);
        Assert.Equal("gamer1", outcome.AccountName);
        Assert.Equal("gamer1", store.Current.SteamAccountName);
        Assert.NotNull(captured);
        Assert.Contains("-qr", captured.Arguments);
        Assert.Contains("-remember-password", captured.Arguments);
        Assert.Null(captured.StandardInput);
    }

    [Fact]
    public async Task LoginWithQrAsync_WhenSucceededWithoutParseableAccount_ReturnsFailure()
    {
        FakeStore store = new(LauncherState.Empty);
        SteamLoginManager manager = Create(
            store,
            runnerOutcome: new ExecutableOutcome(0, ["done"])
        );

        SteamLoginOutcome outcome = await manager.LoginWithQrAsync();

        Assert.False(outcome.IsSuccess);
        Assert.Contains("无法解析账号名", outcome.ErrorMessage, StringComparison.Ordinal);
        Assert.Null(store.Current.SteamAccountName);
    }

    [Fact]
    public async Task LoginWithQrAsync_WhenRunnerFails_ReturnsFailure()
    {
        SteamLoginManager manager = Create(runnerOutcome: new ExecutableOutcome(1, ["cancelled"]));

        SteamLoginOutcome outcome = await manager.LoginWithQrAsync();

        Assert.False(outcome.IsSuccess);
        Assert.Equal("cancelled", outcome.ErrorMessage);
    }

    [Fact]
    public async Task LoginWithQrAsync_ForwardsOutputLinesToProgress()
    {
        IDepotDownloaderRunner runner = Substitute.For<IDepotDownloaderRunner>();
        runner
            .RunAsync(
                Arg.Any<ExecutableCommand>(),
                Arg.Any<IProgress<string>?>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(call =>
            {
                call.Arg<IProgress<string>?>()?.Report("qr line 1");
                call.Arg<IProgress<string>?>()?.Report("qr line 2");
                return new ExecutableOutcome(
                    0,
                    [
                        "Success! Next time you can login with -username knight -remember-password instead of -qr.",
                    ]
                );
            });
        SteamLoginManager manager = Create(runner: runner);
        List<string> seen = [];
        IProgress<string> output = new Progress<string>(seen.Add);

        SteamLoginOutcome outcome = await manager.LoginWithQrAsync(output);

        Assert.True(outcome.IsSuccess);
        Assert.Contains("qr line 1", seen);
        Assert.Contains("qr line 2", seen);
    }

    private SteamLoginManager Create(
        FakeStore? store = null,
        IDepotDownloaderRunner? runner = null,
        ExecutableOutcome? runnerOutcome = null,
        bool provisionerInstalled = true
    )
    {
        FakeStore target = store ?? new FakeStore(LauncherState.Empty);
        IDepotDownloaderProvisioner provisioner = Substitute.For<IDepotDownloaderProvisioner>();
        provisioner.IsInstalled().Returns(provisionerInstalled);
        provisioner
            .EnsureAsync(Arg.Any<IProgress<DownloadProgressInfo>?>(), Arg.Any<CancellationToken>())
            .Returns(new DepotDownloaderEnsureResult(true, "ok", Path.Combine(_root, "dd.exe")));
        IDepotDownloaderRunner targetRunner = runner ?? Substitute.For<IDepotDownloaderRunner>();
        if (runner is null && runnerOutcome is not null)
        {
            targetRunner
                .RunAsync(
                    Arg.Any<ExecutableCommand>(),
                    Arg.Any<IProgress<string>?>(),
                    Arg.Any<CancellationToken>()
                )
                .Returns(runnerOutcome);
        }

        return new SteamLoginManager(provisioner, targetRunner, target, TestStrings.Instance);
    }

    private sealed class FakeStore(LauncherState state) : IInstanceStore
    {
        public LauncherState Current { get; private set; } = state;

        public LauncherState Load() => Current;

        public void Save(LauncherState state) => Current = state;
    }
}
