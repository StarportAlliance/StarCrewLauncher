using NSubstitute;
using StarCrew.Launcher.Models;
using StarCrew.Launcher.Services;
using StarCrew.Launcher.ViewModels;

namespace StarCrew.Launcher.Tests.ViewModels;

public sealed class AppearanceViewModelTests
{
    private static readonly FontOption[] SampleFonts =
    [
        new("微软雅黑", "Microsoft YaHei", "Microsoft YaHei"),
        new("宋体", "SimSun", "SimSun"),
    ];

    private static AppearanceViewModel Create(
        IThemeSettings? theme = null,
        ISystemFontCatalog? catalog = null,
        IBackdropSupport? backdrop = null,
        IBackgroundImageStore? imageStore = null,
        IFilePickerService? filePicker = null,
        INotificationService? notifications = null,
        IAppRestarter? restarter = null
    )
    {
        ISystemFontCatalog fonts = catalog ?? Substitute.For<ISystemFontCatalog>();
        fonts.GetFonts().Returns(SampleFonts);
        return new AppearanceViewModel(
            theme ?? Substitute.For<IThemeSettings>(),
            fonts,
            TestStrings.Instance,
            backdrop ?? Substitute.For<IBackdropSupport>(),
            imageStore ?? Substitute.For<IBackgroundImageStore>(),
            filePicker ?? Substitute.For<IFilePickerService>(),
            notifications ?? Substitute.For<INotificationService>(),
            restarter ?? Substitute.For<IAppRestarter>()
        );
    }

    private static IBackdropSupport MicaSupportedBackdrop()
    {
        IBackdropSupport backdrop = Substitute.For<IBackdropSupport>();
        backdrop.IsMicaSupported.Returns(true);
        return backdrop;
    }

    [Fact]
    public void Ctor_WithNullThemeSettings_ThrowsArgumentNullException() =>
        Assert.Throws<ArgumentNullException>(() =>
            new AppearanceViewModel(
                null!,
                Substitute.For<ISystemFontCatalog>(),
                TestStrings.Instance,
                Substitute.For<IBackdropSupport>(),
                Substitute.For<IBackgroundImageStore>(),
                Substitute.For<IFilePickerService>(),
                Substitute.For<INotificationService>(),
                Substitute.For<IAppRestarter>()
            )
        );

    [Fact]
    public void Ctor_WithNullFontCatalog_ThrowsArgumentNullException() =>
        Assert.Throws<ArgumentNullException>(() =>
            new AppearanceViewModel(
                Substitute.For<IThemeSettings>(),
                null!,
                TestStrings.Instance,
                Substitute.For<IBackdropSupport>(),
                Substitute.For<IBackgroundImageStore>(),
                Substitute.For<IFilePickerService>(),
                Substitute.For<INotificationService>(),
                Substitute.For<IAppRestarter>()
            )
        );

    [Fact]
    public void Ctor_WithNullStrings_ThrowsArgumentNullException() =>
        Assert.Throws<ArgumentNullException>(() =>
            new AppearanceViewModel(
                Substitute.For<IThemeSettings>(),
                Substitute.For<ISystemFontCatalog>(),
                null!,
                Substitute.For<IBackdropSupport>(),
                Substitute.For<IBackgroundImageStore>(),
                Substitute.For<IFilePickerService>(),
                Substitute.For<INotificationService>(),
                Substitute.For<IAppRestarter>()
            )
        );

    [Fact]
    public void Ctor_WithNullBackdropSupport_ThrowsArgumentNullException() =>
        Assert.Throws<ArgumentNullException>(() =>
            new AppearanceViewModel(
                Substitute.For<IThemeSettings>(),
                Substitute.For<ISystemFontCatalog>(),
                TestStrings.Instance,
                null!,
                Substitute.For<IBackgroundImageStore>(),
                Substitute.For<IFilePickerService>(),
                Substitute.For<INotificationService>(),
                Substitute.For<IAppRestarter>()
            )
        );

    [Fact]
    public void Ctor_WithNullImageStore_ThrowsArgumentNullException() =>
        Assert.Throws<ArgumentNullException>(() =>
            new AppearanceViewModel(
                Substitute.For<IThemeSettings>(),
                Substitute.For<ISystemFontCatalog>(),
                TestStrings.Instance,
                Substitute.For<IBackdropSupport>(),
                null!,
                Substitute.For<IFilePickerService>(),
                Substitute.For<INotificationService>(),
                Substitute.For<IAppRestarter>()
            )
        );

