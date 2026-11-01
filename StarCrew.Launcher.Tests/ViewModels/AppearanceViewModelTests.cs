using NSubstitute;
using StarCrew.Launcher.Models;
using StarCrew.Launcher.Services;
using StarCrew.Launcher.ViewModels;

namespace StarCrew.Launcher.Tests.ViewModels;

public sealed class AppearanceViewModelTests
{
    private static AppearanceViewModel Create(IThemeSettings? theme = null) =>
        new(theme ?? Substitute.For<IThemeSettings>());

    [Fact]
    public void Ctor_WithNullThemeSettings_ThrowsArgumentNullException() =>
        Assert.Throws<ArgumentNullException>(() => new AppearanceViewModel(null!));

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
        AppearanceViewModel vm = new(new ThemeSettings(new Dictionary<string, object>()));

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
        AppearanceViewModel vm = new(settings);
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
        AppearanceViewModel vm = new(settings);
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
        AppearanceViewModel vm = new(settings);

        vm.LightAccentColor = "#123456";
        vm.LightAccentColor = AccentPalette.DefaultLight;

        Assert.Null(settings.LightAccentColor);
        Assert.False(vm.CanResetLightAccent);
    }

    [Fact]
    public void LightAccentColor_SetInvalid_KeepsStoredValue()
    {
        ThemeSettings settings = new(new Dictionary<string, object>());
        AppearanceViewModel vm = new(settings);

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
        AppearanceViewModel vm = new(settings);
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
    public void ResetDarkAccent_WhenNotCustomized_DoesNothing()
    {
        ThemeSettings settings = new(new Dictionary<string, object>());
        AppearanceViewModel vm = new(settings);
        List<string?> changed = [];
        vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        vm.ResetDarkAccentCommand.Execute(null);

        Assert.Equal(AccentPalette.DefaultDark, vm.DarkAccentColor);
        Assert.False(vm.CanResetDarkAccent);
        Assert.Empty(changed);
    }
}
