using NSubstitute;
using StarCrew.Launcher.Models;
using StarCrew.Launcher.Services;

namespace StarCrew.Launcher.Tests.Services;

public sealed class SteamLoginManagerTests : IDisposable
{
    private readonly string _root;
    private readonly string _tools;

    public SteamLoginManagerTests()
    {
        _root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        _tools = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
        Directory.CreateDirectory(_tools);
        File.WriteAllBytes(Path.Combine(_tools, "DepotDownloader.exe"), []);
    }

    public void Dispose()
    {
        foreach (string dir in new[] { _root, _tools })
        {
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, recursive: true);
            }
        }
    }

    [Fact]
    public void Ctor_WithNullLocator_ThrowsArgumentNullException() =>
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
                new DepotDownloaderLocator(_tools),
                null!,
                Substitute.For<IInstanceStore>(),
                TestStrings.Instance
            )
        );

    [Fact]
    public void Ctor_WithNullStore_ThrowsArgumentNullException() =>
        Assert.Throws<ArgumentNullException>(() =>
            new SteamLoginManager(
                new DepotDownloaderLocator(_tools),
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
    public async Task LoginAsync_WhenDepotDownloaderMissing_ReturnsFailure()
    {
        SteamLoginManager manager = Create(locatorDirectory: Path.Combine(_root, "empty-tools"));

        SteamLoginOutcome outcome = await manager.LoginAsync("user", "pw", null);

        Assert.False(outcome.IsSuccess);
        Assert.Equal(TestStrings.Instance.GetString("Dd_Missing"), outcome.ErrorMessage);
    }

    [Fact]
    public async Task LoginWithQrAsync_WhenDepotDownloaderMissing_ReturnsFailure()
    {
        SteamLoginManager manager = Create(locatorDirectory: Path.Combine(_root, "empty-tools"));

        SteamLoginOutcome outcome = await manager.LoginWithQrAsync();

        Assert.False(outcome.IsSuccess);
        Assert.Equal(TestStrings.Instance.GetString("Dd_Missing"), outcome.ErrorMessage);
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
        Assert.Equal(
            TestStrings.Instance.Format("Login_FailedExitCodeFormat", 3),
            outcome.ErrorMessage
        );
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
        Assert.Equal(
            TestStrings.Instance.GetString("Login_AccountNameParseFailed"),
            outcome.ErrorMessage
        );
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
        string? locatorDirectory = null
    )
    {
        FakeStore target = store ?? new FakeStore(LauncherState.Empty);
        DepotDownloaderLocator locator = new(locatorDirectory ?? _tools);
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

        return new SteamLoginManager(locator, targetRunner, target, TestStrings.Instance);
    }

    private sealed class FakeStore(LauncherState state) : IInstanceStore
    {
        public LauncherState Current { get; private set; } = state;

        public LauncherState Load() => Current;

        public void Save(LauncherState state) => Current = state;
    }
}
