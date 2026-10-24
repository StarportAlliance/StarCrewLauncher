using NSubstitute;
using StarCrew.Launcher.Models;
using StarCrew.Launcher.Services;

namespace StarCrew.Launcher.Tests.Services;

public sealed class InstanceLauncherTests : IDisposable
{
    private readonly string _root;

    public InstanceLauncherTests()
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

    private GameInstance CreateInstance(
        string name = "A",
        InstanceState state = InstanceState.Ready
    ) =>
        new(
            Guid.NewGuid(),
            name,
            Path.Combine(_root, name.Replace(' ', '_')),
            945360,
            945361,
            990035194164819049L,
            "2026.9.1",
            state,
            DateTimeOffset.UtcNow,
            null
        );

    private static string CreateExeFor(GameInstance instance)
    {
        Directory.CreateDirectory(instance.InstallDirectory);
        string exe = Path.Combine(instance.InstallDirectory, "Among Us.exe");
        File.WriteAllBytes(exe, []);
        return exe;
    }

    [Fact]
    public void Ctor_WithNullStarter_ThrowsArgumentNullException()
    {
        ISteamClient steam = Substitute.For<ISteamClient>();

        Assert.Throws<ArgumentNullException>(() =>
            new InstanceLauncher(null!, steam, TestStrings.Instance)
        );
    }

    [Fact]
    public void Ctor_WithNullSteam_ThrowsArgumentNullException()
    {
        IProcessStarter starter = Substitute.For<IProcessStarter>();

        Assert.Throws<ArgumentNullException>(() =>
            new InstanceLauncher(starter, null!, TestStrings.Instance)
        );
    }

    [Fact]
    public async Task LaunchAsync_WithIncompleteInstance_ReturnsFailureAndSkipsStarter()
    {
        IProcessStarter starter = Substitute.For<IProcessStarter>();
        ISteamClient steam = Substitute.For<ISteamClient>();
        InstanceLauncher launcher = new(starter, steam, TestStrings.Instance);
        GameInstance instance = CreateInstance(state: InstanceState.Incomplete);

        LaunchOutcome outcome = await launcher.LaunchAsync(instance);

        Assert.False(outcome.IsSuccess);
        Assert.Equal(TestStrings.Instance.GetString("Launch_NotReady"), outcome.Message);
        Assert.False(steam.IsSteamRunning());
        starter.DidNotReceiveWithAnyArgs().TryStart(default!, default!, out _);
    }

    [Fact]
    public async Task LaunchAsync_WithMissingExe_ReturnsFailure()
    {
        IProcessStarter starter = Substitute.For<IProcessStarter>();
        ISteamClient steam = Substitute.For<ISteamClient>();
        steam.IsSteamRunning().Returns(true);
        InstanceLauncher launcher = new(starter, steam, TestStrings.Instance);
        GameInstance instance = CreateInstance();
        Directory.CreateDirectory(instance.InstallDirectory);

        LaunchOutcome outcome = await launcher.LaunchAsync(instance);

        Assert.False(outcome.IsSuccess);
        Assert.Equal(TestStrings.Instance.GetString("Launch_GameFileMissing"), outcome.Message);
    }

    [Fact]
    public async Task LaunchAsync_WithSteamRunningAndExe_StarterSucceeded_ReturnsSuccess()
    {
        IProcessStarter starter = Substitute.For<IProcessStarter>();
        starter.TryStart(Arg.Any<string>(), Arg.Any<string>(), out _).Returns(true);
        ISteamClient steam = Substitute.For<ISteamClient>();
        steam.IsSteamRunning().Returns(true);
        InstanceLauncher launcher = new(starter, steam, TestStrings.Instance);
        GameInstance instance = CreateInstance();
        _ = CreateExeFor(instance);

        LaunchOutcome outcome = await launcher.LaunchAsync(instance);

        Assert.True(outcome.IsSuccess);
        Assert.Equal(TestStrings.Instance.GetString("Launch_Started"), outcome.Message);
    }

    [Fact]
    public async Task LaunchAsync_WhenStartSteamFails_ContinuesLaunching()
    {
        IProcessStarter starter = Substitute.For<IProcessStarter>();
        starter.TryStart(Arg.Any<string>(), Arg.Any<string>(), out _).Returns(true);
        ISteamClient steam = Substitute.For<ISteamClient>();
        steam.IsSteamRunning().Returns(false);
        steam.TryStartSteam(out _).Returns(false);
        InstanceLauncher launcher = new(starter, steam, TestStrings.Instance);
        GameInstance instance = CreateInstance();
        _ = CreateExeFor(instance);

        LaunchOutcome outcome = await launcher.LaunchAsync(instance);

        Assert.True(outcome.IsSuccess);
        Assert.False(steam.IsSteamRunning());
    }

    [Fact]
    public async Task LaunchAsync_WhenStartSteamSucceeds_WaitsForProcess()
    {
        IProcessStarter starter = Substitute.For<IProcessStarter>();
        starter.TryStart(Arg.Any<string>(), Arg.Any<string>(), out _).Returns(true);
        ISteamClient steam = Substitute.For<ISteamClient>();
        // 首次 false，之后 true（启动成功后视为已在线）
        steam.IsSteamRunning().Returns(false, true);
        steam.TryStartSteam(out _).Returns(true);
        InstanceLauncher launcher = new(starter, steam, TestStrings.Instance);
        GameInstance instance = CreateInstance();
        _ = CreateExeFor(instance);

        LaunchOutcome outcome = await launcher.LaunchAsync(instance);

        Assert.True(outcome.IsSuccess);
    }

    [Fact]
    public async Task LaunchAsync_WithStarterFailure_ReturnsFailureFromStarter()
    {
        IProcessStarter starter = Substitute.For<IProcessStarter>();
        starter
            .TryStart(Arg.Any<string>(), Arg.Any<string>(), out string? outError)
            .Returns(call =>
            {
                call[2] = "boom";
                return false;
            });
        ISteamClient steam = Substitute.For<ISteamClient>();
        steam.IsSteamRunning().Returns(true);
        InstanceLauncher launcher = new(starter, steam, TestStrings.Instance);
        GameInstance instance = CreateInstance();
        _ = CreateExeFor(instance);

        LaunchOutcome outcome = await launcher.LaunchAsync(instance);

        Assert.False(outcome.IsSuccess);
        Assert.Equal("boom", outcome.Message);
    }

    [Fact]
    public async Task LaunchAsync_WithCanceledToken_Throws()
    {
        IProcessStarter starter = Substitute.For<IProcessStarter>();
        ISteamClient steam = Substitute.For<ISteamClient>();
        InstanceLauncher launcher = new(starter, steam, TestStrings.Instance);
        GameInstance instance = CreateInstance();
        using CancellationTokenSource cts = new();
        await cts.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            launcher.LaunchAsync(instance, cts.Token)
        );
    }
}