    [Fact]
    public void Ctor_WithNullFilePicker_ThrowsArgumentNullException() =>
        Assert.Throws<ArgumentNullException>(() =>
            new AppearanceViewModel(
                Substitute.For<IThemeSettings>(),
                Substitute.For<ISystemFontCatalog>(),
                TestStrings.Instance,
                Substitute.For<IBackdropSupport>(),
                Substitute.For<IBackgroundImageStore>(),
                null!,
                Substitute.For<INotificationService>(),
                Substitute.For<IAppRestarter>()
            )
        );

    [Fact]
    public void Ctor_WithNullNotifications_ThrowsArgumentNullException() =>
        Assert.Throws<ArgumentNullException>(() =>
            new AppearanceViewModel(
                Substitute.For<IThemeSettings>(),
                Substitute.For<ISystemFontCatalog>(),
                TestStrings.Instance,
                Substitute.For<IBackdropSupport>(),
                Substitute.For<IBackgroundImageStore>(),
                Substitute.For<IFilePickerService>(),
                null!,
                Substitute.For<IAppRestarter>()
            )
        );

    [Fact]
    public void Ctor_WithNullRestarter_ThrowsArgumentNullException() =>
        Assert.Throws<ArgumentNullException>(() =>
            new AppearanceViewModel(
                Substitute.For<IThemeSettings>(),
                Substitute.For<ISystemFontCatalog>(),
                TestStrings.Instance,
                Substitute.For<IBackdropSupport>(),
                Substitute.For<IBackgroundImageStore>(),
                Substitute.For<IFilePickerService>(),
                Substitute.For<INotificationService>(),
                null!
            )
        );

    [Theory]
    [InlineData("System", 0)]
    [InlineData("Light", 1)]
    [InlineData("Dark", 2)]
    public void SelectedThemeIndex_ReadsFromSettings(string themeName, int expected)
    {
        IThemeSettings settings = Substitute.For<IThemeSettings>();
        settings.Theme.Returns(Enum.Parse<AppTheme>(themeName));

        AppearanceViewModel vm = Create(theme: settings);

        Assert.Equal(expected, vm.SelectedThemeIndex);
    }

    [Theory]
    [InlineData(0, "System")]
    [InlineData(1, "Light")]
    [InlineData(2, "Dark")]
    [InlineData(99, "System")]
    public void SelectedThemeIndex_WritesToSettings(int index, string expectedName)
    {
        IThemeSettings settings = Substitute.For<IThemeSettings>();
        AppearanceViewModel vm = Create(theme: settings);

        vm.SelectedThemeIndex = index;

        settings.Received(1).Theme = Enum.Parse<AppTheme>(expectedName);
    }

    [Fact]
    public void AccentColors_WithoutCustomization_ReturnBuiltInDefaults()
    {
        AppearanceViewModel vm = Create(theme: new ThemeSettings(new Dictionary<string, object>()));

        Assert.Equal(AccentPalette.DefaultLight, vm.LightAccentColor);
        Assert.Equal(AccentPalette.DefaultDark, vm.DarkAccentColor);
        Assert.False(vm.CanResetLightAccent);
        Assert.False(vm.CanResetDarkAccent);
    }

    [Fact]
    public void LightAccentColor_SetCustom_StoresAndRaisesNotifications()
    {
        Dictionary<string, object> store = [];
        ThemeSettings settings = new(store);
        AppearanceViewModel vm = Create(theme: settings);
        List<string?> changed = [];
        vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        vm.LightAccentColor = "#123456";

        Assert.Equal("#123456", vm.LightAccentColor);
        Assert.Equal("#123456", settings.LightAccentColor);
        Assert.True(vm.CanResetLightAccent);
        Assert.Contains(nameof(AppearanceViewModel.LightAccentColor), changed);
        Assert.Contains(nameof(AppearanceViewModel.CanResetLightAccent), changed);
    }

