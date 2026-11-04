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

    [Fact]
    public void DisplayFont_WithEmptyStore_ReturnsNull()
    {
        ThemeSettings settings = new(new Dictionary<string, object>());

        Assert.Null(settings.DisplayFont);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(42)]
    public void DisplayFont_WithInvalidStoredValue_ReturnsNull(object stored)
    {
        Dictionary<string, object> store = new() { [ThemeSettings.DisplayFontKey] = stored };
        ThemeSettings settings = new(store);

        Assert.Null(settings.DisplayFont);
    }

    [Fact]
    public void DisplayFont_SetTrimsBeforeStoring()
    {
        Dictionary<string, object> store = [];
        ThemeSettings settings = new(store);

        settings.DisplayFont = "  Microsoft YaHei  ";

        Assert.Equal("Microsoft YaHei", store[ThemeSettings.DisplayFontKey]);
        Assert.Equal("Microsoft YaHei", settings.DisplayFont);
    }

    [Fact]
    public void DisplayFont_SetSameValue_DoesNotRaiseChanged()
    {
        Dictionary<string, object> store = new() { [ThemeSettings.DisplayFontKey] = "SimSun" };
        ThemeSettings settings = new(store);
        bool raised = false;
        settings.DisplayFontChanged += (_, _) => raised = true;

        settings.DisplayFont = "SimSun";

        Assert.False(raised);
    }

    [Fact]
    public void DisplayFont_WhenChanged_RaisesOnce()
    {
        ThemeSettings settings = new(new Dictionary<string, object>());
        int raised = 0;
        settings.DisplayFontChanged += (_, _) => raised++;

        settings.DisplayFont = "SimSun";
        settings.DisplayFont = "Microsoft YaHei";

        Assert.Equal(2, raised);
    }

    [Fact]
    public void DisplayFont_SetNull_RemovesKeyAndRaises()
    {
        Dictionary<string, object> store = new() { [ThemeSettings.DisplayFontKey] = "SimSun" };
        ThemeSettings settings = new(store);
        bool raised = false;
        settings.DisplayFontChanged += (_, _) => raised = true;

        settings.DisplayFont = null;

        Assert.False(store.ContainsKey(ThemeSettings.DisplayFontKey));
        Assert.Null(settings.DisplayFont);
        Assert.True(raised);
    }

    [Fact]
    public void DisplayFont_SetWhitespace_ThrowsArgumentException()
    {
        ThemeSettings settings = new(new Dictionary<string, object>());

        Assert.Throws<ArgumentException>(() => settings.DisplayFont = "   ");
    }

    [Fact]
    public void Background_WithEmptyStore_ReturnsImage()
    {
        ThemeSettings settings = new(new Dictionary<string, object>());

        Assert.Equal(BackgroundKind.Image, settings.Background);
    }

    [Fact]
    public void Background_RoundTrip_PersistsString()
    {
        Dictionary<string, object> store = [];
        ThemeSettings settings = new(store) { Background = BackgroundKind.Mica };

        Assert.Equal(BackgroundKind.Mica, settings.Background);
        Assert.Equal("Mica", store[ThemeSettings.BackgroundKey]);
    }

    [Theory]
    [InlineData("Mica", "Mica")]
    [InlineData("solidcolor", "SolidColor")]
    [InlineData("IMAGE", "Image")]
    public void Background_WithStoredString_ReturnsParsed(string stored, string expectedName)
    {
        Dictionary<string, object> store = new() { [ThemeSettings.BackgroundKey] = stored };
        ThemeSettings settings = new(store);

        Assert.Equal(
            Enum.Parse<BackgroundKind>(expectedName, ignoreCase: true),
            settings.Background
        );
    }

    [Theory]
    [InlineData("Transparent")]
    [InlineData("")]
    [InlineData(42)]
    public void Background_WithInvalidStoredValue_ReturnsImage(object stored)
    {
        Dictionary<string, object> store = new() { [ThemeSettings.BackgroundKey] = stored };
        ThemeSettings settings = new(store);

        Assert.Equal(BackgroundKind.Image, settings.Background);
    }

    [Fact]
    public void Background_SetUndefined_ThrowsArgumentOutOfRange()
    {
        ThemeSettings settings = new(new Dictionary<string, object>());

        Assert.Throws<ArgumentOutOfRangeException>(() => settings.Background = (BackgroundKind)99);
    }

    [Fact]
    public void Background_WhenChanged_RaisesBackgroundChangedOnce()
    {
        ThemeSettings settings = new(new Dictionary<string, object>());
        int raised = 0;
        settings.BackgroundChanged += (_, _) => raised++;

        settings.Background = BackgroundKind.SolidColor;
        settings.Background = BackgroundKind.SolidColor;

        Assert.Equal(1, raised);
    }

    [Fact]
    public void SolidBackgroundColor_WithEmptyStore_ReturnsNull()
    {
        ThemeSettings settings = new(new Dictionary<string, object>());

        Assert.Null(settings.SolidBackgroundColor);
    }

    [Fact]
    public void SolidBackgroundColor_SetNormalizesBeforeStoring()
    {
        Dictionary<string, object> store = [];
        ThemeSettings settings = new(store);

        settings.SolidBackgroundColor = "a1b2c3";

        Assert.Equal("#A1B2C3", store[ThemeSettings.SolidBackgroundColorKey]);
        Assert.Equal("#A1B2C3", settings.SolidBackgroundColor);
    }

    [Fact]
    public void SolidBackgroundColor_SetInvalid_ThrowsArgumentException()
    {
        ThemeSettings settings = new(new Dictionary<string, object>());

        Assert.Throws<ArgumentException>(() => settings.SolidBackgroundColor = "oops");
    }

    [Fact]
    public void SolidBackgroundColor_SetNull_RemovesKeyAndRaises()
    {
        Dictionary<string, object> store = new()
        {
            [ThemeSettings.SolidBackgroundColorKey] = "#A1B2C3",
        };
        ThemeSettings settings = new(store);
        bool raised = false;
        settings.BackgroundChanged += (_, _) => raised = true;

        settings.SolidBackgroundColor = null;

        Assert.False(store.ContainsKey(ThemeSettings.SolidBackgroundColorKey));
        Assert.Null(settings.SolidBackgroundColor);
        Assert.True(raised);
    }

    [Fact]
    public void BackgroundImagePath_WithEmptyStore_ReturnsNull()
    {
        ThemeSettings settings = new(new Dictionary<string, object>());

        Assert.Null(settings.BackgroundImagePath);
    }

    [Fact]
    public void BackgroundImagePath_SetTrimsBeforeStoring()
    {
        Dictionary<string, object> store = [];
        ThemeSettings settings = new(store);

        settings.BackgroundImagePath = @"  C:\data\background.png  ";

        Assert.Equal(@"C:\data\background.png", store[ThemeSettings.BackgroundImagePathKey]);
        Assert.Equal(@"C:\data\background.png", settings.BackgroundImagePath);
    }

    [Fact]
    public void BackgroundImagePath_SetWhitespace_ThrowsArgumentException()
    {
        ThemeSettings settings = new(new Dictionary<string, object>());

        Assert.Throws<ArgumentException>(() => settings.BackgroundImagePath = "   ");
    }

    [Fact]
    public void BackgroundImagePath_SetNull_RemovesKeyAndRaises()
    {
        Dictionary<string, object> store = new()
        {
            [ThemeSettings.BackgroundImagePathKey] = @"C:\data\background.png",
        };
        ThemeSettings settings = new(store);
        bool raised = false;
        settings.BackgroundChanged += (_, _) => raised = true;

        settings.BackgroundImagePath = null;

        Assert.False(store.ContainsKey(ThemeSettings.BackgroundImagePathKey));
        Assert.Null(settings.BackgroundImagePath);
        Assert.True(raised);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(42)]
    public void BackgroundImagePath_WithInvalidStoredValue_ReturnsNull(object stored)
    {
        Dictionary<string, object> store = new()
        {
            [ThemeSettings.BackgroundImagePathKey] = stored,
        };
        ThemeSettings settings = new(store);

        Assert.Null(settings.BackgroundImagePath);
    }

    [Fact]
    public void SolidBackgroundFollowsTheme_WithEmptyStore_ReturnsTrue()
    {
        ThemeSettings settings = new(new Dictionary<string, object>());

        Assert.True(settings.SolidBackgroundFollowsTheme);
    }

    [Fact]
    public void SolidBackgroundFollowsTheme_WithStoredColorButNoFlag_ReturnsFalse()
    {
        Dictionary<string, object> store = new()
        {
            [ThemeSettings.SolidBackgroundColorKey] = "#123456",
        };
        ThemeSettings settings = new(store);

        Assert.False(settings.SolidBackgroundFollowsTheme);
    }

    [Fact]
    public void SolidBackgroundFollowsTheme_RoundTrip_PersistsBool()
    {
        Dictionary<string, object> store = [];
        ThemeSettings settings = new(store) { SolidBackgroundFollowsTheme = false };

        Assert.False(settings.SolidBackgroundFollowsTheme);
        Assert.Equal(false, store[ThemeSettings.SolidBackgroundFollowsThemeKey]);
    }

    [Fact]
    public void SolidBackgroundFollowsTheme_WhenChanged_RaisesBackgroundChangedOnce()
    {
        ThemeSettings settings = new(new Dictionary<string, object>());
        int raised = 0;
        settings.BackgroundChanged += (_, _) => raised++;

        settings.SolidBackgroundFollowsTheme = false;
        settings.SolidBackgroundFollowsTheme = false;

        Assert.Equal(1, raised);
    }

    [Fact]
    public void UseCustomBackgroundImage_WithEmptyStore_ReturnsFalse()
    {
        ThemeSettings settings = new(new Dictionary<string, object>());

        Assert.False(settings.UseCustomBackgroundImage);
    }

    [Fact]
    public void UseCustomBackgroundImage_WithStoredPathButNoFlag_ReturnsTrue()
    {
        Dictionary<string, object> store = new()
        {
            [ThemeSettings.BackgroundImagePathKey] = @"C:\data\background.png",
        };
        ThemeSettings settings = new(store);

        Assert.True(settings.UseCustomBackgroundImage);
    }

    [Fact]
    public void UseCustomBackgroundImage_RoundTrip_PersistsBool()
    {
        Dictionary<string, object> store = [];
        ThemeSettings settings = new(store) { UseCustomBackgroundImage = true };

        Assert.True(settings.UseCustomBackgroundImage);
        Assert.Equal(true, store[ThemeSettings.UseCustomBackgroundImageKey]);
    }

    [Fact]
    public void UseCustomBackgroundImage_WhenChanged_RaisesBackgroundChangedOnce()
    {
        ThemeSettings settings = new(new Dictionary<string, object>());
        int raised = 0;
        settings.BackgroundChanged += (_, _) => raised++;

        settings.UseCustomBackgroundImage = true;
        settings.UseCustomBackgroundImage = true;

        Assert.Equal(1, raised);
    }

    [Fact]
    public void BuiltInBackgroundId_WithEmptyStore_ReturnsNull()
    {
        ThemeSettings settings = new(new Dictionary<string, object>());

        Assert.Null(settings.BuiltInBackgroundId);
    }

    [Fact]
    public void BuiltInBackgroundId_SetKnownId_PersistsNormalizedAndRaises()
    {
        Dictionary<string, object> store = [];
        ThemeSettings settings = new(store);
        int raised = 0;
        settings.BackgroundChanged += (_, _) => raised++;

        settings.BuiltInBackgroundId = "  crewmateduo  ";

        Assert.Equal("CrewmateDuo", store[ThemeSettings.BuiltInBackgroundIdKey]);
        Assert.Equal("CrewmateDuo", settings.BuiltInBackgroundId);
        Assert.Equal(1, raised);
    }

    [Fact]
    public void BuiltInBackgroundId_SetDefaultId_RemovesKey()
    {
        Dictionary<string, object> store = new()
        {
            [ThemeSettings.BuiltInBackgroundIdKey] = "CrewmateDuo",
        };
        ThemeSettings settings = new(store);

        settings.BuiltInBackgroundId = BuiltInBackgroundCatalog.Default.Id;

        Assert.False(store.ContainsKey(ThemeSettings.BuiltInBackgroundIdKey));
        Assert.Null(settings.BuiltInBackgroundId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Unknown")]
    public void BuiltInBackgroundId_SetInvalid_ThrowsArgumentException(string value)
    {
        ThemeSettings settings = new(new Dictionary<string, object>());

        Assert.Throws<ArgumentException>(() => settings.BuiltInBackgroundId = value);
    }

    [Theory]
    [InlineData("")]
    [InlineData(42)]
    [InlineData("Unknown")]
    public void BuiltInBackgroundId_WithInvalidStoredValue_ReturnsNull(object stored)
    {
        Dictionary<string, object> store = new()
        {
            [ThemeSettings.BuiltInBackgroundIdKey] = stored,
        };
        ThemeSettings settings = new(store);

        Assert.Null(settings.BuiltInBackgroundId);
    }

    [Fact]
    public void BuiltInBackgroundId_SetNull_RemovesKeyAndRaises()
    {
        Dictionary<string, object> store = new()
        {
            [ThemeSettings.BuiltInBackgroundIdKey] = "CrewmateDuo",
        };
        ThemeSettings settings = new(store);
        bool raised = false;
        settings.BackgroundChanged += (_, _) => raised = true;

        settings.BuiltInBackgroundId = null;

        Assert.False(store.ContainsKey(ThemeSettings.BuiltInBackgroundIdKey));
        Assert.Null(settings.BuiltInBackgroundId);
        Assert.True(raised);
    }

    [Fact]
    public void ScrimOpacity_WithEmptyStore_ReturnsDefault()
    {
        ThemeSettings settings = new(new Dictionary<string, object>());

        Assert.Equal(ThemeSettings.DefaultScrimOpacity, settings.ScrimOpacity);
    }

    [Theory]
    [InlineData(1.5, 1.0)]
    [InlineData(-0.2, 0.0)]
    [InlineData(0.432, 0.43)]
    public void ScrimOpacity_SetClampsAndRoundsBeforeStoring(double stored, double expected)
    {
        Dictionary<string, object> store = [];
        ThemeSettings settings = new(store);

        settings.ScrimOpacity = stored;

        Assert.Equal(expected, settings.ScrimOpacity);
        Assert.Equal(expected, store[ThemeSettings.ScrimOpacityKey]);
    }

    [Theory]
    [InlineData("oops")]
    [InlineData("true")]
    public void ScrimOpacity_WithInvalidStoredValue_ReturnsDefault(object stored)
    {
        Dictionary<string, object> store = new() { [ThemeSettings.ScrimOpacityKey] = stored };
        ThemeSettings settings = new(store);

        Assert.Equal(ThemeSettings.DefaultScrimOpacity, settings.ScrimOpacity);
    }

    [Fact]
    public void ScrimOpacity_WithOutOfRangeStoredValue_Clamps()
    {
        Dictionary<string, object> store = new() { [ThemeSettings.ScrimOpacityKey] = 2.5 };
        ThemeSettings settings = new(store);

        Assert.Equal(1, settings.ScrimOpacity);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void ScrimOpacity_SetNonFinite_ThrowsArgumentOutOfRange(double value)
    {
        ThemeSettings settings = new(new Dictionary<string, object>());

        Assert.Throws<ArgumentOutOfRangeException>(() => settings.ScrimOpacity = value);
    }

    [Fact]
    public void ScrimOpacity_WhenChanged_RaisesScrimOpacityChangedOnce()
    {
        ThemeSettings settings = new(new Dictionary<string, object>());
        int raised = 0;
        settings.ScrimOpacityChanged += (_, _) => raised++;

        settings.ScrimOpacity = 0.2;
        settings.ScrimOpacity = 0.2;

        Assert.Equal(1, raised);
    }
}
