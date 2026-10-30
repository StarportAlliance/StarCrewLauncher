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
        IVersionCacheSettings? cache = null,
        IGameVersionCatalog? catalog = null,
        INotificationService? notifications = null,
        IAppRestarter? restarter = null,
        IExternalLauncher? external = null
    ) =>
        new(
            logging ?? Substitute.For<ILoggingSettings>(),
            cache ?? Substitute.For<IVersionCacheSettings>(),
            catalog ?? Substitute.For<IGameVersionCatalog>(),
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

    [Theory]
    [InlineData(0, 0)]
    [InlineData(-3, 0)]
    [InlineData(0.4, 0)]
    [InlineData(48.6, 49)]
    [InlineData(999, 720)]
    public void VersionCacheHours_RoundsAndClamps(double input, int expected)
    {
        IVersionCacheSettings settings = Substitute.For<IVersionCacheSettings>();
        settings.MaxAgeHours.Returns(24);
        AdvancedViewModel vm = Create(cache: settings);

        vm.VersionCacheHours = input;

        settings.Received(1).MaxAgeHours = expected;
    }

    [Fact]
    public void VersionCacheHours_NaN_Ignores()
    {
        IVersionCacheSettings settings = Substitute.For<IVersionCacheSettings>();
        AdvancedViewModel vm = Create(cache: settings);

        vm.VersionCacheHours = double.NaN;

        settings.DidNotReceive().MaxAgeHours = Arg.Any<int>();
    }

    [Fact]
    public void VersionCacheHours_Zero_ClearsCache()
    {
        IVersionCacheSettings settings = Substitute.For<IVersionCacheSettings>();
        settings.MaxAgeHours.Returns(24);
        IGameVersionCatalog catalog = Substitute.For<IGameVersionCatalog>();
        AdvancedViewModel vm = Create(cache: settings, catalog: catalog);

        vm.VersionCacheHours = 0;

        catalog.Received(1).ClearCache();
    }

    [Fact]
    public void VersionCacheHours_NonZero_KeepsCache()
    {
        IVersionCacheSettings settings = Substitute.For<IVersionCacheSettings>();
        settings.MaxAgeHours.Returns(24);
        IGameVersionCatalog catalog = Substitute.For<IGameVersionCatalog>();
        AdvancedViewModel vm = Create(cache: settings, catalog: catalog);

        vm.VersionCacheHours = 48;

        catalog.DidNotReceive().ClearCache();
    }

    [Fact]
    public void VersionCacheHours_DoesNotNotifyRestart()
    {
        IVersionCacheSettings settings = Substitute.For<IVersionCacheSettings>();
        settings.MaxAgeHours.Returns(24);
        INotificationService notifications = Substitute.For<INotificationService>();
        AdvancedViewModel vm = Create(cache: settings, notifications: notifications);

        vm.VersionCacheHours = 48;

        settings.Received(1).MaxAgeHours = 48;
        notifications.DidNotReceiveWithAnyArgs().Notify(default!, default, default);
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
