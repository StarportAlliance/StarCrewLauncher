using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Serilog.Events;
using StarCrew.Launcher.Models;
using StarCrew.Launcher.Services;

namespace StarCrew.Launcher.ViewModels;

internal sealed partial class AppearanceViewModel
{
    private static readonly LogEventLevel[] Levels =
    [
        LogEventLevel.Verbose,
        LogEventLevel.Debug,
        LogEventLevel.Information,
        LogEventLevel.Warning,
        LogEventLevel.Error,
        LogEventLevel.Fatal,
    ];

    private readonly IThemeSettings _themeSettings;
    private readonly ILoggingSettings _loggingSettings;
    private readonly IAppStrings _strings;
    private readonly INotificationService _notifications;
    private readonly IAppRestarter _restarter;
    private readonly IExternalLauncher _external;
    private readonly ILogger<AppearanceViewModel> _logger;
    private bool _restartNotified;

    public AppearanceViewModel(
        IThemeSettings themeSettings,
        ILoggingSettings loggingSettings,
        IAppStrings strings,
        INotificationService notifications,
        IAppRestarter restarter,
        IExternalLauncher external,
        ILogger<AppearanceViewModel> logger
    )
    {
        ArgumentNullException.ThrowIfNull(themeSettings);
        ArgumentNullException.ThrowIfNull(loggingSettings);
        ArgumentNullException.ThrowIfNull(strings);
        ArgumentNullException.ThrowIfNull(notifications);
        ArgumentNullException.ThrowIfNull(restarter);
        ArgumentNullException.ThrowIfNull(external);
        ArgumentNullException.ThrowIfNull(logger);
        _themeSettings = themeSettings;
        _loggingSettings = loggingSettings;
        _strings = strings;
        _notifications = notifications;
        _restarter = restarter;
        _external = external;
        _logger = logger;
    }

    /// <summary>下拉框下标。</summary>
    public int SelectedThemeIndex
    {
        get =>
            _themeSettings.Theme switch
            {
                AppTheme.Light => 1,
                AppTheme.Dark => 2,
                _ => 0,
            };
        set =>
            _themeSettings.Theme = value switch
            {
                1 => AppTheme.Light,
                2 => AppTheme.Dark,
                _ => AppTheme.System,
            };
    }

    public bool LoggingEnabled
    {
        get => _loggingSettings.Enabled;
        set
        {
            if (_loggingSettings.Enabled == value)
            {
                return;
            }

            _loggingSettings.Enabled = value;
            MarkRestartNeeded();
        }
    }

    public int SelectedLevelIndex
    {
        get
        {
            int index = Array.IndexOf(Levels, _loggingSettings.Level);
            return index < 0 ? 0 : index;
        }
        set
        {
            if (value < 0 || value >= Levels.Length || _loggingSettings.Level == Levels[value])
            {
                return;
            }

            _loggingSettings.Level = Levels[value];
            MarkRestartNeeded();
        }
    }

    // 与 NumberBox.Value 同型（double，清空时为 NaN）；落库前取整，设置本身存 int
    public double RetentionDays
    {
        get => _loggingSettings.RetentionDays;
        set
        {
            // NumberBox 清空时给 NaN，视为未提交
            if (!double.IsFinite(value))
            {
                return;
            }

            int days = Math.Max(1, (int)Math.Round(value));
            if (_loggingSettings.RetentionDays == days)
            {
                return;
            }

            _loggingSettings.RetentionDays = days;
            MarkRestartNeeded();
        }
    }

    [RelayCommand]
    private async Task OpenLogFolderAsync()
    {
        try
        {
            await _external.OpenFolderAsync(AppPaths.LogDirectory);
        }
        catch (DirectoryNotFoundException ex)
        {
            _notifications.Notify(
                _strings.GetString("Logging_OpenFailedTitle"),
                ex.Message,
                NotificationSeverity.Error
            );
        }
    }

    private void MarkRestartNeeded()
    {
        _logger.LogInformation(
            "日志设置已更新：启用 {Enabled}，等级 {Level}，保留 {Days} 天",
            _loggingSettings.Enabled,
            _loggingSettings.Level,
            _loggingSettings.RetentionDays
        );

        if (_restartNotified)
        {
            return;
        }

        _restartNotified = true;
        _notifications.Notify(
            _strings.GetString("Logging_RestartTitle"),
            null,
            NotificationSeverity.Informational,
            new NotificationAction(_strings.GetString("Logging_RestartNow"), _restarter.Restart)
        );
    }
}
