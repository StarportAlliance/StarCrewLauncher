using NSubstitute;
using StarCrew.Launcher.Models;
using StarCrew.Launcher.Services;
using StarCrew.Launcher.ViewModels;

namespace StarCrew.Launcher.Tests.ViewModels;

/// <summary>HomeViewModel 的单元测试：启动、回退选文件与提示全部走替身。</summary>
public sealed class HomeViewModelTests
{
    [Fact]
    public void Ctor_WithNullLauncher_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new HomeViewModel(
                null!,
                Substitute.For<IFilePickerService>(),
                Substitute.For<INotificationService>()
            )
        );
    }

    [Fact]
    public void Ctor_WithNullPicker_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new HomeViewModel(
                Substitute.For<IGameLauncher>(),
                null!,
                Substitute.For<INotificationService>()
            )
        );
    }

    [Fact]
    public void Ctor_WithNullNotifications_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new HomeViewModel(
                Substitute.For<IGameLauncher>(),
                Substitute.For<IFilePickerService>(),
                null!
            )
        );
    }

    [Fact]
    public void LaunchButtonText_DefaultsToStartText()
    {
        (HomeViewModel vm, _, _, _) = Create();

        Assert.Equal("启动游戏", vm.LaunchButtonText);
    }

    [Fact]
    public void LaunchButtonText_WhenLaunching_ShowsProgressText()
    {
        (HomeViewModel vm, _, _, _) = Create();
        List<string?> changed = [];
        vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        vm.IsLaunching = true;

        Assert.Equal("正在启动...", vm.LaunchButtonText);
        Assert.Contains(nameof(HomeViewModel.LaunchButtonText), changed);
    }

    [Fact]
    public async Task LaunchCommand_WhenSucceeded_NotifiesSuccessAndSkipsPicker()
    {
        (
            HomeViewModel vm,
            IGameLauncher launcher,
            IFilePickerService picker,
            INotificationService notifications
        ) = Create();
        launcher
            .LaunchAsync(Arg.Any<CancellationToken>())
            .Returns(new LaunchResult(true, LaunchMethod.SteamProtocol, "已通过 Steam 发起启动。"));

        await vm.LaunchCommand.ExecuteAsync(null);

        notifications
            .Received(1)
            .Notify("启动成功", "已通过 Steam 发起启动。", NotificationSeverity.Success);
        await picker.DidNotReceiveWithAnyArgs().PickGameExeAsync();
    }

    [Fact]
    public async Task LaunchCommand_WhenFailedAndPickCanceled_NotifiesCanceled()
    {
        (
            HomeViewModel vm,
            IGameLauncher launcher,
            IFilePickerService picker,
            INotificationService notifications
        ) = Create();
        launcher
            .LaunchAsync(Arg.Any<CancellationToken>())
            .Returns(new LaunchResult(false, LaunchMethod.None, "找不到游戏。"));
        picker.PickGameExeAsync().Returns((string?)null);

        await vm.LaunchCommand.ExecuteAsync(null);

        notifications
            .Received(1)
            .Notify(
                "已取消",
                "已取消选择，点击启动游戏可重试。",
                NotificationSeverity.Informational
            );
    }

    [Fact]
    public async Task LaunchCommand_WhenManualPickSucceeds_NotifiesSuccess()
    {
        (
            HomeViewModel vm,
            IGameLauncher launcher,
            IFilePickerService picker,
            INotificationService notifications
        ) = Create();
        launcher
            .LaunchAsync(Arg.Any<CancellationToken>())
            .Returns(new LaunchResult(false, LaunchMethod.None, "找不到游戏。"));
        picker.PickGameExeAsync().Returns(Path.Combine("C:", "Games", "Among Us.exe"));
        string? outError = null;
        launcher.TryLaunchExe(Arg.Any<string>(), out outError).Returns(true);

        await vm.LaunchCommand.ExecuteAsync(null);

        notifications
            .Received(1)
            .Notify("启动成功", "已启动手动选择的游戏。", NotificationSeverity.Success);
    }

    [Fact]
    public async Task LaunchCommand_WhenManualPickFails_NotifiesError()
    {
        (
            HomeViewModel vm,
            IGameLauncher launcher,
            IFilePickerService picker,
            INotificationService notifications
        ) = Create();
        launcher
            .LaunchAsync(Arg.Any<CancellationToken>())
            .Returns(new LaunchResult(false, LaunchMethod.None, "找不到游戏。"));
        picker.PickGameExeAsync().Returns(Path.Combine("C:", "Games", "Among Us.exe"));
        string? outError = null;
        launcher
            .TryLaunchExe(Arg.Any<string>(), out outError)
            .Returns(call =>
            {
                call[1] = "boom";
                return false;
            });

        await vm.LaunchCommand.ExecuteAsync(null);

        notifications
            .Received(1)
            .Notify("启动失败", "启动所选文件时发生错误：boom", NotificationSeverity.Error);
    }

    private static (
        HomeViewModel Vm,
        IGameLauncher Launcher,
        IFilePickerService Picker,
        INotificationService Notifications
    ) Create()
    {
        IGameLauncher launcher = Substitute.For<IGameLauncher>();
        IFilePickerService picker = Substitute.For<IFilePickerService>();
        INotificationService notifications = Substitute.For<INotificationService>();
        return (
            new HomeViewModel(launcher, picker, notifications),
            launcher,
            picker,
            notifications
        );
    }
}
