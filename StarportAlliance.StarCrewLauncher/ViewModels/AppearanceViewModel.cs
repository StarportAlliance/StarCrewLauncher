using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StarportAlliance.StarCrewLauncher.Models;
using StarportAlliance.StarCrewLauncher.Services;

namespace StarportAlliance.StarCrewLauncher.ViewModels;

internal sealed partial class AppearanceViewModel : ObservableObject
{
    private const string DefaultSolidBackgroundColor = "#808080";

    private readonly IThemeSettings _themeSettings;
    private readonly IBackdropSupport _backdropSupport;
    private readonly IBackgroundImageStore _imageStore;
    private readonly IFilePickerService _filePicker;
    private readonly INotificationService _notifications;
    private readonly IAppStrings _strings;
    private readonly IAppRestarter _restarter;
    private readonly FontOption _defaultFont;
    private FontOption _selectedFontOption;
    private bool _restartNotified;

    public AppearanceViewModel(
        IThemeSettings themeSettings,
        ISystemFontCatalog fontCatalog,
        IAppStrings strings,
        IBackdropSupport backdropSupport,
        IBackgroundImageStore imageStore,
        IFilePickerService filePicker,
        INotificationService notifications,
        IAppRestarter restarter
    )
    {
        ArgumentNullException.ThrowIfNull(themeSettings);
        ArgumentNullException.ThrowIfNull(fontCatalog);
        ArgumentNullException.ThrowIfNull(strings);
        ArgumentNullException.ThrowIfNull(backdropSupport);
        ArgumentNullException.ThrowIfNull(imageStore);
        ArgumentNullException.ThrowIfNull(filePicker);
        ArgumentNullException.ThrowIfNull(notifications);
        ArgumentNullException.ThrowIfNull(restarter);

        _themeSettings = themeSettings;
        _backdropSupport = backdropSupport;
        _imageStore = imageStore;
        _filePicker = filePicker;
        _notifications = notifications;
        _strings = strings;
        _restarter = restarter;
        _defaultFont = new FontOption(
            strings.GetString("Appearance_FontDefault"),
            string.Empty,
            null
        );
        List<FontOption> options = [_defaultFont, .. fontCatalog.GetFonts()];
        FontOptions = options;
        BuiltInBackgrounds =
        [
            .. BuiltInBackgroundCatalog.All.Select(
                background => new BuiltInBackgroundOptionViewModel(
                    background,
                    strings.GetString(background.NameKey),
                    SelectBuiltInBackground
                )
            ),
        ];
        _selectedFontOption =
            options.Find(option =>
                option.FamilyName is { } family
                && string.Equals(
                    family,
                    themeSettings.DisplayFont,
                    StringComparison.OrdinalIgnoreCase
                )
            ) ?? _defaultFont;
        UpdateBuiltInBackgroundSelection();
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

    public string LightAccentColor
    {
        get => _themeSettings.LightAccentColor ?? AccentPalette.DefaultLight;
        set
        {
            if (AccentPalette.Normalize(value) is not { } normalized)
            {
                return;
            }

            // 选回默认色等同于未自定义，避免“颜色没变但重置按钮亮着”
            string? stored = normalized == AccentPalette.DefaultLight ? null : normalized;
            if (stored == _themeSettings.LightAccentColor)
            {
                return;
            }

            _themeSettings.LightAccentColor = stored;
            OnPropertyChanged(nameof(LightAccentColor));
            OnPropertyChanged(nameof(CanResetLightAccent));
        }
    }

    public string DarkAccentColor
    {
        get => _themeSettings.DarkAccentColor ?? AccentPalette.DefaultDark;
        set
        {
            if (AccentPalette.Normalize(value) is not { } normalized)
            {
                return;
            }

            // 选回默认色等同于未自定义，避免“颜色没变但重置按钮亮着”
            string? stored = normalized == AccentPalette.DefaultDark ? null : normalized;
            if (stored == _themeSettings.DarkAccentColor)
            {
                return;
            }

            _themeSettings.DarkAccentColor = stored;
            OnPropertyChanged(nameof(DarkAccentColor));
            OnPropertyChanged(nameof(CanResetDarkAccent));
        }
    }

    public bool CanResetLightAccent => _themeSettings.LightAccentColor is not null;

    public bool CanResetDarkAccent => _themeSettings.DarkAccentColor is not null;

    public IReadOnlyList<FontOption> FontOptions { get; }

    public IReadOnlyList<BuiltInBackgroundOptionViewModel> BuiltInBackgrounds { get; }

    public FontOption SelectedFontOption
    {
        get => _selectedFontOption;
        set
        {
            if (value is null || ReferenceEquals(value, _selectedFontOption))
            {
                return;
            }

            _selectedFontOption = value;
            _themeSettings.DisplayFont = value.FamilyName;
            OnPropertyChanged(nameof(SelectedFontOption));
            OnPropertyChanged(nameof(CanResetFont));
        }
    }

    public bool CanResetFont => _themeSettings.DisplayFont is not null;

    [RelayCommand]
    private void ResetLightAccent()
    {
        if (_themeSettings.LightAccentColor is null)
        {
            return;
        }

        _themeSettings.LightAccentColor = null;
        OnPropertyChanged(nameof(LightAccentColor));
        OnPropertyChanged(nameof(CanResetLightAccent));
    }

    [RelayCommand]
    private void ResetDarkAccent()
    {
        if (_themeSettings.DarkAccentColor is null)
        {
            return;
        }

        _themeSettings.DarkAccentColor = null;
        OnPropertyChanged(nameof(DarkAccentColor));
        OnPropertyChanged(nameof(CanResetDarkAccent));
    }

    [RelayCommand]
    private void ResetFont()
    {
        if (_themeSettings.DisplayFont is null)
        {
            return;
        }

        _themeSettings.DisplayFont = null;
        _selectedFontOption = _defaultFont;
        OnPropertyChanged(nameof(SelectedFontOption));
        OnPropertyChanged(nameof(CanResetFont));
    }

    public bool IsMicaSupported => _backdropSupport.IsMicaSupported;

    public bool IsMicaUnsupported => !_backdropSupport.IsMicaSupported;

    public bool IsMicaBackground
    {
        get => _themeSettings.Background == BackgroundKind.Mica;
        set
        {
            if (value)
            {
                SetBackground(BackgroundKind.Mica);
            }
        }
    }

    public bool IsImageBackground
    {
        get => _themeSettings.Background == BackgroundKind.Image;
        set
        {
            if (value)
            {
                SetBackground(BackgroundKind.Image);
            }
        }
    }

    public bool IsSolidBackground
    {
        get => _themeSettings.Background == BackgroundKind.SolidColor;
        set
        {
            if (value)
            {
                SetBackground(BackgroundKind.SolidColor);
            }
        }
    }

    public string SolidBackgroundColor
    {
        get => _themeSettings.SolidBackgroundColor ?? DefaultSolidBackgroundColor;
        set
        {
            if (AccentPalette.Normalize(value) is not { } normalized)
            {
                return;
            }

            if (
                normalized == _themeSettings.SolidBackgroundColor
                && !_themeSettings.SolidBackgroundFollowsTheme
            )
            {
                return;
            }

            _themeSettings.SolidBackgroundColor = normalized;
            _themeSettings.SolidBackgroundFollowsTheme = false;
            MarkRestartNeeded();
            OnPropertyChanged(nameof(SolidBackgroundColor));
            OnPropertyChanged(nameof(SolidBackgroundColorLabel));
            OnPropertyChanged(nameof(CanResetSolidBackgroundColor));
            OnPropertyChanged(nameof(IsSolidBackgroundColorFollowTheme));
        }
    }

    public string SolidBackgroundColorLabel =>
        _themeSettings.SolidBackgroundFollowsTheme
            ? _strings.GetString("Appearance_SolidColorFollowTheme")
            : _themeSettings.SolidBackgroundColor ?? DefaultSolidBackgroundColor;

    public bool CanResetSolidBackgroundColor => !_themeSettings.SolidBackgroundFollowsTheme;

    public bool IsSolidBackgroundColorFollowTheme => _themeSettings.SolidBackgroundFollowsTheme;

    public bool IsSolidBackgroundColorLabelDisabled => !IsSolidBackground;

    public bool IsScrimOpacityDisabled => !IsImageBackground;

    public string BackgroundImageDisplayName =>
        _themeSettings.BackgroundImagePath is { } path
            ? Path.GetFileNameWithoutExtension(path)
            : SelectedBuiltInBackground.DisplayName;

    public string? BackgroundImagePath => _themeSettings.BackgroundImagePath;

    public bool HasCustomBackgroundImage => _themeSettings.BackgroundImagePath is not null;

    public bool CanAddBackgroundImage => !HasCustomBackgroundImage;

    public bool IsBuiltInBackgroundImageSelected =>
        !_themeSettings.UseCustomBackgroundImage || _themeSettings.BackgroundImagePath is null;

    public bool IsCustomBackgroundImageSelected =>
        _themeSettings.UseCustomBackgroundImage && _themeSettings.BackgroundImagePath is not null;

    public bool IsCustomBackgroundImageHighlighted =>
        IsImageBackground && IsCustomBackgroundImageSelected;

    public double ScrimOpacity
    {
        get => _themeSettings.ScrimOpacity;
        set
        {
            if (!double.IsFinite(value))
            {
                return;
            }

            double normalized = Math.Round(
                Math.Clamp(value, 0, 1),
                2,
                MidpointRounding.AwayFromZero
            );
            if (normalized == _themeSettings.ScrimOpacity)
            {
                return;
            }

            _themeSettings.ScrimOpacity = normalized;
            OnPropertyChanged(nameof(ScrimOpacity));
            OnPropertyChanged(nameof(ScrimOpacityLabel));
        }
    }

    public string ScrimOpacityLabel =>
        string.Format(
            System.Globalization.CultureInfo.InvariantCulture,
            "{0}%",
            Math.Round(ScrimOpacity * 100, MidpointRounding.AwayFromZero)
        );

    [RelayCommand]
    private void ResetSolidBackgroundColor()
    {
        if (_themeSettings.SolidBackgroundFollowsTheme)
        {
            return;
        }

        // 保留取色器当前颜色，避免重置时把透明色反写回设置
        _themeSettings.SolidBackgroundFollowsTheme = true;
        MarkRestartNeeded();
        OnPropertyChanged(nameof(SolidBackgroundColorLabel));
        OnPropertyChanged(nameof(CanResetSolidBackgroundColor));
        OnPropertyChanged(nameof(IsSolidBackgroundColorFollowTheme));
    }

    [RelayCommand]
    private async Task ChooseBackgroundImage()
    {
        string? source = await _filePicker.PickBackgroundImageAsync();
        if (source is null)
        {
            return;
        }

        try
        {
            string stored = await _imageStore.ImportAsync(source);
            _themeSettings.BackgroundImagePath = stored;
            _themeSettings.UseCustomBackgroundImage = true;
            _themeSettings.Background = BackgroundKind.Image;
            MarkRestartNeeded();
            OnPropertyChanged(nameof(BackgroundImagePath));
            OnPropertyChanged(nameof(BackgroundImageDisplayName));
            OnPropertyChanged(nameof(HasCustomBackgroundImage));
            OnPropertyChanged(nameof(CanAddBackgroundImage));
            RaiseBackgroundImageSelectionChanged();
            RaiseBackgroundChanged();
        }
        catch (Exception ex)
            when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            _notifications.Notify(
                _strings.GetString("Appearance_BackgroundImageImportFailedTitle"),
                _strings.GetString("Appearance_BackgroundImageImportFailedMessage"),
                NotificationSeverity.Error
            );
        }
    }

