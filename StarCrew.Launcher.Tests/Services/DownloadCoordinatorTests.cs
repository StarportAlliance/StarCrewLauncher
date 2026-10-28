using System.Globalization;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using StarCrew.Launcher.Models;
using StarCrew.Launcher.Services;

namespace StarCrew.Launcher.Tests.Services;

public sealed class DownloadCoordinatorTests : IDisposable
{
    private readonly string _root;
    private readonly string _tools;

    public DownloadCoordinatorTests()
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
    public void Ctor_WithNullStore_ThrowsArgumentNullException() =>
        Assert.Throws<ArgumentNullException>(() =>
            new DownloadCoordinator(
                null!,
                new DepotDownloaderLocator(_tools),
                Substitute.For<IDepotDownloaderRunner>(),
                _root,
                TestStrings.Instance,
                NullLogger<DownloadCoordinator>.Instance
            )
        );

    [Fact]
    public void Ctor_WithNullLocator_ThrowsArgumentNullException() =>
        Assert.Throws<ArgumentNullException>(() =>
            new DownloadCoordinator(
                Substitute.For<IInstanceStore>(),
                null!,
                Substitute.For<IDepotDownloaderRunner>(),
                _root,
                TestStrings.Instance,
                NullLogger<DownloadCoordinator>.Instance
            )
        );

    [Fact]
    public void Ctor_WithNullRunner_ThrowsArgumentNullException() =>
        Assert.Throws<ArgumentNullException>(() =>
            new DownloadCoordinator(
                Substitute.For<IInstanceStore>(),
                new DepotDownloaderLocator(_tools),
                null!,
                _root,
                TestStrings.Instance,
                NullLogger<DownloadCoordinator>.Instance
            )
        );

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Ctor_WithBlankRoot_ThrowsArgumentException(string root) =>
        Assert.Throws<ArgumentException>(() =>
            new DownloadCoordinator(
                Substitute.For<IInstanceStore>(),
                new DepotDownloaderLocator(_tools),
                Substitute.For<IDepotDownloaderRunner>(),
                root,
                TestStrings.Instance,
                NullLogger<DownloadCoordinator>.Instance
            )
        );

    [Fact]
    public async Task InstallAsync_WithoutAccount_ReturnsFailure()
    {
        FakeStore store = new(LauncherState.Empty);
        DownloadCoordinator coordinator = Create(store);

        GameInstallResult result = await coordinator.InstallAsync("A", VersionEntry());

        Assert.False(result.IsSuccess);
        Assert.Equal(TestStrings.Instance.GetString("Install_LoginRequired"), result.Message);
        Assert.Empty(store.Current.Instances);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task InstallAsync_WithBlankName_ThrowsArgumentException(string name)
    {
        FakeStore store = StoreWithAccount();
        DownloadCoordinator coordinator = Create(store);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            coordinator.InstallAsync(name, VersionEntry())
        );
    }

    [Fact]
    public async Task InstallAsync_WhenDepotDownloaderMissing_ReturnsFailureWithoutInstance()
    {
        FakeStore store = StoreWithAccount();
        DownloadCoordinator coordinator = new(
            store,
            new DepotDownloaderLocator(Path.Combine(_root, "empty-tools")),
            Substitute.For<IDepotDownloaderRunner>(),
            _root,
            TestStrings.Instance,
            NullLogger<DownloadCoordinator>.Instance
        );

        GameInstallResult result = await coordinator.InstallAsync("A", VersionEntry());

        Assert.False(result.IsSuccess);
        Assert.Equal(TestStrings.Instance.GetString("Dd_Missing"), result.Message);
        Assert.Empty(store.Current.Instances);
        Assert.Empty(Directory.GetDirectories(_root));
    }

