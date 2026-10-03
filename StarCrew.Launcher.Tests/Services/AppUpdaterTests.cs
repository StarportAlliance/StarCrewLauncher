using NSubstitute;
using NSubstitute.Core;
using StarCrew.Launcher.Models;
using StarCrew.Launcher.Services;
using Velopack.Exceptions;

namespace StarCrew.Launcher.Tests.Services;

/// <summary>AppUpdater 的单元测试：全流程用 IUpdateClient 替身覆盖，无真实网络。</summary>
public sealed class AppUpdaterTests
{
    [Fact]
    public void Ctor_WithNullClient_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new AppUpdater(null!));
    }

    [Fact]
    public void CurrentVersion_DelegatesToClient()
    {
        IUpdateClient client = Substitute.For<IUpdateClient>();
        client.CurrentVersion.Returns("0.3.0");
        AppUpdater updater = new(client);

        Assert.Equal("0.3.0", updater.CurrentVersion);
    }

    [Fact]
    public async Task CheckAndPrepare_WithCanceledToken_ThrowsOperationCanceled()
    {
        IUpdateClient client = Substitute.For<IUpdateClient>();
        AppUpdater updater = new(client);
        using CancellationTokenSource cts = new();
        await cts.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            updater.CheckAndPrepareUpdateAsync(cancellationToken: cts.Token)
        );
    }

    [Fact]
    public async Task CheckAndPrepare_NotInstalled_ReturnsNotInstalledWithoutCheck()
    {
        IUpdateClient client = Substitute.For<IUpdateClient>();
        client.IsInstalled.Returns(false);
        AppUpdater updater = new(client);

        UpdateCheckResult result = await updater.CheckAndPrepareUpdateAsync();

        Assert.Equal(UpdateState.NotInstalled, result.State);
        Assert.Null(result.AvailableVersion);
        await client.DidNotReceive().CheckForUpdatesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CheckAndPrepare_NoUpdate_ReturnsUpToDate()
    {
        IUpdateClient client = Substitute.For<IUpdateClient>();
        client.IsInstalled.Returns(true);
        client.CurrentVersion.Returns("0.3.0");
        client.CheckForUpdatesAsync(Arg.Any<CancellationToken>()).Returns((string?)null);
        AppUpdater updater = new(client);

        UpdateCheckResult result = await updater.CheckAndPrepareUpdateAsync();

        Assert.Equal(UpdateState.UpToDate, result.State);
        Assert.Null(result.AvailableVersion);
        Assert.Equal("已是最新版本（0.3.0）。", result.Message);
        await client
            .DidNotReceive()
            .DownloadUpdatesAsync(
                Arg.Any<string>(),
                Arg.Any<IProgress<int>?>(),
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task CheckAndPrepare_UpdateAvailable_DownloadsAndSchedulesRestart()
    {
        IUpdateClient client = Substitute.For<IUpdateClient>();
        client.IsInstalled.Returns(true);
        client.CheckForUpdatesAsync(Arg.Any<CancellationToken>()).Returns("0.4.0");
        AppUpdater updater = new(client);
        Progress<int> progress = new();

        UpdateCheckResult result = await updater.CheckAndPrepareUpdateAsync(progress);

        Assert.Equal(UpdateState.ReadyToRestart, result.State);
        Assert.Equal("0.4.0", result.AvailableVersion);
        Assert.Equal("已下载新版本 0.4.0，重启后生效。", result.Message);
        await client
            .Received(1)
            .DownloadUpdatesAsync("0.4.0", progress, Arg.Any<CancellationToken>());
        client.Received(1).PrepareUpdateForRestart("0.4.0");
    }

    [Fact]
    public async Task CheckAndPrepare_CheckThrowsNotInstalled_ReturnsNotInstalled()
    {
        IUpdateClient client = Substitute.For<IUpdateClient>();
        client.IsInstalled.Returns(true);
        // NotInstalledException 无公开构造器，只能用 Velopack 真抛出的实例做替身行为。
        NotInstalledException notInstalled = await CaptureNotInstalledAsync();
        client
            .CheckForUpdatesAsync(Arg.Any<CancellationToken>())
            .Returns((Func<CallInfo, string?>)(_ => throw notInstalled));
        AppUpdater updater = new(client);

        UpdateCheckResult result = await updater.CheckAndPrepareUpdateAsync();

        Assert.Equal(UpdateState.NotInstalled, result.State);
        Assert.Null(result.AvailableVersion);
    }

    [Fact]
    public async Task CheckAndPrepare_CheckThrows_ReturnsFailed()
    {
        IUpdateClient client = Substitute.For<IUpdateClient>();
        client.IsInstalled.Returns(true);
        client
            .CheckForUpdatesAsync(Arg.Any<CancellationToken>())
            .Returns((Func<CallInfo, string?>)(_ => throw new InvalidOperationException("boom")));
        AppUpdater updater = new(client);

        UpdateCheckResult result = await updater.CheckAndPrepareUpdateAsync();

        Assert.Equal(UpdateState.Failed, result.State);
        Assert.Null(result.AvailableVersion);
        Assert.Contains("boom", result.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CheckAndPrepare_DownloadThrows_ReturnsFailedWithoutSchedule()
    {
        IUpdateClient client = Substitute.For<IUpdateClient>();
        client.IsInstalled.Returns(true);
        client.CheckForUpdatesAsync(Arg.Any<CancellationToken>()).Returns("0.4.0");
        client
            .DownloadUpdatesAsync(
                Arg.Any<string>(),
                Arg.Any<IProgress<int>?>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(_ => throw new IOException("net down"));
        AppUpdater updater = new(client);

        UpdateCheckResult result = await updater.CheckAndPrepareUpdateAsync();

        Assert.Equal(UpdateState.Failed, result.State);
        Assert.Null(result.AvailableVersion);
        client.DidNotReceive().PrepareUpdateForRestart(Arg.Any<string>());
    }

    /// <summary>抓取 Velopack 真抛出的 NotInstalledException，该类型无公开构造器，只能接住现成的。</summary>
    private static async Task<NotInstalledException> CaptureNotInstalledAsync()
    {
        VelopackUpdateClient real = new("https://example.com/updates");
        return await Assert.ThrowsAsync<NotInstalledException>(() =>
            real.CheckForUpdatesAsync(default)
        );
    }
}
