using Serilog.Events;

namespace StarCrew.Launcher.Services;

internal sealed class LoggingSettings : ILoggingSettings
{
    public const string EnabledKey = "LoggingEnabled";
    public const string LevelKey = "LoggingLevel";
    public const string RetentionDaysKey = "LogRetentionDays";
    public const bool DefaultEnabled = true;
    public const LogEventLevel DefaultLevel = LogEventLevel.Information;
    public const int DefaultRetentionDays = 7;

    private readonly IDictionary<string, object> _store;

    public LoggingSettings(IDictionary<string, object> store)
    {
        ArgumentNullException.ThrowIfNull(store);
        _store = store;
    }

    public bool Enabled
    {
        get => ReadBool(EnabledKey, DefaultEnabled);
        set => _store[EnabledKey] = value;
    }

    public LogEventLevel Level
    {
        get
        {
            if (!_store.TryGetValue(LevelKey, out object? raw) || raw is not string text)
            {
                return DefaultLevel;
            }

            return
                Enum.TryParse(text, ignoreCase: true, out LogEventLevel parsed)
                && Enum.IsDefined(parsed)
                ? parsed
                : DefaultLevel;
        }
        set
        {
            if (!Enum.IsDefined(value))
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "未知的日志等级。");
            }

            _store[LevelKey] = value.ToString();
        }
    }

    public int RetentionDays
    {
        get
        {
            if (!_store.TryGetValue(RetentionDaysKey, out object? raw))
            {
                return DefaultRetentionDays;
            }

            int? days = raw switch
            {
                int value => value,
                double value => (int)value,
                string text when int.TryParse(text, out int parsed) => parsed,
                _ => null,
            };
            return days is >= 1 ? days.Value : DefaultRetentionDays;
        }
        set
        {
            if (value < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "保留天数至少为 1。");
            }

            _store[RetentionDaysKey] = value;
        }
    }

    private bool ReadBool(string key, bool fallback)
    {
        if (!_store.TryGetValue(key, out object? raw))
        {
            return fallback;
        }

        return raw switch
        {
            bool value => value,
            string text when bool.TryParse(text, out bool parsed) => parsed,
            _ => fallback,
        };
    }
}