    [Fact]
    public void DarkAccentColor_SetCustom_StoresAndRaisesNotifications()
    {
        Dictionary<string, object> store = [];
        ThemeSettings settings = new(store);
        AppearanceViewModel vm = Create(theme: settings);
        List<string?> changed = [];
        vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        vm.DarkAccentColor = "#ABCDEF";

        Assert.Equal("#ABCDEF", vm.DarkAccentColor);
        Assert.Equal("#ABCDEF", settings.DarkAccentColor);
        Assert.True(vm.CanResetDarkAccent);
        Assert.Contains(nameof(AppearanceViewModel.DarkAccentColor), changed);
        Assert.Contains(nameof(AppearanceViewModel.CanResetDarkAccent), changed);
    }

    [Fact]
    public void LightAccentColor_SetBuiltInDefault_TreatedAsNotCustomized()
    {
        ThemeSettings settings = new(new Dictionary<string, object>());
        AppearanceViewModel vm = Create(theme: settings);

        vm.LightAccentColor = "#123456";
        vm.LightAccentColor = AccentPalette.DefaultLight;

        Assert.Null(settings.LightAccentColor);
        Assert.False(vm.CanResetLightAccent);
    }

    [Fact]
    public void LightAccentColor_SetInvalid_KeepsStoredValue()
    {
        ThemeSettings settings = new(new Dictionary<string, object>());
        AppearanceViewModel vm = Create(theme: settings);

        vm.LightAccentColor = "oops";

        Assert.Null(settings.LightAccentColor);
        Assert.Equal(AccentPalette.DefaultLight, vm.LightAccentColor);
    }

    [Fact]
    public void ResetLightAccent_WhenCustomized_ClearsStoredColor()
    {
        Dictionary<string, object> store = new()
        {
            [ThemeSettings.LightAccentColorKey] = "#123456",
        };
        ThemeSettings settings = new(store);
        AppearanceViewModel vm = Create(theme: settings);
        List<string?> changed = [];
        vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        vm.ResetLightAccentCommand.Execute(null);

        Assert.Equal(AccentPalette.DefaultLight, vm.LightAccentColor);
        Assert.False(vm.CanResetLightAccent);
        Assert.False(store.ContainsKey(ThemeSettings.LightAccentColorKey));
        Assert.Contains(nameof(AppearanceViewModel.LightAccentColor), changed);
        Assert.Contains(nameof(AppearanceViewModel.CanResetLightAccent), changed);
    }

    [Fact]
    public void FontOptions_PrependDefaultAndExposeSystemFonts()
    {
        AppearanceViewModel vm = Create(theme: new ThemeSettings(new Dictionary<string, object>()));

        Assert.Equal(3, vm.FontOptions.Count);
        Assert.True(vm.FontOptions[0].IsDefault);
        Assert.Equal(
            TestStrings.Instance.GetString("Appearance_FontDefault"),
            vm.FontOptions[0].DisplayName
        );
        Assert.Equal("Microsoft YaHei", vm.FontOptions[1].FamilyName);
    }

    [Fact]
    public void SelectedFontOption_WithoutCustomization_IsDefault()
    {
        AppearanceViewModel vm = Create(theme: new ThemeSettings(new Dictionary<string, object>()));

        Assert.True(vm.SelectedFontOption.IsDefault);
        Assert.False(vm.CanResetFont);
    }

    [Fact]
    public void SelectedFontOption_WithStoredFont_SelectsMatchingOption()
    {
        ThemeSettings settings = new(new Dictionary<string, object>()) { DisplayFont = "SimSun" };

        AppearanceViewModel vm = Create(theme: settings);

        Assert.Equal("SimSun", vm.SelectedFontOption.FamilyName);
        Assert.True(vm.CanResetFont);
    }

    [Fact]
    public void SelectedFontOption_WithUninstalledFont_SelectsDefault()
    {
        ThemeSettings settings = new(new Dictionary<string, object>())
        {
            DisplayFont = "Gone Font",
        };

        AppearanceViewModel vm = Create(theme: settings);

        Assert.True(vm.SelectedFontOption.IsDefault);
    }

    [Fact]
    public void SelectedFontOption_SetFont_StoresAndRaisesNotifications()
    {
        ThemeSettings settings = new(new Dictionary<string, object>());
        AppearanceViewModel vm = Create(theme: settings);
        List<string?> changed = [];
        vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        vm.SelectedFontOption = vm.FontOptions[2];

        Assert.Equal("SimSun", settings.DisplayFont);
        Assert.True(vm.CanResetFont);
        Assert.Contains(nameof(AppearanceViewModel.SelectedFontOption), changed);
        Assert.Contains(nameof(AppearanceViewModel.CanResetFont), changed);
    }

