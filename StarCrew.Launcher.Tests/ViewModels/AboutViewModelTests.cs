using NSubstitute;
using StarCrew.Launcher.Models;
using StarCrew.Launcher.Services;
using StarCrew.Launcher.ViewModels;

namespace StarCrew.Launcher.Tests.ViewModels;

public sealed class AboutViewModelTests
{
    [Fact]
    public void Ctor_WithNullUpdater_ThrowsArgumentNullException() =>
        Assert.Throws<ArgumentNullException>(() =>
            new AboutViewModel(
                null!,
                Substitute.For<IExternalLauncher>(),
                Substitute.For<INotificationService>(),
                Substitute.For<IAppInfo>(),
                TestStrings.Instance
            )
        );

    [Fact]
    public void Ctor_WithNullLauncher_ThrowsArgumentNullException() =>
        Assert.Throws<ArgumentNullException>(() =>
            new AboutViewModel(
                new AppUpdater(Substitute.For<IUpdateClient>(), TestStrings.Instance),
                null!,
                Substitute.For<INotificationService>(),
                Substitute.For<IAppInfo>(),
                TestStrings.Instance
            )
        );

    [Fact]
    public void Ctor_WithNullNotifications_ThrowsArgumentNullException() =>
        Assert.Throws<ArgumentNullException>(() =>
            new AboutViewModel(
                new AppUpdater(Substitute.For<IUpdateClient>(), TestStrings.Instance),
                Substitute.For<IExternalLauncher>(),
                null!,
                Substitute.For<IAppInfo>(),
                TestStrings.Instance
            )
        );

    [Fact]
    public void Ctor_WithNullAppInfo_ThrowsArgumentNullException() =>
        Assert.Throws<ArgumentNullException>(() =>
            new AboutViewModel(
                new AppUpdater(Substitute.For<IUpdateClient>(), TestStrings.Instance),
                Substitute.For<IExternalLauncher>(),
                Substitute.For<INotificationService>(),
                null!,
                TestStrings.Instance
            )
        );

    [Theory]
    [InlineData(null)]
    [InlineData("0.3.0")]
    public void VersionDescription_FormatsAppVersion(string? version)
    {
        (AboutViewModel vm, _, _, _, IAppInfo appInfo) = Create();
        appInfo.Version.Returns(version);
        string expected = version is null
            ? TestStrings.Instance.GetString("About_VersionUnknown")
            : TestStrings.Instance.Format("About_VersionFormat", version);

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
            .Notify(
                TestStrings.Instance.Format("Update_UpToDateFormat", "0.3.0"),
                null,
                NotificationSeverity.Success
            );
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
                TestStrings.Instance.GetString("Update_DevRunSkip"),
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

        notifications
            .Received(1)
            .Notify(
                TestStrings.Instance.Format("Update_CheckFailedFormat", "boom"),
                null,
                NotificationSeverity.Error
            );
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
        notifications
            .Received(1)
            .Notify(
                TestStrings.Instance.Format("Update_DownloadFailedFormat", "dl boom"),
                null,
                NotificationSeverity.Error
            );
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
            .Notify(
                TestStrings.Instance.GetString("About_UpdateDownloadCancelled"),
                null,
                NotificationSeverity.Informational
            );
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
        AppUpdater updater = new(client, TestStrings.Instance);
        return (
            new AboutViewModel(updater, launcher, notifications, appInfo, TestStrings.Instance),
            client,
            launcher,
            notifications,
            appInfo
        );
    }
}
