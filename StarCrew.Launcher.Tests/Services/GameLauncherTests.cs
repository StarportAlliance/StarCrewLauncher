using NSubstitute;
using StarCrew.Launcher.Models;
using StarCrew.Launcher.Services;

namespace StarCrew.Launcher.Tests.Services;

/// <summary>GameLauncher 的单元测试：成功路径用 NSubstitute 替身覆盖，无真实副作用。</summary>
public sealed class GameLauncherTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void TryLaunchExe_WithBlankPath_ThrowsArgumentException(string exePath)
    {
        ISteamGameLocator locator = Substitute.For<ISteamGameLocator>();
        IProcessStarter starter = Substitute.For<IProcessStarter>();
        GameLauncher launcher = new(locator, starter);

        Assert.Throws<ArgumentException>(() => launcher.TryLaunchExe(exePath, out string? _));
    }

    [Fact]
    public void TryLaunchExe_WithMissingFile_ReturnsFalseWithError()
    {
        ISteamGameLocator locator = Substitute.For<ISteamGameLocator>();
        IProcessStarter starter = new ProcessStarter();
        GameLauncher launcher = new(locator, starter);
        string missing = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".exe");

        bool ok = launcher.TryLaunchExe(missing, out string? error);

        Assert.False(ok);
        Assert.False(string.IsNullOrWhiteSpace(error));
    }

    [Fact]
    public void Ctor_WithNullLocator_ThrowsArgumentNullException()
    {
        IProcessStarter starter = Substitute.For<IProcessStarter>();

        Assert.Throws<ArgumentNullException>(() => new GameLauncher(null!, starter));
    }

    [Fact]
    public void Ctor_WithNullStarter_ThrowsArgumentNullException()
    {
        ISteamGameLocator locator = Substitute.For<ISteamGameLocator>();

        Assert.Throws<ArgumentNullException>(() => new GameLauncher(locator, null!));
    }

    [Fact]
    public async Task LaunchAsync_WithCanceledToken_ThrowsOperationCanceled()
    {
        ISteamGameLocator locator = Substitute.For<ISteamGameLocator>();
        IProcessStarter starter = Substitute.For<IProcessStarter>();
        GameLauncher launcher = new(locator, starter);
        using CancellationTokenSource cts = new();
        await cts.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(() => launcher.LaunchAsync(cts.Token));
    }

    [Fact]
    public async Task LaunchAsync_SteamSuccess_ReturnsSteamProtocol()
    {
        ISteamGameLocator locator = Substitute.For<ISteamGameLocator>();
        IProcessStarter starter = Substitute.For<IProcessStarter>();
        string? steamOut = null;
        starter
            .TryStart(
                Arg.Is<string>(s =>
                    s != null && s.StartsWith("steam://", StringComparison.Ordinal)
                ),
                Arg.Any<string>(),
                out steamOut
            )
            .Returns(true);
        GameLauncher launcher = new(locator, starter);

        LaunchResult result = await launcher.LaunchAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(LaunchMethod.SteamProtocol, result.Method);
        Assert.Equal(
            "已通过 Steam 发起启动，如果游戏没有弹出请确认 Steam 已登录。",
            result.Message
        );
        locator.DidNotReceiveWithAnyArgs().FindGameExe();
    }

    [Fact]
    public async Task LaunchAsync_SteamFailsExeSucceeds_ReturnsDirectExe()
    {
        ISteamGameLocator locator = Substitute.For<ISteamGameLocator>();
        IProcessStarter starter = Substitute.For<IProcessStarter>();
        string exe = Path.Combine("C:", "Games", "Among Us.exe");
        locator.FindGameExe().Returns(exe);
        string? steamOut = null;
        starter
            .TryStart(
                Arg.Is<string>(s =>
                    s != null && s.StartsWith("steam://", StringComparison.Ordinal)
                ),
                Arg.Any<string>(),
                out steamOut
            )
            .Returns(call =>
            {
                call[2] = "steam boom";
                return false;
            });
        string? exeOut = null;
        starter
            .TryStart(
                Arg.Is<string>(s =>
                    s != null && s.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
                ),
                Arg.Any<string>(),
                out exeOut
            )
            .Returns(true);
        GameLauncher launcher = new(locator, starter);

        LaunchResult result = await launcher.LaunchAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(LaunchMethod.DirectExe, result.Method);
        Assert.Equal($"已直接启动 {Path.GetFileName(exe)}。", result.Message);
    }

    [Fact]
    public async Task LaunchAsync_AllFailWithExe_ReturnsFailureWithSteamSuffix()
    {
        ISteamGameLocator locator = Substitute.For<ISteamGameLocator>();
        IProcessStarter starter = Substitute.For<IProcessStarter>();
        string exe = Path.Combine("C:", "Games", "Among Us.exe");
        locator.FindGameExe().Returns(exe);
        string? steamOut = null;
        starter
            .TryStart(
                Arg.Is<string>(s =>
                    s != null && s.StartsWith("steam://", StringComparison.Ordinal)
                ),
                Arg.Any<string>(),
                out steamOut
            )
            .Returns(call =>
            {
                call[2] = "steam boom";
                return false;
            });
        string? exeOut = null;
        starter
            .TryStart(
                Arg.Is<string>(s =>
                    s != null && s.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
                ),
                Arg.Any<string>(),
                out exeOut
            )
            .Returns(call =>
            {
                call[2] = "exe boom";
                return false;
            });
        GameLauncher launcher = new(locator, starter);

        LaunchResult result = await launcher.LaunchAsync();

        Assert.False(result.IsSuccess);
        Assert.Equal(LaunchMethod.None, result.Method);
        Assert.Equal(
            "通过 Steam 与直接启动均失败，请手动选择游戏文件后重试。（Steam 拉起失败：steam boom）",
            result.Message
        );
    }

    [Fact]
    public async Task LaunchAsync_AllFailWithoutExe_ReturnsFailureWithSteamSuffix()
    {
        ISteamGameLocator locator = Substitute.For<ISteamGameLocator>();
        IProcessStarter starter = Substitute.For<IProcessStarter>();
        locator.FindGameExe().Returns((string?)null);
        string? steamOut = null;
        starter
            .TryStart(
                Arg.Is<string>(s =>
                    s != null && s.StartsWith("steam://", StringComparison.Ordinal)
                ),
                Arg.Any<string>(),
                out steamOut
            )
            .Returns(call =>
            {
                call[2] = "steam boom";
                return false;
            });
        GameLauncher launcher = new(locator, starter);

        LaunchResult result = await launcher.LaunchAsync();

        Assert.False(result.IsSuccess);
        Assert.Equal(LaunchMethod.None, result.Method);
        Assert.Equal(
            "没有找到 Steam，也没有在 Steam 游戏库中找到 Among Us，请手动选择游戏文件。（Steam 拉起失败：steam boom）",
            result.Message
        );
    }

    [Fact]
    public async Task LaunchAsync_AllFailWithoutSteamError_ReturnsFailureWithoutSuffix()
    {
        ISteamGameLocator locator = Substitute.For<ISteamGameLocator>();
        IProcessStarter starter = Substitute.For<IProcessStarter>();
        locator.FindGameExe().Returns((string?)null);
        string? steamOut = null;
        starter.TryStart(Arg.Any<string>(), Arg.Any<string>(), out steamOut).Returns(false);
        GameLauncher launcher = new(locator, starter);

        LaunchResult result = await launcher.LaunchAsync();

        Assert.False(result.IsSuccess);
        Assert.Equal(LaunchMethod.None, result.Method);
        Assert.Equal(
            "没有找到 Steam，也没有在 Steam 游戏库中找到 Among Us，请手动选择游戏文件。",
            result.Message
        );
    }

    [Fact]
    public void TryLaunchExe_DelegatesWorkingDirectory_ToStarter()
    {
        ISteamGameLocator locator = Substitute.For<ISteamGameLocator>();
        IProcessStarter starter = Substitute.For<IProcessStarter>();
        string? outError = null;
        starter.TryStart(Arg.Any<string>(), Arg.Any<string>(), out outError).Returns(true);
        GameLauncher launcher = new(locator, starter);
        string exe = Path.Combine("C:", "Games", "Among Us.exe");

        bool ok = launcher.TryLaunchExe(exe, out string? error);

        Assert.True(ok);
        starter
            .Received(1)
            .TryStart(exe, Arg.Is<string>(d => d == Path.GetDirectoryName(exe)), out outError);
    }

    [Fact]
    public void TryLaunchBySteamProtocol_UsesBaseDirectory_AsWorkingDirectory()
    {
        ISteamGameLocator locator = Substitute.For<ISteamGameLocator>();
        IProcessStarter starter = Substitute.For<IProcessStarter>();
        string? outError = null;
        starter.TryStart(Arg.Any<string>(), Arg.Any<string>(), out outError).Returns(true);
        GameLauncher launcher = new(locator, starter);

        bool ok = launcher.TryLaunchBySteamProtocol(out string? error);

        Assert.True(ok);
        starter
            .Received(1)
            .TryStart(
                $"steam://rungameid/{SteamGameLocator.AmongUsAppId}",
                Arg.Is<string>(d => d == AppContext.BaseDirectory),
                out outError
            );
    }
}