    [Fact]
    public async Task InstallAsync_WhenRunnerFails_RemovesRecordAndDirectory()
    {
        FakeStore store = StoreWithAccount();
        IDepotDownloaderRunner runner = Substitute.For<IDepotDownloaderRunner>();
        runner
            .RunAsync(
                Arg.Any<ExecutableCommand>(),
                Arg.Any<IProgress<string>?>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(new ExecutableOutcome(1, ["boom happened"]));
        DownloadCoordinator coordinator = Create(store, runner: runner);

        GameInstallResult result = await coordinator.InstallAsync("A", VersionEntry());

        Assert.False(result.IsSuccess);
        Assert.Equal(
            TestStrings.Instance.Format("Install_FailedWithReasonFormat", "boom happened"),
            result.Message
        );
        Assert.Null(result.Instance);
        Assert.Empty(store.Current.Instances);
        Assert.Empty(Directory.GetDirectories(_root));
    }

    [Fact]
    public async Task InstallAsync_WhenExeMissing_RemovesRecordAndDirectory()
    {
        FakeStore store = StoreWithAccount();
        IDepotDownloaderRunner runner = Substitute.For<IDepotDownloaderRunner>();
        runner
            .RunAsync(
                Arg.Any<ExecutableCommand>(),
                Arg.Any<IProgress<string>?>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(new ExecutableOutcome(0, []));
        DownloadCoordinator coordinator = Create(store, runner: runner);

        GameInstallResult result = await coordinator.InstallAsync("A", VersionEntry());

        Assert.False(result.IsSuccess);
        Assert.Equal(TestStrings.Instance.GetString("Install_ExeMissing"), result.Message);
        Assert.Null(result.Instance);
        Assert.Empty(store.Current.Instances);
        Assert.Empty(Directory.GetDirectories(_root));
    }

    [Fact]
    public async Task InstallAsync_WhenRunnerThrows_ReturnsFailureWithoutRecord()
    {
        FakeStore store = StoreWithAccount();
        IDepotDownloaderRunner runner = Substitute.For<IDepotDownloaderRunner>();
        runner
            .RunAsync(
                Arg.Any<ExecutableCommand>(),
                Arg.Any<IProgress<string>?>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(
                Task.FromException<ExecutableOutcome>(new InvalidOperationException("kaboom"))
            );
        DownloadCoordinator coordinator = Create(store, runner: runner);

        GameInstallResult result = await coordinator.InstallAsync("A", VersionEntry());

        Assert.False(result.IsSuccess);
        Assert.Equal(TestStrings.Instance.Format("Install_FailedFormat", "kaboom"), result.Message);
        Assert.Null(result.Instance);
        Assert.Empty(store.Current.Instances);
        Assert.Empty(Directory.GetDirectories(_root));
    }

    [Fact]
    public async Task InstallAsync_WhenRunnerSucceeds_MarksInstanceReady()
    {
        FakeStore store = StoreWithAccount();
        IDepotDownloaderRunner runner = Substitute.For<IDepotDownloaderRunner>();
        runner
            .RunAsync(
                Arg.Any<ExecutableCommand>(),
                Arg.Any<IProgress<string>?>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(call =>
            {
                string newest = Directory
                    .GetDirectories(_root)
                    .OrderByDescending(Directory.GetCreationTimeUtc)
                    .First();
                File.WriteAllBytes(Path.Combine(newest, "Among Us.exe"), []);
                return new ExecutableOutcome(0, []);
            });
        DownloadCoordinator coordinator = Create(store, runner: runner);
        List<DownloadProgressInfo> seen = [];
        IProgress<DownloadProgressInfo> progress = new SyncProgress<DownloadProgressInfo>(seen.Add);

        GameInstallResult result = await coordinator.InstallAsync("A", VersionEntry(), progress);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Instance);
        Assert.Equal(InstanceState.Ready, result.Instance.State);
        Assert.Equal(InstanceState.Ready, store.Current.Instances[0].State);
        Assert.Contains(seen, i => i.Stage == DownloadStage.PreparingTool);
        Assert.Contains(seen, i => i.Stage == DownloadStage.Completed && i.Percent == 100);
    }

    [Fact]
    public async Task InstallAsync_WhenCanceled_RemovesRecordAndDirectory()
    {
        FakeStore store = StoreWithAccount();
        IDepotDownloaderRunner runner = Substitute.For<IDepotDownloaderRunner>();
        runner
            .RunAsync(
                Arg.Any<ExecutableCommand>(),
                Arg.Any<IProgress<string>?>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(Task.FromException<ExecutableOutcome>(new OperationCanceledException()));
        DownloadCoordinator coordinator = Create(store, runner: runner);

        GameInstallResult result = await coordinator.InstallAsync("A", VersionEntry());

        Assert.False(result.IsSuccess);
        Assert.Equal(TestStrings.Instance.GetString("Install_Cancelled"), result.Message);
        Assert.Null(result.Instance);
        Assert.Empty(store.Current.Instances);
        Assert.Empty(Directory.GetDirectories(_root));
    }

    [Fact]
    public async Task InstallAsync_ParsesPercentFromRunnerOutput()
    {
        FakeStore store = StoreWithAccount();
        IDepotDownloaderRunner runner = Substitute.For<IDepotDownloaderRunner>();
        runner
            .RunAsync(
                Arg.Any<ExecutableCommand>(),
                Arg.Any<IProgress<string>?>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(call =>
            {
                call.Arg<IProgress<string>?>()?.Report("  50.0% somefile.dat");
                call.Arg<IProgress<string>?>()?.Report("not a percent line");
                return new ExecutableOutcome(1, ["  50.0% somefile.dat"]);
            });
        DownloadCoordinator coordinator = Create(store, runner: runner);
        List<DownloadProgressInfo> seen = [];
        TaskCompletionSource<DownloadProgressInfo> fiftyPct = new(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        IProgress<DownloadProgressInfo> progress = new SyncProgress<DownloadProgressInfo>(info =>
        {
            seen.Add(info);
            if (info.Stage == DownloadStage.DownloadingGame && Math.Abs(info.Percent - 50) < 0.001)
            {
                fiftyPct.TrySetResult(info);
            }
        });

        _ = await coordinator.InstallAsync("A", VersionEntry(), progress);

        DownloadProgressInfo got = await fiftyPct.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(DownloadStage.DownloadingGame, got.Stage);
        Assert.True(Math.Abs(got.Percent - 50) < 0.001);
        Assert.DoesNotContain(
            seen,
            i =>
                i.Stage == DownloadStage.DownloadingGame
                && i.Message.Contains("not a percent", StringComparison.Ordinal)
        );
    }

    [Fact]
    public async Task InstallAsync_ForwardsUsernameAndManifestToRunner()
    {
        FakeStore store = StoreWithAccount("gamer123");
        List<ExecutableCommand> captured = [];
        IDepotDownloaderRunner runner = Substitute.For<IDepotDownloaderRunner>();
        runner
            .RunAsync(
                Arg.Any<ExecutableCommand>(),
                Arg.Any<IProgress<string>?>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(call =>
            {
                captured.Add(call.Arg<ExecutableCommand>()!);
                return new ExecutableOutcome(1, ["x"]);
            });
        DownloadCoordinator coordinator = Create(store, runner: runner);
        GameVersionEntry version = VersionEntry();

        _ = await coordinator.InstallAsync("A", version);

        Assert.Equal(2, captured.Count);
        ExecutableCommand downloading = captured[1];
        Assert.Contains("-manifest", downloading.Arguments);
        Assert.Contains(
            version.ManifestId.ToString(CultureInfo.InvariantCulture),
            downloading.Arguments
        );
        Assert.Contains("gamer123", downloading.Arguments);
        Assert.Contains("-remember-password", downloading.Arguments);
        Assert.DoesNotContain("-manifest-only", downloading.Arguments);
    }

    [Fact]
    public async Task InstallAsync_ReadsTotalBytesFromManifestSummary()
    {
        FakeStore store = StoreWithAccount();
        IDepotDownloaderRunner runner = Substitute.For<IDepotDownloaderRunner>();
        GameVersionEntry version = VersionEntry();
        List<ExecutableCommand> commands = [];
        TaskCompletionSource<DownloadProgressInfo> reported = new(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        runner
            .RunAsync(
                Arg.Any<ExecutableCommand>(),
                Arg.Any<IProgress<string>?>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(call =>
            {
                ExecutableCommand command = call.Arg<ExecutableCommand>()!;
                commands.Add(command);

                if (command.Arguments.Contains("-manifest-only", StringComparer.Ordinal))
                {
                    // DD 只在 -manifest-only 时把摘要落成文本
                    int dirIndex = Array.IndexOf([.. command.Arguments], "-dir");
                    File.WriteAllText(
                        Path.Combine(
                            command.Arguments[dirIndex + 1],
                            $"manifest_945361_{version.ManifestId}.txt"
                        ),
                        "Total bytes on disk    : 1,234,567 \nTotal bytes compressed : 987,654 \n"
                    );
                    return new ExecutableOutcome(0, []);
                }

                call.Arg<IProgress<string>?>()?.Report("  12.34% Among Us.exe");
                return new ExecutableOutcome(0, []);
            });
        DownloadCoordinator coordinator = Create(store, runner: runner);
        IProgress<DownloadProgressInfo> progress = new SyncProgress<DownloadProgressInfo>(info =>
        {
            if (info.Stage == DownloadStage.DownloadingGame && info.TotalBytes is not null)
            {
                reported.TrySetResult(info);
            }
        });

        _ = await coordinator.InstallAsync("A", version, progress);

        DownloadProgressInfo got = await reported.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(1234567, got.TotalBytes);
        Assert.Equal(2, commands.Count);
    }

    [Fact]
    public async Task InstallAsync_WhenManifestSummaryFails_ReportsUnknownTotalBytes()
    {
        FakeStore store = StoreWithAccount();
        IDepotDownloaderRunner runner = Substitute.For<IDepotDownloaderRunner>();
        runner
            .RunAsync(
                Arg.Any<ExecutableCommand>(),
                Arg.Any<IProgress<string>?>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(call =>
            {
                call.Arg<IProgress<string>?>()?.Report("  50.0% Among Us.exe");
                return new ExecutableOutcome(1, ["manifest boom"]);
            });
        DownloadCoordinator coordinator = Create(store, runner: runner);
        List<DownloadProgressInfo> seen = [];
        IProgress<DownloadProgressInfo> progress = new SyncProgress<DownloadProgressInfo>(seen.Add);

        _ = await coordinator.InstallAsync("A", VersionEntry(), progress);

        Assert.DoesNotContain(
            seen,
            i => i.Stage == DownloadStage.DownloadingGame && i.TotalBytes is not null
        );
    }

    private DownloadCoordinator Create(FakeStore store, IDepotDownloaderRunner? runner = null) =>
        new(
            store,
            new DepotDownloaderLocator(_tools),
            runner ?? Substitute.For<IDepotDownloaderRunner>(),
            _root,
            TestStrings.Instance,
            NullLogger<DownloadCoordinator>.Instance
        );

    private static FakeStore StoreWithAccount(string account = "user") =>
        new(new LauncherState(1, null, account, []));

    private static GameVersionEntry VersionEntry() =>
        new("2026.9.5", 990035194164819049L, DateTimeOffset.UtcNow, []);

    private sealed class FakeStore : IInstanceStore
    {
        public FakeStore(LauncherState state)
        {
            Current = state;
        }

        public LauncherState Current { get; private set; }

        public LauncherState Load() => Current;

        public void Save(LauncherState state) => Current = state;
    }

    // 同步执行回调，避免线程池异步延迟
    private sealed class SyncProgress<T>(Action<T> handler) : IProgress<T>
    {
        private readonly Action<T> _handler = handler;

        public void Report(T value) => _handler(value);
    }
}
