using StarCrew.Launcher.Models;
using StarCrew.Launcher.Services;

namespace StarCrew.Launcher.Tests.Services;

/// <summary>ThemeSettings 的单元测试：以内存字典代替 LocalSettings，无 WinRT 依赖。</summary>
public sealed class ThemeSettingsTests
{
    [Fact]
    public void Ctor_WithNullStore_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new ThemeSettings(null!));
    }

    [Fact]
    public void Theme_WithEmptyStore_ReturnsSystem()
    {
        ThemeSettings settings = new(new Dictionary<string, object>());

        Assert.Equal(AppTheme.System, settings.Theme);
    }

    [Fact]
    public void Theme_RoundTrip_PersistsString()
    {
        Dictionary<string, object> store = new();
        ThemeSettings settings = new(store);

        settings.Theme = AppTheme.Dark;

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
}