    [Fact]
    public void SelectedFontOption_SetDefault_ClearsStoredFont()
    {
        ThemeSettings settings = new(new Dictionary<string, object>()) { DisplayFont = "SimSun" };
        AppearanceViewModel vm = Create(theme: settings);

        vm.SelectedFontOption = vm.FontOptions[0];

        Assert.Null(settings.DisplayFont);
        Assert.False(vm.CanResetFont);
    }

    [Fact]
    public void ResetFont_WhenCustomized_ClearsAndSelectsDefault()
    {
        Dictionary<string, object> store = new() { [ThemeSettings.DisplayFontKey] = "SimSun" };
        ThemeSettings settings = new(store);
        AppearanceViewModel vm = Create(theme: settings);
        List<string?> changed = [];
        vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        vm.ResetFontCommand.Execute(null);

        Assert.Null(settings.DisplayFont);
        Assert.True(vm.SelectedFontOption.IsDefault);
        Assert.False(vm.CanResetFont);
        Assert.False(store.ContainsKey(ThemeSettings.DisplayFontKey));
        Assert.Contains(nameof(AppearanceViewModel.SelectedFontOption), changed);
        Assert.Contains(nameof(AppearanceViewModel.CanResetFont), changed);
    }

    [Fact]
    public void ResetFont_WhenNotCustomized_DoesNothing()
    {
        AppearanceViewModel vm = Create(theme: new ThemeSettings(new Dictionary<string, object>()));
        List<string?> changed = [];
        vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        vm.ResetFontCommand.Execute(null);

        Assert.True(vm.SelectedFontOption.IsDefault);
        Assert.False(vm.CanResetFont);
        Assert.Empty(changed);
    }

    [Fact]
    public void IsMicaSupported_ReflectsBackdropSupport()
    {
        AppearanceViewModel vm = Create(backdrop: MicaSupportedBackdrop());

        Assert.True(vm.IsMicaSupported);
        Assert.False(vm.IsMicaUnsupported);
    }

    [Theory]
    [InlineData("Mica")]
    [InlineData("Image")]
    [InlineData("SolidColor")]
    public void BackgroundFlags_ReadFromSettings(string backgroundName)
    {
        BackgroundKind background = Enum.Parse<BackgroundKind>(backgroundName);
        ThemeSettings settings = new(new Dictionary<string, object>()) { Background = background };
        AppearanceViewModel vm = Create(theme: settings, backdrop: MicaSupportedBackdrop());

        Assert.Equal(background == BackgroundKind.Mica, vm.IsMicaBackground);
        Assert.Equal(background == BackgroundKind.Image, vm.IsImageBackground);
        Assert.Equal(background == BackgroundKind.SolidColor, vm.IsSolidBackground);
        Assert.Equal(
            background != BackgroundKind.SolidColor,
            vm.IsSolidBackgroundColorLabelDisabled
        );
    }

    [Fact]
    public void IsSolidBackground_SetTrue_StoresSolidColorAndRaisesNotifications()
    {
        ThemeSettings settings = new(new Dictionary<string, object>());
        AppearanceViewModel vm = Create(theme: settings);
        List<string?> changed = [];
        vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        vm.IsSolidBackground = true;

        Assert.Equal(BackgroundKind.SolidColor, settings.Background);
        Assert.Contains(nameof(AppearanceViewModel.IsMicaBackground), changed);
        Assert.Contains(nameof(AppearanceViewModel.IsImageBackground), changed);
        Assert.Contains(nameof(AppearanceViewModel.IsSolidBackground), changed);
        Assert.Contains(nameof(AppearanceViewModel.IsSolidBackgroundColorLabelDisabled), changed);
        Assert.Contains(nameof(AppearanceViewModel.IsScrimOpacityDisabled), changed);
    }

