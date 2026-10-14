using NSubstitute;
using StarCrew.Launcher.Models;
using StarCrew.Launcher.Services;
using StarCrew.Launcher.ViewModels;

namespace StarCrew.Launcher.Tests.ViewModels;

/// <summary>AboutViewModel 的单元测试：更新检查/下载的提示与事件全部走替身。</summary>
public sealed class AboutViewModelTests
{
    [Fact]
    public void Ctor_WithNullUpdater_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new AboutViewModel(
                null!,
                Substitute.For<IExternalLauncher>(),
                Substitute.For<INotificationService>(),
                Substitute.For<IAppInfo>()
            )
        );
    }

    [Fact]
    public void Ctor_WithNullLauncher_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new AboutViewModel(
                new AppUpdater(Substitute.For<IUpdateClient>()),
                null!,
                Substitute.For<INotificationService>(),
                Substitute.For<IAppInfo>()
            )
        );
    }

    [Fact]
    public void Ctor_WithNullNotifications_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new AboutViewModel(
                new AppUpdater(Substitute.For<IUpdateClient>()),
                Substitute.For<IExternalLauncher>(),
                null!,
                Substitute.For<IAppInfo>()
            )
        );
    }

    [Fact]
    public void Ctor_WithNullAppInfo_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new AboutViewModel(
                new AppUpdater(Substitute.For<IUpdateClient>()),
                Substitute.For<IExternalLauncher>(),
                Substitute.For<INotificationService>(),
                null!
            )
        );
    }

    [Theory]
    [InlineData(null, "版本 未知")]
    [InlineData("0.3.0", "版本 0.3.0")]
    public void VersionDescription_FormatsAppVersion(string? version, string expected)
    {
        (AboutViewModel vm, _, _, _, IAppInfo appInfo) = Create();
        appInfo.Version.Returns(version);

        Assert.Equal(expected, vm.VersionDescription);
    }

    [Fact]
    public async Task CheckUpdateCommand_WhenUpToDate_NotifiesLatest()
    {
        (AboutViewModel vm, IUpdateClient client, _, INotificationService notifications, _) =
            Create();
        client.CurrentVersion.Returns("0.3.0");
        client
            .CheckForUpdatesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<string?>(null));

        await vm.CheckUpdateCommand.ExecuteAsync(null);

        notifications
            .Received(1)
            .Notify("已是最新版本（0.3.0）。", null, NotificationSeverity.Success);
    }

    [Fact]
    public async Task CheckUpdateCommand_WhenAvailable_RaisesUpdateAvailable()
    {
        (AboutViewModel vm, IUpdateClient client, _, INotificationService notifications, _) =
            Create();
        client
            .CheckForUpdatesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<string?>("0.4.0"));
        string? seen = null;
        vm.UpdateAvailable += (_, version) => seen = version;

        await vm.CheckUpdateCommand.ExecuteAsync(null);

        Assert.Equal("0.4.0", seen);
        notifications.DidNotReceiveWithAnyArgs().Notify(default!, default!, default);
    }

    [Fact]
    public async Task CheckUpdateCommand_WhenNotInstalled_NotifiesNotInstalled()
    {
        (AboutViewModel vm, IUpdateClient client, _, INotificationService notifications, _) =
            Create();
        client.IsInstalled.Returns(false);

        await vm.CheckUpdateCommand.ExecuteAsync(null);

        notifications
            .Received(1)
            .Notify(
                "当前为开发运行，未经安装，跳过更新检查。",
                null,
                NotificationSeverity.Informational
            );
    }

    [Fact]
    public async Task CheckUpdateCommand_WhenFailed_NotifiesError()
    {
        (AboutViewModel vm, IUpdateClient client, _, INotificationService notifications, _) =
            Create();
        client
            .CheckForUpdatesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException<string?>(new InvalidOperationException("boom")));

        await vm.CheckUpdateCommand.ExecuteAsync(null);

        notifications.Received(1).Notify("检查更新失败：boom", null, NotificationSeverity.Error);
    }

    [Fact]
    public async Task DownloadUpdateAsync_WhenReadyToRestart_ReturnsTrue()
    {
        (AboutViewModel vm, IUpdateClient client, _, INotificationService notifications, _) =
            Create();
        client
            .DownloadUpdatesAsync(
                Arg.Any<string>(),
                Arg.Any<IProgress<int>?>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(Task.CompletedTask);

        bool ready = await vm.DownloadUpdateAsync("0.4.0", null, CancellationToken.None);

        Assert.True(ready);
        client.Received(1).PrepareUpdateForRestart("0.4.0");
        notifications.DidNotReceiveWithAnyArgs().Notify(default!, default!, default);
    }

    [Fact]
    public async Task DownloadUpdateAsync_WhenFailed_NotifiesErrorAndReturnsFalse()
    {
        (AboutViewModel vm, IUpdateClient client, _, INotificationService notifications, _) =
            Create();
        client
            .DownloadUpdatesAsync(
                Arg.Any<string>(),
                Arg.Any<IProgress<int>?>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(Task.FromException(new InvalidOperationException("dl boom")));

        bool ready = await vm.DownloadUpdateAsync("0.4.0", null, CancellationToken.None);

        Assert.False(ready);
        notifications.Received(1).Notify("下载更新失败：dl boom", null, NotificationSeverity.Error);
    }

    [Fact]
    public async Task DownloadUpdateAsync_WhenCanceled_NotifiesCanceledAndReturnsFalse()
    {
        (AboutViewModel vm, _, _, INotificationService notifications, _) = Create();
        using CancellationTokenSource cts = new();
        await cts.CancelAsync();

        bool ready = await vm.DownloadUpdateAsync("0.4.0", null, cts.Token);

        Assert.False(ready);
        notifications
            .Received(1)
            .Notify("已取消新版本下载。", null, NotificationSeverity.Informational);
    }

    [Fact]
    public async Task OpenUriAsync_DelegatesToExternalLauncher()
    {
        (AboutViewModel vm, _, IExternalLauncher launcher, _, _) = Create();

        await vm.OpenUriAsync("https://stcrew.app");

        await launcher.Received(1).OpenUriAsync("https://stcrew.app");
    }

    private static (
        AboutViewModel Vm,
        IUpdateClient Client,
        IExternalLauncher Launcher,
        INotificationService Notifications,
        IAppInfo AppInfo
    ) Create()
    {
        IUpdateClient client = Substitute.For<IUpdateClient>();
        client.IsInstalled.Returns(true);
        client.CurrentVersion.Returns("0.3.0");
        IExternalLauncher launcher = Substitute.For<IExternalLauncher>();
        INotificationService notifications = Substitute.For<INotificationService>();
        IAppInfo appInfo = Substitute.For<IAppInfo>();
        appInfo.Version.Returns("0.3.0");
        AppUpdater updater = new(client);
        return (
            new AboutViewModel(updater, launcher, notifications, appInfo),
            client,
            launcher,
            notifications,
            appInfo
        );
    }
}
