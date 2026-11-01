using StarCrew.Launcher.Models;
using StarCrew.Launcher.Services;

namespace StarCrew.Launcher.Tests.Services;

public sealed class ThemeSettingsTests
{
    [Fact]
    public void Ctor_WithNullStore_ThrowsArgumentNullException() =>
        Assert.Throws<ArgumentNullException>(() => new ThemeSettings(null!));

    [Fact]
    public void Theme_WithEmptyStore_ReturnsSystem()
    {
        ThemeSettings settings = new(new Dictionary<string, object>());

        Assert.Equal(AppTheme.System, settings.Theme);
    }

    [Fact]
    public void Theme_RoundTrip_PersistsString()
    {
        Dictionary<string, object> store = [];
        ThemeSettings settings = new(store) { Theme = AppTheme.Dark };

        Assert.Equal(AppTheme.Dark, settings.Theme);
        Assert.Equal("Dark", store[ThemeSettings.ThemeKey]);
    }

    [Theory]
    [InlineData("Light", "Light")]
    [InlineData("dark", "Dark")]
    [InlineData("SYSTEM", "System")]
    public void Theme_WithStoredString_ReturnsParsed(string stored, string expectedName)
    {
        Dictionary<string, object> store = new() { [ThemeSettings.ThemeKey] = stored };
        ThemeSettings settings = new(store);
        AppTheme expected = Enum.Parse<AppTheme>(expectedName, ignoreCase: true);

        Assert.Equal(expected, settings.Theme);
    }

    [Theory]
    [InlineData("Pink")]
    [InlineData("")]
    [InlineData("   ")]
    public void Theme_WithInvalidString_ReturnsSystem(string stored)
    {
        Dictionary<string, object> store = new() { [ThemeSettings.ThemeKey] = stored };
        ThemeSettings settings = new(store);

        Assert.Equal(AppTheme.System, settings.Theme);
    }

    [Fact]
    public void Theme_WithNonStringValue_ReturnsSystem()
    {
        Dictionary<string, object> store = new() { [ThemeSettings.ThemeKey] = 42 };
        ThemeSettings settings = new(store);

        Assert.Equal(AppTheme.System, settings.Theme);
    }

    [Fact]
    public void Theme_SetUndefined_ThrowsArgumentOutOfRange()
    {
        ThemeSettings settings = new(new Dictionary<string, object>());

        Assert.Throws<ArgumentOutOfRangeException>(() => settings.Theme = (AppTheme)99);
    }

    [Fact]
    public void Theme_WhenChanged_RaisesThemeChangedOnce()
    {
        ThemeSettings settings = new(new Dictionary<string, object>());
        List<AppTheme> received = [];
        settings.ThemeChanged += (_, theme) => received.Add(theme);

        settings.Theme = AppTheme.Dark;
        settings.Theme = AppTheme.Dark;

        Assert.Equal(new[] { AppTheme.Dark }, received);
    }

    [Fact]
    public void Theme_SetSameValue_DoesNotRaiseThemeChanged()
    {
        Dictionary<string, object> store = new() { [ThemeSettings.ThemeKey] = "Light" };
        ThemeSettings settings = new(store);
        bool raised = false;
        settings.ThemeChanged += (_, _) => raised = true;

        settings.Theme = AppTheme.Light;

        Assert.False(raised);
    }

    [Fact]
    public void LightAccentColor_WithEmptyStore_ReturnsNull()
    {
        ThemeSettings settings = new(new Dictionary<string, object>());

        Assert.Null(settings.LightAccentColor);
    }

    [Fact]
    public void DarkAccentColor_WithEmptyStore_ReturnsNull()
    {
        ThemeSettings settings = new(new Dictionary<string, object>());

        Assert.Null(settings.DarkAccentColor);
    }

    [Theory]
    [InlineData("1a83cb")]
    [InlineData("#1a83cb")]
    [InlineData(" 1A83CB ")]
    public void LightAccentColor_WithStoredString_ReturnsCanonical(string stored)
    {
        Dictionary<string, object> store = new() { [ThemeSettings.LightAccentColorKey] = stored };
        ThemeSettings settings = new(store);

        Assert.Equal("#1A83CB", settings.LightAccentColor);
    }

    [Theory]
    [InlineData("oops")]
    [InlineData("")]
    [InlineData(42)]
    public void LightAccentColor_WithInvalidStoredValue_ReturnsNull(object stored)
    {
        Dictionary<string, object> store = new() { [ThemeSettings.LightAccentColorKey] = stored };
        ThemeSettings settings = new(store);

        Assert.Null(settings.LightAccentColor);
    }

    [Fact]
    public void LightAccentColor_SetNormalizesBeforeStoring()
    {
        Dictionary<string, object> store = [];
        ThemeSettings settings = new(store);

        settings.LightAccentColor = "1a83cb";

        Assert.Equal("#1A83CB", store[ThemeSettings.LightAccentColorKey]);
        Assert.Equal("#1A83CB", settings.LightAccentColor);
    }

    [Fact]
    public void DarkAccentColor_UsesSeparateKey()
    {
        Dictionary<string, object> store = [];
        ThemeSettings settings = new(store);

        settings.DarkAccentColor = "#11AA22";

        Assert.Equal("#11AA22", store[ThemeSettings.DarkAccentColorKey]);
        Assert.Null(settings.LightAccentColor);
        Assert.Equal("#11AA22", settings.DarkAccentColor);
    }

    [Fact]
    public void AccentColorsChanged_WhenChanged_RaisesOnce()
    {
        ThemeSettings settings = new(new Dictionary<string, object>());
        int raised = 0;
        settings.AccentColorsChanged += (_, _) => raised++;

        settings.LightAccentColor = "#123456";
        settings.LightAccentColor = "123456";

        Assert.Equal(1, raised);
    }

    [Fact]
    public void AccentColorsChanged_WhenSameValue_DoesNotRaise()
    {
        ThemeSettings settings = new(new Dictionary<string, object>())
        {
            LightAccentColor = "#123456",
        };
        bool raised = false;
        settings.AccentColorsChanged += (_, _) => raised = true;

        settings.LightAccentColor = "#123456";

        Assert.False(raised);
    }

    [Fact]
    public void LightAccentColor_SetNull_RemovesKeyAndRaises()
    {
        Dictionary<string, object> store = new()
        {
            [ThemeSettings.LightAccentColorKey] = "#123456",
        };
        ThemeSettings settings = new(store);
        bool raised = false;
        settings.AccentColorsChanged += (_, _) => raised = true;

        settings.LightAccentColor = null;

        Assert.False(store.ContainsKey(ThemeSettings.LightAccentColorKey));
        Assert.Null(settings.LightAccentColor);
        Assert.True(raised);
    }

    [Theory]
    [InlineData("oops")]
    [InlineData("#12345")]
    public void AccentColor_SetInvalid_ThrowsArgumentException(string value)
    {
        ThemeSettings settings = new(new Dictionary<string, object>());

        Assert.Throws<ArgumentException>(() => settings.LightAccentColor = value);
    }
}
