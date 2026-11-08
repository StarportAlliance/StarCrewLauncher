namespace StarportAlliance.StarCrewLauncher.Services;

internal sealed class NetworkSettings : INetworkSettings
{
    public const string AuthCacheSecondsKey = "AuthStatusCacheSeconds";
    public const int DefaultAuthCacheSeconds = 60;
    public const int MinAuthCacheSeconds = 0;
    public const int MaxAuthCacheSeconds = 86400;

    public const string TimeoutSecondsKey = "NetworkTimeoutSeconds";
    public const int DefaultTimeoutSeconds = 6;
    public const int MinTimeoutSeconds = 1;
    public const int MaxTimeoutSeconds = 30;

    private readonly IDictionary<string, object> _store;

    public NetworkSettings(IDictionary<string, object> store)
    {
        ArgumentNullException.ThrowIfNull(store);
        _store = store;
    }

    public int AuthCacheSeconds
    {
        get =>
            Read(
                AuthCacheSecondsKey,
                DefaultAuthCacheSeconds,
                MinAuthCacheSeconds,
                MaxAuthCacheSeconds
            );
        set => Write(AuthCacheSecondsKey, value, MinAuthCacheSeconds, MaxAuthCacheSeconds);
    }

    public int TimeoutSeconds
    {
        get => Read(TimeoutSecondsKey, DefaultTimeoutSeconds, MinTimeoutSeconds, MaxTimeoutSeconds);
        set => Write(TimeoutSecondsKey, value, MinTimeoutSeconds, MaxTimeoutSeconds);
    }

    private int Read(string key, int fallback, int min, int max)
    {
        if (!_store.TryGetValue(key, out object? raw))
        {
            return fallback;
        }

        int? value = raw switch
        {
            int number => number,
            double number => (int)number,
            string text when int.TryParse(text, out int parsed) => parsed,
            _ => null,
        };
        return value is null ? fallback : Math.Clamp(value.Value, min, max);
    }

    private void Write(string key, int value, int min, int max)
    {
        if (value < min || value > max)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "网络设置超出允许范围。");
        }

        _store[key] = value;
    }
}
