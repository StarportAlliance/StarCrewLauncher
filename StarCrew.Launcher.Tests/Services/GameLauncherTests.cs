using StarCrew.Launcher.Services;

namespace StarCrew.Launcher.Tests.Services;

/// <summary>
/// GameLauncher 的无副作用测试：只覆盖参数校验与失败路径。
/// steam:// 协议拉起成功分支会真实拉起游戏，不在单元测试里调用，
/// 等 IProcessStarter 接缝拆出来后再用 NSubstitute 覆盖。
/// </summary>
public sealed class GameLauncherTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void TryLaunchExe_WithBlankPath_ThrowsArgumentException(string exePath)
    {
        GameLauncher launcher = new();

        Assert.Throws<ArgumentException>(() => launcher.TryLaunchExe(exePath, out string? _));
    }

    [Fact]
    public void TryLaunchExe_WithMissingFile_ReturnsFalseWithError()
    {
        GameLauncher launcher = new();
        string missing = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".exe");

        bool ok = launcher.TryLaunchExe(missing, out string? error);

        Assert.False(ok);
        Assert.False(string.IsNullOrWhiteSpace(error));
    }

    [Fact]
    public async Task LaunchAsync_WithCanceledToken_ThrowsOperationCanceled()
    {
        GameLauncher launcher = new();
        using CancellationTokenSource cts = new();
        await cts.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(() => launcher.LaunchAsync(cts.Token));
    }
}
