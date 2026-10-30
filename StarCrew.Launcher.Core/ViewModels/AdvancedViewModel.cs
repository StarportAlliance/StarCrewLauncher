using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Serilog.Events;
using StarCrew.Launcher.Models;
using StarCrew.Launcher.Services;

namespace StarCrew.Launcher.ViewModels;

internal sealed partial class AdvancedViewModel
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

    private readonly ILoggingSettings _loggingSettings;
    private readonly IVersionCacheSettings _versionCacheSettings;
    private readonly IGameVersionCatalog _catalog;
    private readonly IAppStrings _strings;
    private readonly INotificationService _notifications;
    private readonly IAppRestarter _restarter;
    private readonly IExternalLauncher _external;
    private readonly ILogger<AdvancedViewModel> _logger;
    private bool _restartNotified;

    public AdvancedViewModel(
        ILoggingSettings loggingSettings,
        IVersionCacheSettings versionCacheSettings,
        IGameVersionCatalog catalog,
        IAppStrings strings,
        INotificationService notifications,
        IAppRestarter restarter,
        IExternalLauncher external,
        ILogger<AdvancedViewModel> logger
    )
    {
        ArgumentNullException.ThrowIfNull(loggingSettings);
        ArgumentNullException.ThrowIfNull(versionCacheSettings);
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(strings);
        ArgumentNullException.ThrowIfNull(notifications);
        ArgumentNullException.ThrowIfNull(restarter);
        ArgumentNullException.ThrowIfNull(external);
        ArgumentNullException.ThrowIfNull(logger);
        _loggingSettings = loggingSettings;
        _versionCacheSettings = versionCacheSettings;
        _catalog = catalog;
        _strings = strings;
        _notifications = notifications;
        _restarter = restarter;
        _external = external;
        _logger = logger;
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

    // 与 NumberBox.Value 同型（double，清空时为 NaN）；落库前取整并夹取到 0-720，0 表示禁用缓存
    public double VersionCacheHours
    {
        get => _versionCacheSettings.MaxAgeHours;
        set
        {
            if (!double.IsFinite(value))
            {
                return;
            }

            int hours = Math.Clamp(
                (int)Math.Round(value),
                VersionCacheSettings.MinMaxAgeHours,
                VersionCacheSettings.MaxMaxAgeHours
            );
            if (_versionCacheSettings.MaxAgeHours == hours)
            {
                return;
            }

            _versionCacheSettings.MaxAgeHours = hours;
            if (hours == 0)
            {
                _catalog.ClearCache();
            }
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
