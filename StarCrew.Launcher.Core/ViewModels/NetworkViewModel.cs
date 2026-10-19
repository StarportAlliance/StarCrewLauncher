using CommunityToolkit.Mvvm.ComponentModel;
using StarCrew.Launcher.Services;

namespace StarCrew.Launcher.ViewModels;

internal sealed partial class NetworkViewModel : ObservableObject
{
    private readonly INetworkSettings _settings;
    private readonly string _officialPreview;
    private readonly string _fullUrlPlaceholder;

    public NetworkViewModel(INetworkSettings settings, IAppStrings strings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(strings);
        _settings = settings;
        _officialPreview = strings.GetString("Network_PreviewOfficial");
        _fullUrlPlaceholder = strings.GetString("Network_PreviewFullUrl");
        Options =
        [
            strings.GetString("Network_OptionOfficial"),
            strings.GetString("Network_OptionGhProxy"),
            strings.GetString("Network_OptionCustom"),
        ];
        SelectedIndex = Math.Clamp((int)_settings.Accelerator, 0, 2);
        CustomPrefix = _settings.CustomPrefix ?? string.Empty;
    }

    public IReadOnlyList<string> Options { get; }

    /// <summary>按当前加速源展示的请求地址示意文案；自定义前缀为空时与官方一致（回退直连）。</summary>
    public string PreviewText =>
        SelectedIndex switch
        {
            0 => _officialPreview,
            1 => $"https://gh-proxy.org/{_fullUrlPlaceholder}",
            _ => string.IsNullOrWhiteSpace(CustomPrefix)
                ? _officialPreview
                : $"{CustomPrefix.TrimEnd('/')}/{_fullUrlPlaceholder}",
        };

    /// <summary>下拉下标。</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CustomVisible), nameof(PreviewText))]
    public partial int SelectedIndex { get; set; }

    public bool CustomVisible => SelectedIndex == 2;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PreviewText))]
    public partial string CustomPrefix { get; set; } = string.Empty;

    partial void OnSelectedIndexChanged(int value)
    {
        _settings.Accelerator = value switch
        {
            0 => DownloadAccelerator.Official,
            1 => DownloadAccelerator.GhProxy,
            _ => DownloadAccelerator.Custom,
        };
    }

    partial void OnCustomPrefixChanged(string value)
    {
        _settings.CustomPrefix = value ?? string.Empty;
    }
}