    [Fact]
    public void IsSolidBackground_SetTrue_NotifiesRestartOnlyOnce()
    {
        ThemeSettings settings = new(new Dictionary<string, object>());
        INotificationService notifications = Substitute.For<INotificationService>();
        IAppRestarter restarter = Substitute.For<IAppRestarter>();
        AppearanceViewModel vm = Create(
            theme: settings,
            notifications: notifications,
            restarter: restarter
        );

        vm.IsSolidBackground = true;
        vm.SolidBackgroundColor = "#123456";

        notifications
            .Received(1)
            .Notify(
                TestStrings.Instance.GetString("Logging_RestartTitle"),
                null,
                NotificationSeverity.Informational,
                Arg.Is<NotificationAction?>(action => action != null)
            );

        NotificationAction action = notifications
            .ReceivedCalls()
            .Select(call => call.GetArguments()[3] as NotificationAction)
            .First(a => a != null)!;
        action.Callback();

        restarter.Received(1).Restart();
    }

    [Fact]
    public void IsScrimOpacityDisabled_TracksImageBackground()
    {
        ThemeSettings settings = new(new Dictionary<string, object>());
        AppearanceViewModel vm = Create(theme: settings);

        Assert.False(vm.IsScrimOpacityDisabled);

        vm.IsSolidBackground = true;

        Assert.True(vm.IsScrimOpacityDisabled);
    }

    [Fact]
    public void IsImageBackground_SetFalse_DoesNotChangeBackground()
    {
        ThemeSettings settings = new(new Dictionary<string, object>());
        AppearanceViewModel vm = Create(theme: settings);

        vm.IsImageBackground = false;

        Assert.Equal(BackgroundKind.Image, settings.Background);
    }

    [Fact]
    public void IsMicaBackground_SetTrue_WhenUnsupported_KeepsCurrentBackground()
    {
        ThemeSettings settings = new(new Dictionary<string, object>());
        AppearanceViewModel vm = Create(theme: settings);

        vm.IsMicaBackground = true;

        Assert.Equal(BackgroundKind.Image, settings.Background);
    }

    [Fact]
    public void IsMicaBackground_SetTrue_WhenSupported_StoresMica()
    {
        ThemeSettings settings = new(new Dictionary<string, object>());
        AppearanceViewModel vm = Create(theme: settings, backdrop: MicaSupportedBackdrop());

        vm.IsMicaBackground = true;

        Assert.Equal(BackgroundKind.Mica, settings.Background);
    }

    [Fact]
    public void SolidBackgroundColor_WithoutCustomization_ReturnsPickerFallbackAndFollowsTheme()
    {
        AppearanceViewModel vm = Create(theme: new ThemeSettings(new Dictionary<string, object>()));

        Assert.Equal("#808080", vm.SolidBackgroundColor);
        Assert.Equal(
            TestStrings.Instance.GetString("Appearance_SolidColorFollowTheme"),
            vm.SolidBackgroundColorLabel
        );
        Assert.False(vm.CanResetSolidBackgroundColor);
        Assert.True(vm.IsSolidBackgroundColorFollowTheme);
    }

    [Fact]
    public void SolidBackgroundColor_SetValid_NormalizesAndRaisesNotifications()
    {
        ThemeSettings settings = new(new Dictionary<string, object>());
        AppearanceViewModel vm = Create(theme: settings);
        List<string?> changed = [];
        vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        vm.SolidBackgroundColor = "123456";

        Assert.Equal("#123456", settings.SolidBackgroundColor);
        Assert.False(settings.SolidBackgroundFollowsTheme);
        Assert.Equal("#123456", vm.SolidBackgroundColorLabel);
        Assert.True(vm.CanResetSolidBackgroundColor);
        Assert.False(vm.IsSolidBackgroundColorFollowTheme);
        Assert.Contains(nameof(AppearanceViewModel.SolidBackgroundColor), changed);
        Assert.Contains(nameof(AppearanceViewModel.SolidBackgroundColorLabel), changed);
        Assert.Contains(nameof(AppearanceViewModel.CanResetSolidBackgroundColor), changed);
        Assert.Contains(nameof(AppearanceViewModel.IsSolidBackgroundColorFollowTheme), changed);
    }

    [Fact]
    public void SolidBackgroundColor_SetInvalid_KeepsStoredValue()
    {
        ThemeSettings settings = new(new Dictionary<string, object>());
        AppearanceViewModel vm = Create(theme: settings);

        vm.SolidBackgroundColor = "oops";

        Assert.Null(settings.SolidBackgroundColor);
        Assert.False(vm.CanResetSolidBackgroundColor);
    }

