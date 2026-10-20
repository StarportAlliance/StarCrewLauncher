using CommunityToolkit.Mvvm.ComponentModel;
using StarCrew.Launcher.Services;

namespace StarCrew.Launcher.ViewModels;

internal sealed partial class NetworkViewModel : ObservableObject
{
    private readonly INetworkSettings _settings;
    private readonly string _officialPreview;
    private readonly string _fullUrlPlaceholder;
    private string _customPrefix = string.Empty;
    private string _appliedPrefix = string.Empty;
    private bool _customPrefixValid = true;

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

    /// <summary>按当前加速源展示的请求地址示意文案；自定义前缀非法时保持上一次合法预览。</summary>
    public string PreviewText =>
        SelectedIndex switch
        {
            0 => _officialPreview,
            1 => $"https://gh-proxy.org/{_fullUrlPlaceholder}",
            _ => string.IsNullOrEmpty(_appliedPrefix)
                ? _officialPreview
                : $"{_appliedPrefix.TrimEnd('/')}/{_fullUrlPlaceholder}",
        };

    /// <summary>下拉下标。</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(
        nameof(CustomVisible),
        nameof(PreviewText),
        nameof(SelectedOptionText),
        nameof(CustomPrefixErrorVisible),
        nameof(CustomPrefixHelpVisible)
    )]
    public partial int SelectedIndex { get; set; }

    public bool CustomVisible => SelectedIndex == 2;

    /// <summary>折叠态展示的当前加速源文案。</summary>
    public string SelectedOptionText => Options[Math.Clamp(SelectedIndex, 0, Options.Count - 1)];

    /// <summary>
    /// 自定义加速源前缀；首尾空白会被剔除后回写输入框。
    /// 手写而不用 [ObservableProperty]：值变化时不能发通知，否则两向绑定会把文本推回输入框、
    /// 光标被顶到末尾；只有真正修剪过空白时才通知一次用于回写。
    /// </summary>
    public string CustomPrefix
    {
        get => _customPrefix;
        set
        {
            string raw = value ?? string.Empty;
            string trimmed = raw.Trim();
            bool trimmedChanged = !string.Equals(trimmed, raw, StringComparison.Ordinal);
            if (!string.Equals(_customPrefix, trimmed, StringComparison.Ordinal))
            {
                _customPrefix = trimmed;
                ApplyCustomPrefix(trimmed);
            }

            if (trimmedChanged)
            {
                OnPropertyChanged();
            }
        }
    }

    /// <summary>是否显示错误文案：仅在选中自定义且输入非法时。</summary>
    public bool CustomPrefixErrorVisible => CustomVisible && !_customPrefixValid;

    /// <summary>是否显示说明文案。</summary>
    public bool CustomPrefixHelpVisible => !CustomPrefixErrorVisible;

    partial void OnSelectedIndexChanged(int value)
    {
        _settings.Accelerator = value switch
        {
            0 => DownloadAccelerator.Official,
            1 => DownloadAccelerator.GhProxy,
            _ => DownloadAccelerator.Custom,
        };
    }

    private void ApplyCustomPrefix(string trimmed)
    {
        bool valid = trimmed.Length == 0 || HttpsPrefix.IsValid(trimmed);
        if (valid)
        {
            // 非法输入不落盘：设置里保留上一次合法值，下载时按它兜底。
            _appliedPrefix = trimmed;
            _settings.CustomPrefix = trimmed;
        }

        if (_customPrefixValid != valid)
        {
            _customPrefixValid = valid;
            OnPropertyChanged(nameof(CustomPrefixErrorVisible));
            OnPropertyChanged(nameof(CustomPrefixHelpVisible));
        }

        OnPropertyChanged(nameof(PreviewText));
    }
}
