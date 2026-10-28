using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Serilog.Events;
using StarCrew.Launcher.Models;
using StarCrew.Launcher.Services;
using StarCrew.Launcher.ViewModels;

namespace StarCrew.Launcher.Tests.ViewModels;

public sealed class AppearanceViewModelTests
{
    private static AppearanceViewModel Create(
        IThemeSettings? theme = null,
        ILoggingSettings? logging = null,
        INotificationService? notifications = null,
        IAppRestarter? restarter = null,
        IExternalLauncher? external = null
    ) =>
        new(
            theme ?? Substitute.For<IThemeSettings>(),
            logging ?? Substitute.For<ILoggingSettings>(),
            TestStrings.Instance,
            notifications ?? Substitute.For<INotificationService>(),
            restarter ?? Substitute.For<IAppRestarter>(),
            external ?? Substitute.For<IExternalLauncher>(),
            NullLogger<AppearanceViewModel>.Instance
        );

    [Fact]
    public void Ctor_WithNullThemeSettings_ThrowsArgumentNullException() =>
        Assert.Throws<ArgumentNullException>(() =>
            new AppearanceViewModel(
                null!,
                Substitute.For<ILoggingSettings>(),
                TestStrings.Instance,
                Substitute.For<INotificationService>(),
                Substitute.For<IAppRestarter>(),
                Substitute.For<IExternalLauncher>(),
                NullLogger<AppearanceViewModel>.Instance
            )
        );

    [Theory]
    [InlineData("System", 0)]
    [InlineData("Light", 1)]
    [InlineData("Dark", 2)]
    public void SelectedThemeIndex_ReadsFromSettings(string themeName, int expected)
    {
        IThemeSettings settings = Substitute.For<IThemeSettings>();
        settings.Theme.Returns(Enum.Parse<AppTheme>(themeName));

        AppearanceViewModel vm = Create(theme: settings);

        Assert.Equal(expected, vm.SelectedThemeIndex);
    }

    [Theory]
    [InlineData(0, "System")]
    [InlineData(1, "Light")]
    [InlineData(2, "Dark")]
    [InlineData(99, "System")]
    public void SelectedThemeIndex_WritesToSettings(int index, string expectedName)
    {
        IThemeSettings settings = Substitute.For<IThemeSettings>();
        AppearanceViewModel vm = Create(theme: settings);

        vm.SelectedThemeIndex = index;

        settings.Received(1).Theme = Enum.Parse<AppTheme>(expectedName);
    }

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

        AppearanceViewModel vm = Create(logging: settings);

        Assert.Equal(expected, vm.SelectedLevelIndex);
    }

    [Fact]
    public void SelectedLevelIndex_WritesToSettings()
    {
        ILoggingSettings settings = Substitute.For<ILoggingSettings>();
        settings.Level.Returns(LogEventLevel.Information);
        AppearanceViewModel vm = Create(logging: settings);

        vm.SelectedLevelIndex = 4;

        settings.Received(1).Level = LogEventLevel.Error;
    }

    [Fact]
    public void SelectedLevelIndex_OutOfRange_Ignores()
    {
        ILoggingSettings settings = Substitute.For<ILoggingSettings>();
        AppearanceViewModel vm = Create(logging: settings);

        vm.SelectedLevelIndex = 99;

        settings.DidNotReceive().Level = Arg.Any<LogEventLevel>();
    }

    [Fact]
    public void LoggingEnabled_WritesToSettings()
    {
        ILoggingSettings settings = Substitute.For<ILoggingSettings>();
        AppearanceViewModel vm = Create(logging: settings);

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
        AppearanceViewModel vm = Create(logging: settings);

        vm.RetentionDays = input;

        settings.Received(1).RetentionDays = expected;
    }

    [Fact]
    public void RetentionDays_NaN_Ignores()
    {
        ILoggingSettings settings = Substitute.For<ILoggingSettings>();
        AppearanceViewModel vm = Create(logging: settings);

        vm.RetentionDays = double.NaN;

        settings.DidNotReceive().RetentionDays = Arg.Any<int>();
    }

    [Fact]
    public void ChangingSettings_NotifiesRestartOnlyOnce()
    {
        ILoggingSettings settings = Substitute.For<ILoggingSettings>();
        INotificationService notifications = Substitute.For<INotificationService>();
        IAppRestarter restarter = Substitute.For<IAppRestarter>();
        AppearanceViewModel vm = Create(
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
        AppearanceViewModel vm = Create(logging: settings, notifications: notifications);

        vm.LoggingEnabled = true;

        notifications.DidNotReceiveWithAnyArgs().Notify(default!, default, default);
    }

    [Fact]
    public async Task OpenLogFolderCommand_OpensLogDirectory()
    {
        IExternalLauncher external = Substitute.For<IExternalLauncher>();
        AppearanceViewModel vm = Create(external: external);

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
        AppearanceViewModel vm = Create(notifications: notifications, external: external);

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