    [Fact]
    public void ResetSolidBackgroundColor_WhenCustomized_KeepsPickerColorAndFollowsTheme()
    {
        ThemeSettings settings = new(new Dictionary<string, object>())
        {
            SolidBackgroundColor = "#123456",
        };
        AppearanceViewModel vm = Create(theme: settings);

        vm.ResetSolidBackgroundColorCommand.Execute(null);

        Assert.Equal("#123456", settings.SolidBackgroundColor);
        Assert.True(settings.SolidBackgroundFollowsTheme);
        Assert.Equal("#123456", vm.SolidBackgroundColor);
        Assert.False(vm.CanResetSolidBackgroundColor);
        Assert.True(vm.IsSolidBackgroundColorFollowTheme);
    }

    [Fact]
    public void ResetSolidBackgroundColor_WhenAlreadyFollowingTheme_DoesNothing()
    {
        ThemeSettings settings = new(new Dictionary<string, object>());
        AppearanceViewModel vm = Create(theme: settings);
        List<string?> changed = [];
        vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        vm.ResetSolidBackgroundColorCommand.Execute(null);

        Assert.True(settings.SolidBackgroundFollowsTheme);
        Assert.Empty(changed);
    }

    [Fact]
    public void BackgroundImageDisplayName_WithoutCustomImage_IsBuiltIn()
    {
        AppearanceViewModel vm = Create(theme: new ThemeSettings(new Dictionary<string, object>()));

        Assert.Equal(
            TestStrings.Instance.GetString("Appearance_BackgroundImageBuiltIn"),
            vm.BackgroundImageDisplayName
        );
        Assert.Equal(
            TestStrings.Instance.GetString("Appearance_BackgroundImageBuiltIn"),
            vm.BuiltInBackgroundImageName
        );
        Assert.False(vm.HasCustomBackgroundImage);
        Assert.True(vm.CanAddBackgroundImage);
        Assert.True(vm.IsBuiltInBackgroundImageSelected);
        Assert.False(vm.IsCustomBackgroundImageSelected);
    }

    [Fact]
    public void BackgroundImageDisplayName_WithCustomImage_IsFileNameWithoutExtension()
    {
        ThemeSettings settings = new(new Dictionary<string, object>())
        {
            BackgroundImagePath = @"C:\data\Background\background.png",
        };
        AppearanceViewModel vm = Create(theme: settings);

        Assert.Equal("background", vm.BackgroundImageDisplayName);
        Assert.True(vm.HasCustomBackgroundImage);
        Assert.False(vm.CanAddBackgroundImage);
        Assert.True(vm.IsCustomBackgroundImageSelected);
        Assert.False(vm.IsBuiltInBackgroundImageSelected);
    }

    [Fact]
    public void IsBuiltInBackgroundImageSelected_WithPathButNotUsingCustom_IsTrue()
    {
        Dictionary<string, object> store = new()
        {
            [ThemeSettings.BackgroundImagePathKey] = @"C:\data\Background\background.png",
            [ThemeSettings.UseCustomBackgroundImageKey] = false,
        };
        ThemeSettings settings = new(store);
        AppearanceViewModel vm = Create(theme: settings);

        Assert.True(vm.HasCustomBackgroundImage);
        Assert.True(vm.IsBuiltInBackgroundImageSelected);
        Assert.False(vm.IsCustomBackgroundImageSelected);
    }

    [Fact]
    public void BackgroundImageHighlight_TracksImageBackgroundAndSelection()
    {
        Dictionary<string, object> store = new()
        {
            [ThemeSettings.BackgroundImagePathKey] = @"C:\data\Background\background.png",
            [ThemeSettings.UseCustomBackgroundImageKey] = true,
        };
        AppearanceViewModel vm = Create(theme: new ThemeSettings(store));

        Assert.False(vm.IsBuiltInBackgroundImageHighlighted);
        Assert.True(vm.IsCustomBackgroundImageHighlighted);

        vm.IsSolidBackground = true;

        Assert.False(vm.IsBuiltInBackgroundImageHighlighted);
        Assert.False(vm.IsCustomBackgroundImageHighlighted);
    }