    private void SelectBuiltInBackground(BuiltInBackgroundOptionViewModel option)
    {
        _themeSettings.BuiltInBackgroundId = option.Id;
        _themeSettings.UseCustomBackgroundImage = false;
        _themeSettings.Background = BackgroundKind.Image;
        MarkRestartNeeded();
        RaiseBackgroundImageSelectionChanged();
        OnPropertyChanged(nameof(BackgroundImageDisplayName));
    }

    [RelayCommand]
    private void SelectCustomBackgroundImage()
    {
        if (!HasCustomBackgroundImage)
        {
            return;
        }

        _themeSettings.UseCustomBackgroundImage = true;
        _themeSettings.Background = BackgroundKind.Image;
        MarkRestartNeeded();
        RaiseBackgroundImageSelectionChanged();
    }

    [RelayCommand]
    private void RemoveBackgroundImage()
    {
        string? path = _themeSettings.BackgroundImagePath;
        if (path is null)
        {
            return;
        }

        _themeSettings.UseCustomBackgroundImage = false;
        _themeSettings.BackgroundImagePath = null;
        _imageStore.Delete(path);
        MarkRestartNeeded();
        OnPropertyChanged(nameof(BackgroundImagePath));
        OnPropertyChanged(nameof(BackgroundImageDisplayName));
        OnPropertyChanged(nameof(HasCustomBackgroundImage));
        OnPropertyChanged(nameof(CanAddBackgroundImage));
        RaiseBackgroundImageSelectionChanged();
        RaiseBackgroundChanged();
    }

