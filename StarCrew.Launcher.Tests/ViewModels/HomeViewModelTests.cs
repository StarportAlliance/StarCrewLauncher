using NSubstitute;
using StarCrew.Launcher.Models;
using StarCrew.Launcher.Services;
using StarCrew.Launcher.ViewModels;

namespace StarCrew.Launcher.Tests.ViewModels;

public sealed class HomeViewModelTests
{
    [Fact]
    public void Ctor_WithNullStore_ThrowsArgumentNullException() =>
        Assert.Throws<ArgumentNullException>(() =>
            new HomeViewModel(
                null!,
                Substitute.For<IInstanceLauncher>(),
                Substitute.For<INavigationService>(),
                Substitute.For<INotificationService>(),
                TestStrings.Instance
            )
        );

    [Fact]
    public void Ctor_WithNullLauncher_ThrowsArgumentNullException() =>
        Assert.Throws<ArgumentNullException>(() =>
            new HomeViewModel(
                Substitute.For<IInstanceStore>(),
                null!,
                Substitute.For<INavigationService>(),
                Substitute.For<INotificationService>(),
                TestStrings.Instance
            )
        );

    [Fact]
    public void Ctor_WithNullNavigation_ThrowsArgumentNullException() =>
        Assert.Throws<ArgumentNullException>(() =>
            new HomeViewModel(
                Substitute.For<IInstanceStore>(),
                Substitute.For<IInstanceLauncher>(),
                null!,
                Substitute.For<INotificationService>(),
                TestStrings.Instance
            )
        );

    [Fact]
    public void Ctor_WithNullNotifications_ThrowsArgumentNullException() =>
        Assert.Throws<ArgumentNullException>(() =>
            new HomeViewModel(
                Substitute.For<IInstanceStore>(),
                Substitute.For<IInstanceLauncher>(),
                Substitute.For<INavigationService>(),
                null!,
                TestStrings.Instance
            )
        );

    [Fact]
    public void Ctor_WithEmptyStore_HasNoInstances()
    {
        (HomeViewModel vm, _, _, _, _) = Create();

        Assert.Empty(vm.Instances);
        Assert.False(vm.HasInstances);
        Assert.Equal("下载游戏", vm.LaunchButtonText);
        Assert.Empty(vm.CurrentInstanceName);
    }

    [Fact]
    public void SelectedInstance_WhenNotSetAndInstancesExist_DefaultsToLastUsed()
    {
        Guid wanted = Guid.NewGuid();
        GameInstance a = CreateInstance("A");
        GameInstance b = CreateInstance("B", id: wanted);
        (HomeViewModel vm, _, _, _, _) = Create(new LauncherState(1, wanted, null, [a, b]));

        Assert.Equal(b, vm.SelectedInstance);
        Assert.Equal("B", vm.CurrentInstanceName);
        Assert.Equal("启动游戏", vm.LaunchButtonText);
    }

    [Fact]
    public void LaunchAsync_WithoutInstances_NavigatesToDownload()
    {
        (HomeViewModel vm, _, _, _, INotificationService notifications) = Create();

        vm.LaunchCommand.Execute(null);

        notifications.DidNotReceiveWithAnyArgs().Notify(default!, default!, default);
    }

    [Fact]
    public async Task LaunchAsync_WithInstance_LaunchesAndNotifiesSuccess()
    {
        GameInstance a = CreateInstance("A");
        IInstanceStore store = CreateSubstituteWithState(new LauncherState(1, a.Id, null, [a]));
        IInstanceLauncher launcher = Substitute.For<IInstanceLauncher>();
        launcher
            .LaunchAsync(Arg.Any<GameInstance>(), Arg.Any<CancellationToken>())
            .Returns(new LaunchOutcome(true, "已启动。"));
        INavigationService navigation = Substitute.For<INavigationService>();
        INotificationService notifications = Substitute.For<INotificationService>();
        HomeViewModel vm = new(store, launcher, navigation, notifications, TestStrings.Instance);

        await vm.LaunchCommand.ExecuteAsync(null);

        notifications.Received(1).Notify("已启动。", null, NotificationSeverity.Success);
        store.Received(1).Save(Arg.Is<LauncherState>(s => s!.LastInstanceId == a.Id));
    }

    [Fact]
    public async Task LaunchAsync_WithFailureOutcome_NotifiesError()
    {
        GameInstance a = CreateInstance("A");
        IInstanceStore store = CreateSubstituteWithState(new LauncherState(1, null, null, [a]));
        IInstanceLauncher launcher = Substitute.For<IInstanceLauncher>();
        launcher
            .LaunchAsync(Arg.Any<GameInstance>(), Arg.Any<CancellationToken>())
            .Returns(new LaunchOutcome(false, "boom"));
        _ = new HomeViewModel(
            store,
            launcher,
            Substitute.For<INavigationService>(),
            Substitute.For<INotificationService>(),
            TestStrings.Instance
        );
        INotificationService notifications = Substitute.For<INotificationService>();
        HomeViewModel vm2 = new(
            store,
            launcher,
            Substitute.For<INavigationService>(),
            notifications,
            TestStrings.Instance
        );

        await vm2.LaunchCommand.ExecuteAsync(null);

        notifications.Received(1).Notify("boom", null, NotificationSeverity.Error);
    }

    [Fact]
    public void OnSelectedInstanceChanged_PersistsLastId()
    {
        GameInstance a = CreateInstance("A");
        GameInstance b = CreateInstance("B");
        IInstanceStore store = CreateSubstituteWithState(new LauncherState(1, null, null, [a, b]));
        HomeViewModel vm = new(
            store,
            Substitute.For<IInstanceLauncher>(),
            Substitute.For<INavigationService>(),
            Substitute.For<INotificationService>(),
            TestStrings.Instance
        )
        {
            SelectedInstance = b,
        };

        store.Received(1).Save(Arg.Is<LauncherState>(s => s!.LastInstanceId == b.Id));
    }

    private static (
        HomeViewModel Vm,
        IInstanceStore Store,
        IInstanceLauncher Launcher,
        INavigationService Navigation,
        INotificationService Notifications
    ) Create(LauncherState? state = null)
    {
        IInstanceStore store = CreateSubstituteWithState(state);
        IInstanceLauncher launcher = Substitute.For<IInstanceLauncher>();
        INavigationService navigation = Substitute.For<INavigationService>();
        INotificationService notifications = Substitute.For<INotificationService>();
        HomeViewModel vm = new(store, launcher, navigation, notifications, TestStrings.Instance);
        return (vm, store, launcher, navigation, notifications);
    }

    private static IInstanceStore CreateSubstituteWithState(LauncherState? state = null)
    {
        IInstanceStore store = Substitute.For<IInstanceStore>();
        store.Load().Returns(state ?? LauncherState.Empty);
        return store;
    }

    private static GameInstance CreateInstance(string name, Guid? id = null) =>
        new(
            id ?? Guid.NewGuid(),
            name,
            Path.Combine(Path.GetTempPath(), "instances", name),
            945360,
            945361,
            990035194164819049L,
            "2026.9.1",
            InstanceState.Ready,
            DateTimeOffset.UtcNow,
            null
        );
}
