namespace StarCrew.Launcher.Services;

internal sealed class VersionCacheSettings : IVersionCacheSettings
{
    public const string MaxAgeHoursKey = "VersionListMaxAgeHours";
    public const int DefaultMaxAgeHours = 24;
    public const int MinMaxAgeHours = 0;
    public const int MaxMaxAgeHours = 720;

    private readonly IDictionary<string, object> _store;

    public VersionCacheSettings(IDictionary<string, object> store)
    {
        ArgumentNullException.ThrowIfNull(store);
        _store = store;
    }

    public int MaxAgeHours
    {
        get
        {
            if (!_store.TryGetValue(MaxAgeHoursKey, out object? raw))
            {
                return DefaultMaxAgeHours;
            }

            int? hours = raw switch
            {
                int value => value,
                double value => (int)value,
                string text when int.TryParse(text, out int parsed) => parsed,
                _ => null,
            };
            return hours is null
                ? DefaultMaxAgeHours
                : Math.Clamp(hours.Value, MinMaxAgeHours, MaxMaxAgeHours);
        }
        set
        {
            if (value is < MinMaxAgeHours or > MaxMaxAgeHours)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    value,
                    "版本列表过期时间需在 0 到 720 小时之间。"
                );
            }

            _store[MaxAgeHoursKey] = value;
        }
    }
}