    private BuiltInBackgroundOptionViewModel SelectedBuiltInBackground =>
        BuiltInBackgrounds.FirstOrDefault(option => option.IsSelected) ?? BuiltInBackgrounds[0];

    private void UpdateBuiltInBackgroundSelection()
    {
        string selectedId = BuiltInBackgroundCatalog.Resolve(_themeSettings.BuiltInBackgroundId).Id;
        bool useBuiltIn = IsBuiltInBackgroundImageSelected;
        foreach (BuiltInBackgroundOptionViewModel option in BuiltInBackgrounds)
        {
            option.IsSelected = useBuiltIn && option.Id == selectedId;
            option.IsHighlighted = IsImageBackground && option.IsSelected;
        }
    }

    private void RaiseBackgroundImageSelectionChanged()
    {
        OnPropertyChanged(nameof(IsBuiltInBackgroundImageSelected));
        OnPropertyChanged(nameof(IsCustomBackgroundImageSelected));
        OnPropertyChanged(nameof(IsCustomBackgroundImageHighlighted));
        UpdateBuiltInBackgroundSelection();
    }

    private void SetBackground(BackgroundKind background)
    {
        if (background == BackgroundKind.Mica && !_backdropSupport.IsMicaSupported)
        {
            return;
        }

        if (_themeSettings.Background == background)
        {
            return;
        }

        _themeSettings.Background = background;
        MarkRestartNeeded();
        RaiseBackgroundChanged();
    }

    private void MarkRestartNeeded()
    {
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

    private void RaiseBackgroundChanged()
    {
        OnPropertyChanged(nameof(IsMicaBackground));
        OnPropertyChanged(nameof(IsImageBackground));
        OnPropertyChanged(nameof(IsSolidBackground));
        OnPropertyChanged(nameof(IsSolidBackgroundColorLabelDisabled));
        OnPropertyChanged(nameof(IsScrimOpacityDisabled));
        OnPropertyChanged(nameof(IsCustomBackgroundImageHighlighted));
        UpdateBuiltInBackgroundSelection();
    }
}
