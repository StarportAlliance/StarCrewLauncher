using NSubstitute;
using StarCrew.Launcher.Models;
using StarCrew.Launcher.Services;
using StarCrew.Launcher.ViewModels;

namespace StarCrew.Launcher.Tests.ViewModels;

public sealed class InstanceItemViewModelTests
{
    [Fact]
    public void Ctor_WithNullInstance_ThrowsArgumentNullException() =>
        Assert.Throws<ArgumentNullException>(() =>
            new InstanceItemViewModel(
                null!,
                Substitute.For<IInstanceLauncher>(),
                Substitute.For<IExternalLauncher>(),
                Substitute.For<INotificationService>(),
                TestStrings.Instance
            )
        );

    [Fact]
    public void Ctor_WithNullLauncher_ThrowsArgumentNullException() =>
        Assert.Throws<ArgumentNullException>(() =>
            new InstanceItemViewModel(
                CreateInstance(),
                null!,
                Substitute.For<IExternalLauncher>(),
                Substitute.For<INotificationService>(),
                TestStrings.Instance
            )
        );

    [Fact]
    public void Ctor_WithNullExternalLauncher_ThrowsArgumentNullException() =>
        Assert.Throws<ArgumentNullException>(() =>
            new InstanceItemViewModel(
                CreateInstance(),
                Substitute.For<IInstanceLauncher>(),
                null!,
                Substitute.For<INotificationService>(),
                TestStrings.Instance
            )
        );

    [Fact]
    public void Ctor_WithNullNotifications_ThrowsArgumentNullException() =>
        Assert.Throws<ArgumentNullException>(() =>
            new InstanceItemViewModel(
                CreateInstance(),
                Substitute.For<IInstanceLauncher>(),
                Substitute.For<IExternalLauncher>(),
                null!,
                TestStrings.Instance
            )
        );

    [Fact]
    public void SelectedThemeIndexSummary_ForIncompleteInstance_ShowsIncomplete()
    {
        InstanceItemViewModel vm = new(
            CreateInstance(InstanceState.Incomplete),
            Substitute.For<IInstanceLauncher>(),
            Substitute.For<IExternalLauncher>(),
            Substitute.For<INotificationService>(),
            TestStrings.Instance
        );

        Assert.False(vm.CanLaunch);
        Assert.Contains("未完成", vm.Summary, StringComparison.Ordinal);
    }

    [Fact]
    public async Task LaunchCommand_WhenIncomplete_NotifiesWarning()
    {
        INotificationService notifications = Substitute.For<INotificationService>();
        IInstanceLauncher launcher = Substitute.For<IInstanceLauncher>();
        InstanceItemViewModel vm = new(
            CreateInstance(InstanceState.Incomplete),
            launcher,
            Substitute.For<IExternalLauncher>(),
            notifications,
            TestStrings.Instance
        );

        await vm.LaunchCommand.ExecuteAsync(null);

        notifications
            .Received(1)
            .Notify("无法启动：该实例尚未就绪，请先完成安装。", null, NotificationSeverity.Warning);
        await launcher.DidNotReceiveWithAnyArgs().LaunchAsync(default!, default);
    }

    [Fact]
    public async Task LaunchCommand_WhenReady_LaunchesAndNotifiesSuccess()
    {
        INotificationService notifications = Substitute.For<INotificationService>();
        IInstanceLauncher launcher = Substitute.For<IInstanceLauncher>();
        launcher
            .LaunchAsync(Arg.Any<GameInstance>(), Arg.Any<CancellationToken>())
            .Returns(new LaunchOutcome(true, "已启动。"));
        InstanceItemViewModel vm = new(
            CreateInstance(),
            launcher,
            Substitute.For<IExternalLauncher>(),
            notifications,
            TestStrings.Instance
        );

        await vm.LaunchCommand.ExecuteAsync(null);

        notifications.Received(1).Notify("已启动。", null, NotificationSeverity.Success);
    }

    [Fact]
    public async Task LaunchCommand_WhenLauncherFails_NotifiesError()
    {
        INotificationService notifications = Substitute.For<INotificationService>();
        IInstanceLauncher launcher = Substitute.For<IInstanceLauncher>();
        launcher
            .LaunchAsync(Arg.Any<GameInstance>(), Arg.Any<CancellationToken>())
            .Returns(new LaunchOutcome(false, "boom"));
        InstanceItemViewModel vm = new(
            CreateInstance(),
            launcher,
            Substitute.For<IExternalLauncher>(),
            notifications,
            TestStrings.Instance
        );

        await vm.LaunchCommand.ExecuteAsync(null);

        notifications.Received(1).Notify("boom", null, NotificationSeverity.Error);
    }

    [Fact]
    public async Task OpenFolderCommand_WhenFolderMissing_NotifiesError()
    {
        INotificationService notifications = Substitute.For<INotificationService>();
        IExternalLauncher externalLauncher = Substitute.For<IExternalLauncher>();
        externalLauncher
            .OpenFolderAsync(Arg.Any<string>())
            .Returns(Task.FromException(new DirectoryNotFoundException("not here")));
        InstanceItemViewModel vm = new(
            CreateInstance(),
            Substitute.For<IInstanceLauncher>(),
            externalLauncher,
            notifications,
            TestStrings.Instance
        );

        await vm.OpenFolderCommand.ExecuteAsync(null);

        notifications.Received(1).Notify("打开失败：not here", null, NotificationSeverity.Error);
    }

    [Fact]
    public void RequestRenameCommand_RaisesRenameRequested()
    {
        GameInstance instance = CreateInstance();
        GameInstance? raised = null;
        InstanceItemViewModel vm = new(
            instance,
            Substitute.For<IInstanceLauncher>(),
            Substitute.For<IExternalLauncher>(),
            Substitute.For<INotificationService>(),
            TestStrings.Instance
        );
        vm.RenameRequested += (_, inst) => raised = inst;

        vm.RequestRenameCommand.Execute(null);

        Assert.Equal(instance, raised);
    }

    [Fact]
    public void RequestDeleteCommand_RaisesDeleteRequested()
    {
        GameInstance instance = CreateInstance();
        GameInstance? raised = null;
        InstanceItemViewModel vm = new(
            instance,
            Substitute.For<IInstanceLauncher>(),
            Substitute.For<IExternalLauncher>(),
            Substitute.For<INotificationService>(),
            TestStrings.Instance
        );
        vm.DeleteRequested += (_, inst) => raised = inst;

        vm.RequestDeleteCommand.Execute(null);

        Assert.Equal(instance, raised);
    }

    [Fact]
    public void RequestDetailsCommand_RaisesDetailsRequested()
    {
        GameInstance instance = CreateInstance();
        GameInstance? raised = null;
        InstanceItemViewModel vm = new(
            instance,
            Substitute.For<IInstanceLauncher>(),
            Substitute.For<IExternalLauncher>(),
            Substitute.For<INotificationService>(),
            TestStrings.Instance
        );
        vm.DetailsRequested += (_, inst) => raised = inst;

        vm.RequestDetailsCommand.Execute(null);

        Assert.Equal(instance, raised);
    }

    private static GameInstance CreateInstance(InstanceState state = InstanceState.Ready) =>
        new GameInstance(
            Guid.NewGuid(),
            "A",
            Path.Combine(Path.GetTempPath(), "instances", "A"),
            945360,
            945361,
            990035194164819049L,
            "2026.9.1",
            state,
            DateTimeOffset.UtcNow,
            null
        );
}