    [Fact]
    public async Task ChooseBackgroundImage_WhenPicked_ImportsAndSelectsImage()
    {
        ThemeSettings settings = new(new Dictionary<string, object>());
        IFilePickerService picker = Substitute.For<IFilePickerService>();
        picker.PickBackgroundImageAsync().Returns(Task.FromResult<string?>("C:\\src\\photo.png"));
        IBackgroundImageStore store = Substitute.For<IBackgroundImageStore>();
        store
            .ImportAsync("C:\\src\\photo.png")
            .Returns(Task.FromResult(@"C:\data\Background\background.PNG"));
        AppearanceViewModel vm = Create(
            theme: settings,
            backdrop: MicaSupportedBackdrop(),
            imageStore: store,
            filePicker: picker
        );

        await vm.ChooseBackgroundImageCommand.ExecuteAsync(null);

        Assert.Equal(@"C:\data\Background\background.PNG", settings.BackgroundImagePath);
        Assert.True(settings.UseCustomBackgroundImage);
        Assert.Equal(BackgroundKind.Image, settings.Background);
        Assert.Equal("background", vm.BackgroundImageDisplayName);
        Assert.True(vm.HasCustomBackgroundImage);
        Assert.False(vm.CanAddBackgroundImage);
        Assert.True(vm.IsCustomBackgroundImageSelected);
        Assert.False(vm.IsBuiltInBackgroundImageSelected);
    }

    [Fact]
    public async Task ChooseBackgroundImage_WhenCanceled_DoesNothing()
    {
        ThemeSettings settings = new(new Dictionary<string, object>());
        IFilePickerService picker = Substitute.For<IFilePickerService>();
        picker.PickBackgroundImageAsync().Returns(Task.FromResult<string?>(null));
        IBackgroundImageStore store = Substitute.For<IBackgroundImageStore>();
        AppearanceViewModel vm = Create(theme: settings, imageStore: store, filePicker: picker);

        await vm.ChooseBackgroundImageCommand.ExecuteAsync(null);

        await store.DidNotReceive().ImportAsync(Arg.Any<string>());
        Assert.Null(settings.BackgroundImagePath);
    }

    [Fact]
    public async Task ChooseBackgroundImage_WhenImportFails_Notifies()
    {
        ThemeSettings settings = new(new Dictionary<string, object>());
        IFilePickerService picker = Substitute.For<IFilePickerService>();
        picker.PickBackgroundImageAsync().Returns(Task.FromResult<string?>("C:\\src\\broken.gif"));
        IBackgroundImageStore store = Substitute.For<IBackgroundImageStore>();
        store
            .ImportAsync("C:\\src\\broken.gif")
            .Returns(Task.FromException<string>(new IOException("boom")));
        INotificationService notifications = Substitute.For<INotificationService>();
        AppearanceViewModel vm = Create(
            theme: settings,
            imageStore: store,
            filePicker: picker,
            notifications: notifications
        );

        await vm.ChooseBackgroundImageCommand.ExecuteAsync(null);

        notifications
            .Received(1)
            .Notify(
                TestStrings.Instance.GetString("Appearance_BackgroundImageImportFailedTitle"),
                TestStrings.Instance.GetString("Appearance_BackgroundImageImportFailedMessage"),
                NotificationSeverity.Error,
                Arg.Any<NotificationAction?>()
            );
        Assert.Null(settings.BackgroundImagePath);
    }

    [Fact]
    public void SelectBuiltInBackgroundImage_WhenCustomSelected_SwitchesAndKeepsPath()
    {
        Dictionary<string, object> store = new()
        {
            [ThemeSettings.BackgroundImagePathKey] = @"C:\data\Background\background.png",
            [ThemeSettings.UseCustomBackgroundImageKey] = true,
        };
        ThemeSettings settings = new(store);
        AppearanceViewModel vm = Create(theme: settings);

        vm.SelectBuiltInBackgroundImageCommand.Execute(null);

        Assert.False(settings.UseCustomBackgroundImage);
        Assert.Equal(@"C:\data\Background\background.png", settings.BackgroundImagePath);
        Assert.Equal(BackgroundKind.Image, settings.Background);
        Assert.True(vm.IsBuiltInBackgroundImageSelected);
        Assert.False(vm.IsCustomBackgroundImageSelected);
    }

    [Fact]
    public void SelectCustomBackgroundImage_WhenPathExists_SwitchesToCustom()
    {
        Dictionary<string, object> store = new()
        {
            [ThemeSettings.BackgroundImagePathKey] = @"C:\data\Background\background.png",
            [ThemeSettings.UseCustomBackgroundImageKey] = false,
        };
        ThemeSettings settings = new(store);
        AppearanceViewModel vm = Create(theme: settings);

        vm.SelectCustomBackgroundImageCommand.Execute(null);

        Assert.True(settings.UseCustomBackgroundImage);
        Assert.Equal(BackgroundKind.Image, settings.Background);
        Assert.True(vm.IsCustomBackgroundImageSelected);
        Assert.False(vm.IsBuiltInBackgroundImageSelected);
    }

