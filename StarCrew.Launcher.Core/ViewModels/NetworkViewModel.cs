using CommunityToolkit.Mvvm.ComponentModel;
using StarCrew.Launcher.Services;

namespace StarCrew.Launcher.ViewModels;

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

    /// <summary>下拉下标。</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CustomVisible))]
    public partial int SelectedIndex { get; set; }

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
