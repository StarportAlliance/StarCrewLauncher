using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Serilog.Events;
using StarCrew.Launcher.Models;
using StarCrew.Launcher.Services;
using StarCrew.Launcher.ViewModels;

namespace StarCrew.Launcher.Tests.ViewModels;

public sealed class AdvancedViewModelTests
{
    private static AdvancedViewModel Create(
        ILoggingSettings? logging = null,
        INotificationService? notifications = null,
        IAppRestarter? restarter = null,
        IExternalLauncher? external = null
    ) =>
        new(
            logging ?? Substitute.For<ILoggingSettings>(),
            TestStrings.Instance,
            notifications ?? Substitute.For<INotificationService>(),
            restarter ?? Substitute.For<IAppRestarter>(),
            external ?? Substitute.For<IExternalLauncher>(),
            NullLogger<AdvancedViewModel>.Instance
        );

    [Theory]
    [InlineData(LogEventLevel.Verbose, 0)]
    [InlineData(LogEventLevel.Debug, 1)]
    [InlineData(LogEventLevel.Information, 2)]
    [InlineData(LogEventLevel.Warning, 3)]
    [InlineData(LogEventLevel.Error, 4)]
    [InlineData(LogEventLevel.Fatal, 5)]
    public void SelectedLevelIndex_ReadsFromSettings(LogEventLevel level, int expected)
    {
        ILoggingSettings settings = Substitute.For<ILoggingSettings>();
        settings.Level.Returns(level);

        AdvancedViewModel vm = Create(logging: settings);

        Assert.Equal(expected, vm.SelectedLevelIndex);
    }

    [Fact]
    public void SelectedLevelIndex_WritesToSettings()
    {
        ILoggingSettings settings = Substitute.For<ILoggingSettings>();
        settings.Level.Returns(LogEventLevel.Information);
        AdvancedViewModel vm = Create(logging: settings);

        vm.SelectedLevelIndex = 4;

        settings.Received(1).Level = LogEventLevel.Error;
    }

    [Fact]
    public void SelectedLevelIndex_OutOfRange_Ignores()
    {
        ILoggingSettings settings = Substitute.For<ILoggingSettings>();
        AdvancedViewModel vm = Create(logging: settings);

        vm.SelectedLevelIndex = 99;

        settings.DidNotReceive().Level = Arg.Any<LogEventLevel>();
    }

    [Fact]
    public void LoggingEnabled_WritesToSettings()
    {
        ILoggingSettings settings = Substitute.For<ILoggingSettings>();
        AdvancedViewModel vm = Create(logging: settings);

        vm.LoggingEnabled = true;

        settings.Received(1).Enabled = true;
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(3.4, 3)]
    [InlineData(30, 30)]
    public void RetentionDays_RoundsAndClampsToAtLeastOne(double input, int expected)
    {
        ILoggingSettings settings = Substitute.For<ILoggingSettings>();
        settings.RetentionDays.Returns(7);
        AdvancedViewModel vm = Create(logging: settings);

        vm.RetentionDays = input;

        settings.Received(1).RetentionDays = expected;
    }

    [Fact]
    public void RetentionDays_NaN_Ignores()
    {
        ILoggingSettings settings = Substitute.For<ILoggingSettings>();
        AdvancedViewModel vm = Create(logging: settings);

        vm.RetentionDays = double.NaN;

        settings.DidNotReceive().RetentionDays = Arg.Any<int>();
    }

    [Fact]
    public void ChangingSettings_NotifiesRestartOnlyOnce()
    {
        ILoggingSettings settings = Substitute.For<ILoggingSettings>();
        INotificationService notifications = Substitute.For<INotificationService>();
        IAppRestarter restarter = Substitute.For<IAppRestarter>();
        AdvancedViewModel vm = Create(
            logging: settings,
            notifications: notifications,
            restarter: restarter
        );

        vm.LoggingEnabled = true;
        vm.SelectedLevelIndex = 4;
        vm.RetentionDays = 3;

        notifications
            .Received(1)
            .Notify(
                TestStrings.Instance.GetString("Logging_RestartTitle"),
                null,
                NotificationSeverity.Informational,
                Arg.Is<NotificationAction?>(action => action != null)
            );
        restarter.DidNotReceive().Restart();

        NotificationAction action = notifications
            .ReceivedCalls()
            .Select(call => call.GetArguments()[3] as NotificationAction)
            .First(a => a != null)!;
        action.Callback();

        restarter.Received(1).Restart();
    }

    [Fact]
    public void SettingSameValue_DoesNotNotify()
    {
        ILoggingSettings settings = Substitute.For<ILoggingSettings>();
        settings.Enabled.Returns(true);
        INotificationService notifications = Substitute.For<INotificationService>();
        AdvancedViewModel vm = Create(logging: settings, notifications: notifications);

        vm.LoggingEnabled = true;

        notifications.DidNotReceiveWithAnyArgs().Notify(default!, default, default);
    }

    [Fact]
    public async Task OpenLogFolderCommand_OpensLogDirectory()
    {
        IExternalLauncher external = Substitute.For<IExternalLauncher>();
        AdvancedViewModel vm = Create(external: external);

        await vm.OpenLogFolderCommand.ExecuteAsync(null);

        await external.Received(1).OpenFolderAsync(AppPaths.LogDirectory);
    }

    [Fact]
    public async Task OpenLogFolderCommand_WhenMissing_NotifiesError()
    {
        IExternalLauncher external = Substitute.For<IExternalLauncher>();
        external
            .OpenFolderAsync(Arg.Any<string>())
            .Returns(Task.FromException(new DirectoryNotFoundException("missing")));
        INotificationService notifications = Substitute.For<INotificationService>();
        AdvancedViewModel vm = Create(notifications: notifications, external: external);

        await vm.OpenLogFolderCommand.ExecuteAsync(null);

        notifications
            .Received(1)
            .Notify(
                TestStrings.Instance.GetString("Logging_OpenFailedTitle"),
                Arg.Any<string?>(),
                NotificationSeverity.Error
            );
    }
}