    [Fact]
    public void SelectCustomBackgroundImage_WhenNoCustomImage_DoesNothing()
    {
        ThemeSettings settings = new(new Dictionary<string, object>());
        AppearanceViewModel vm = Create(theme: settings);

        vm.SelectCustomBackgroundImageCommand.Execute(null);

        Assert.False(settings.UseCustomBackgroundImage);
        Assert.True(vm.IsBuiltInBackgroundImageSelected);
    }

    [Fact]
    public void RemoveBackgroundImage_WhenCustomized_ClearsAndDeletes()
    {
        Dictionary<string, object> store = new()
        {
            [ThemeSettings.BackgroundImagePathKey] = @"C:\data\Background\background.png",
            [ThemeSettings.UseCustomBackgroundImageKey] = true,
        };
        ThemeSettings settings = new(store);
        IBackgroundImageStore imageStore = Substitute.For<IBackgroundImageStore>();
        AppearanceViewModel vm = Create(theme: settings, imageStore: imageStore);

        vm.RemoveBackgroundImageCommand.Execute(null);

        Assert.Null(settings.BackgroundImagePath);
        Assert.False(settings.UseCustomBackgroundImage);
        imageStore.Received(1).Delete(@"C:\data\Background\background.png");
        Assert.False(vm.HasCustomBackgroundImage);
        Assert.True(vm.CanAddBackgroundImage);
        Assert.True(vm.IsBuiltInBackgroundImageSelected);
        Assert.Equal(
            TestStrings.Instance.GetString("Appearance_BackgroundImageBuiltIn"),
            vm.BackgroundImageDisplayName
        );
    }

    [Fact]
    public void RemoveBackgroundImage_WhenNotCustomized_DoesNothing()
    {
        ThemeSettings settings = new(new Dictionary<string, object>());
        IBackgroundImageStore imageStore = Substitute.For<IBackgroundImageStore>();
        AppearanceViewModel vm = Create(theme: settings, imageStore: imageStore);

        vm.RemoveBackgroundImageCommand.Execute(null);

        imageStore.DidNotReceive().Delete(Arg.Any<string>());
    }

    [Fact]
    public void ScrimOpacity_WithEmptyStore_ReturnsDefaultAndLabel()
    {
        ThemeSettings settings = new(new Dictionary<string, object>());
        AppearanceViewModel vm = Create(theme: settings);

        Assert.Equal(ThemeSettings.DefaultScrimOpacity, vm.ScrimOpacity);
        Assert.Equal("60%", vm.ScrimOpacityLabel);
    }

    [Fact]
    public void ScrimOpacity_SetClampedAndRounded_StoresNormalizedAndRaisesNotifications()
    {
        ThemeSettings settings = new(new Dictionary<string, object>());
        AppearanceViewModel vm = Create(theme: settings);
        List<string?> changed = [];
        vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        vm.ScrimOpacity = 1.4;

        Assert.Equal(1, settings.ScrimOpacity);
        Assert.Contains(nameof(AppearanceViewModel.ScrimOpacity), changed);
        Assert.Contains(nameof(AppearanceViewModel.ScrimOpacityLabel), changed);
    }

    [Fact]
    public void ScrimOpacity_SetSameValue_DoesNotRaiseNotifications()
    {
        ThemeSettings settings = new(new Dictionary<string, object>());
        AppearanceViewModel vm = Create(theme: settings);
        bool raised = false;
        vm.PropertyChanged += (_, _) => raised = true;

        vm.ScrimOpacity = ThemeSettings.DefaultScrimOpacity;

        Assert.False(raised);
    }

    [Fact]
    public void ScrimOpacity_SetNonFinite_KeepsStoredValue()
    {
        ThemeSettings settings = new(new Dictionary<string, object>());
        AppearanceViewModel vm = Create(theme: settings);

        vm.ScrimOpacity = double.NaN;

        Assert.Equal(ThemeSettings.DefaultScrimOpacity, settings.ScrimOpacity);
    }
}
