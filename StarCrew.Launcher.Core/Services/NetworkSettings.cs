namespace StarCrew.Launcher.Services;

internal sealed class NetworkSettings : INetworkSettings
{
    private const string AcceleratorKey = "DownloadAccelerator";
    private const string CustomPrefixKey = "DownloadAcceleratorCustomPrefix";

    private readonly IDictionary<string, object> _store;

    public NetworkSettings(IDictionary<string, object> store)
    {
        ArgumentNullException.ThrowIfNull(store);
        _store = store;
    }

    public DownloadAccelerator Accelerator
    {
        get
        {
            if (
                !_store.TryGetValue(AcceleratorKey, out object? raw)
                || raw is not string text
                || !Enum.TryParse(text, ignoreCase: true, out DownloadAccelerator parsed)
                || !Enum.IsDefined(parsed)
            )
            {
                return DownloadAccelerator.GhProxy;
            }

            return parsed;
        }
        set
        {
            if (!Enum.IsDefined(value))
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "未知的加速源。");
            }

            _store[AcceleratorKey] = value.ToString();
        }
    }

    public string CustomPrefix
    {
        get =>
            _store.TryGetValue(CustomPrefixKey, out object? raw) && raw is string text
                ? text
                : string.Empty;
        set => _store[CustomPrefixKey] = value ?? string.Empty;
    }
}
