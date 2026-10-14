using NSubstitute;
using StarCrew.Launcher.Models;
using StarCrew.Launcher.Services;
using StarCrew.Launcher.ViewModels;

namespace StarCrew.Launcher.Tests.ViewModels;

/// <summary>InstancesViewModel 的单元测试：列表装载、删除与重命名的业务决策。</summary>
public sealed class InstancesViewModelTests : IDisposable
{
    private readonly string _root;

    public InstancesViewModelTests()
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

    [Fact]
    public void Ctor_WithNullStore_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new InstancesViewModel(
                null!,
                Substitute.For<IInstanceLauncher>(),
                Substitute.For<IExternalLauncher>(),
                Substitute.For<INotificationService>(),
                Substitute.For<INavigationService>()
            )
        );
    }

    [Fact]
    public void Items_LoadedFromStore()
    {
        GameInstance a = CreateInstance("A");
        GameInstance b = CreateInstance("B");
        FakeStore store = CreateSubstitute(new LauncherState(1, null, null, [a, b]));
        InstancesViewModel vm = new(
            store,
            Substitute.For<IInstanceLauncher>(),
            Substitute.For<IExternalLauncher>(),
            Substitute.For<INotificationService>(),
            Substitute.For<INavigationService>()
        );

        Assert.Equal(2, vm.Items.Count);
        Assert.Equal(a, vm.Items[0].Instance);
        Assert.Equal(b, vm.Items[1].Instance);
        Assert.True(vm.HasInstances);
    }

    [Fact]
    public void HasInstances_WithEmptyList_False()
    {
        FakeStore store = CreateSubstitute(LauncherState.Empty);
        InstancesViewModel vm = new(
            store,
            Substitute.For<IInstanceLauncher>(),
            Substitute.For<IExternalLauncher>(),
            Substitute.For<INotificationService>(),
            Substitute.For<INavigationService>()
        );

        Assert.False(vm.HasInstances);
        Assert.Empty(vm.Items);
    }

    [Fact]
    public void ItemEvent_Delete_ReRaisedAndForwardingWorks()
    {
        GameInstance a = CreateInstance("A");
        FakeStore store = CreateSubstitute(new LauncherState(1, null, null, [a]));
        InstancesViewModel vm = new(
            store,
            Substitute.For<IInstanceLauncher>(),
            Substitute.For<IExternalLauncher>(),
            Substitute.For<INotificationService>(),
            Substitute.For<INavigationService>()
        );
        GameInstance? raised = null;
        vm.DeleteRequested += (_, inst) => raised = inst;

        vm.Items[0].RequestDeleteCommand.Execute(null);

        Assert.Equal(a, raised);
    }

    [Fact]
    public void ConfirmDelete_WithDeleteFilesFalse_RemovesFromStoreButKeepsDirectory()
    {
        string dir = Path.Combine(_root, "instanceA");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "file.txt"), "x");
        GameInstance a = CreateInstance("A", dir);
        FakeStore store = CreateSubstitute(new LauncherState(1, a.Id, null, [a]));
        INotificationService notifications = Substitute.For<INotificationService>();
        InstancesViewModel vm = new(
            store,
            Substitute.For<IInstanceLauncher>(),
            Substitute.For<IExternalLauncher>(),
            notifications,
            Substitute.For<INavigationService>()
        );

        vm.ConfirmDelete(a, deleteFiles: false);

        Assert.True(Directory.Exists(dir), "目录应该保留");
        Assert.Empty(vm.Items);
        Assert.Empty(store.Current.Instances);
    }

    [Fact]
    public void ConfirmDelete_WithDeleteFilesTrue_DeletesDirectoryAndStore()
    {
        string dir = Path.Combine(_root, "instanceDelete");
        Directory.CreateDirectory(dir);
        GameInstance a = CreateInstance("A", dir);
        FakeStore store = CreateSubstitute(new LauncherState(1, a.Id, null, [a]));
        InstancesViewModel vm = new(
            store,
            Substitute.For<IInstanceLauncher>(),
            Substitute.For<IExternalLauncher>(),
            Substitute.For<INotificationService>(),
            Substitute.For<INavigationService>()
        );

        vm.ConfirmDelete(a, deleteFiles: true);

        Assert.False(Directory.Exists(dir), "目录应该被删除");
        Assert.Empty(vm.Items);
    }

    [Fact]
    public void ConfirmRename_WithValidName_UpdatesStoreAndItems()
    {
        GameInstance a = CreateInstance("A");
        FakeStore store = CreateSubstitute(new LauncherState(1, null, null, [a]));
        InstancesViewModel vm = new(
            store,
            Substitute.For<IInstanceLauncher>(),
            Substitute.For<IExternalLauncher>(),
            Substitute.For<INotificationService>(),
            Substitute.For<INavigationService>()
        );

        vm.ConfirmRename(a, "  新名字  ");

        Assert.Single(vm.Items);
        Assert.Equal("新名字", vm.Items[0].Name);
        Assert.Equal("新名字", store.Current.Instances[0].Name);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ConfirmRename_WithBlankName_NotifiesErrorAndKeepsName(string blankName)
    {
        GameInstance a = CreateInstance("A");
        FakeStore store = CreateSubstitute(new LauncherState(1, null, null, [a]));
        INotificationService notifications = Substitute.For<INotificationService>();
        InstancesViewModel vm = new(
            store,
            Substitute.For<IInstanceLauncher>(),
            Substitute.For<IExternalLauncher>(),
            notifications,
            Substitute.For<INavigationService>()
        );

        vm.ConfirmRename(a, blankName);

        notifications
            .Received(1)
            .Notify("重命名失败：实例名不能为空。", null, NotificationSeverity.Error);
        Assert.Equal("A", vm.Items[0].Name);
    }

    [Fact]
    public void ConfirmRename_WithDuplicateName_NotifiesError()
    {
        GameInstance a = CreateInstance("A");
        GameInstance b = CreateInstance("B");
        FakeStore store = CreateSubstitute(new LauncherState(1, null, null, [a, b]));
        INotificationService notifications = Substitute.For<INotificationService>();
        InstancesViewModel vm = new(
            store,
            Substitute.For<IInstanceLauncher>(),
            Substitute.For<IExternalLauncher>(),
            notifications,
            Substitute.For<INavigationService>()
        );

        vm.ConfirmRename(a, "b");

        notifications
            .Received(1)
            .Notify("重命名失败：已存在同名的实例。", null, NotificationSeverity.Error);
    }

    [Fact]
    public void GoDownloadCommand_NavigatesToDownload()
    {
        GameInstance a = CreateInstance("A");
        FakeStore store = CreateSubstitute(new LauncherState(1, null, null, [a]));
        INavigationService navigation = Substitute.For<INavigationService>();
        InstancesViewModel vm = new(
            store,
            Substitute.For<IInstanceLauncher>(),
            Substitute.For<IExternalLauncher>(),
            Substitute.For<INotificationService>(),
            navigation
        );

        vm.GoDownloadCommand.Execute(null);

        navigation.Received(1).NavigateTo(NavigationTarget.Download);
    }

    private static FakeStore CreateSubstitute(LauncherState state) => new(state);

    private sealed class FakeStore : IInstanceStore
    {
        private LauncherState _current;

        public FakeStore(LauncherState state)
        {
            _current = state;
        }

        public LauncherState Current => _current;

        public LauncherState Load() => _current;

        public void Save(LauncherState state) => _current = state;
    }

    private GameInstance CreateInstance(string name, string? dir = null)
    {
        return new GameInstance(
            Guid.NewGuid(),
            name,
            dir ?? Path.Combine(_root, "instance-" + name),
            945360,
            945361,
            990035194164819049L,
            "2026.9.1",
            InstanceState.Ready,
            DateTimeOffset.UtcNow,
            null
        );
    }
}
