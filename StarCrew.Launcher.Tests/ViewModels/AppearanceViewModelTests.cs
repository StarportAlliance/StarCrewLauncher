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
        ISystemFontCatalog? catalog = null
    )
    {
        ISystemFontCatalog fonts = catalog ?? Substitute.For<ISystemFontCatalog>();
        fonts.GetFonts().Returns(SampleFonts);
        return new AppearanceViewModel(
            theme ?? Substitute.For<IThemeSettings>(),
            fonts,
            TestStrings.Instance
        );
    }

    [Fact]
    public void Ctor_WithNullThemeSettings_ThrowsArgumentNullException() =>
        Assert.Throws<ArgumentNullException>(() =>
            new AppearanceViewModel(
                null!,
                Substitute.For<ISystemFontCatalog>(),
                TestStrings.Instance
            )
        );

    [Fact]
    public void Ctor_WithNullFontCatalog_ThrowsArgumentNullException() =>
        Assert.Throws<ArgumentNullException>(() =>
            new AppearanceViewModel(Substitute.For<IThemeSettings>(), null!, TestStrings.Instance)
        );

    [Fact]
    public void Ctor_WithNullStrings_ThrowsArgumentNullException() =>
        Assert.Throws<ArgumentNullException>(() =>
            new AppearanceViewModel(
                Substitute.For<IThemeSettings>(),
                Substitute.For<ISystemFontCatalog>(),
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
}
