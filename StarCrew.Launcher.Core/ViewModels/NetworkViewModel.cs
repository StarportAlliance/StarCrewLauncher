using CommunityToolkit.Mvvm.ComponentModel;
using StarCrew.Launcher.Services;

namespace StarCrew.Launcher.ViewModels;

/// <summary>网络设置：加速源选择、自定义前缀。</summary>
internal sealed partial class NetworkViewModel : ObservableObject
{
    private readonly INetworkSettings _settings;

    public NetworkViewModel(INetworkSettings settings, IAppStrings strings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(strings);
        _settings = settings;
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

    /// <summary>0=官方直连，1=gh-proxy，2=自定义前缀。</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CustomVisible))]
    public partial int SelectedIndex { get; set; }

    /// <summary>自定义前缀输入框是否为当前选中项。</summary>
    public bool CustomVisible => SelectedIndex == 2;

    [ObservableProperty]
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
