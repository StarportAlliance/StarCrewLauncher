using Serilog.Events;
using StarCrew.Launcher.Services;

namespace StarCrew.Launcher.Tests.Services;

public sealed class LoggingSettingsTests
{
    [Fact]
    public void Defaults_WhenStoreEmpty_ReturnsExpectedValues()
    {
        LoggingSettings settings = new(new Dictionary<string, object>());

        Assert.True(settings.Enabled);
        Assert.Equal(LogEventLevel.Information, settings.Level);
        Assert.Equal(7, settings.RetentionDays);
    }

    [Fact]
    public void SaveThenLoad_RoundTripsAllFields()
    {
        Dictionary<string, object> store = [];
        LoggingSettings settings = new(store);

        settings.Enabled = false;
        settings.Level = LogEventLevel.Debug;
        settings.RetentionDays = 30;

        Assert.False(new LoggingSettings(store).Enabled);
        Assert.Equal(LogEventLevel.Debug, new LoggingSettings(store).Level);
        Assert.Equal(30, new LoggingSettings(store).RetentionDays);
    }

    [Theory]
    [InlineData("true", true)]
    [InlineData("false", false)]
    [InlineData(true, true)]
    [InlineData(false, false)]
    [InlineData(42, true)]
    public void Enabled_ParsesStoredValue(object raw, bool expected)
    {
        LoggingSettings settings = new(
            new Dictionary<string, object> { [LoggingSettings.EnabledKey] = raw }
        );

        Assert.Equal(expected, settings.Enabled);
    }

    [Theory]
    [InlineData("debug", LogEventLevel.Debug)]
    [InlineData("Fatal", LogEventLevel.Fatal)]
    [InlineData("nope", LogEventLevel.Information)]
    [InlineData(3, LogEventLevel.Information)]
    public void Level_ParsesStoredValueOrFallsBack(object raw, LogEventLevel expected)
    {
        LoggingSettings settings = new(
            new Dictionary<string, object> { [LoggingSettings.LevelKey] = raw }
        );

        Assert.Equal(expected, settings.Level);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(14.0, 14)]
    [InlineData("30", 30)]
    [InlineData(0, 7)]
    [InlineData(-5, 7)]
    [InlineData("abc", 7)]
    public void RetentionDays_ParsesStoredValueOrFallsBack(object raw, int expected)
    {
        LoggingSettings settings = new(
            new Dictionary<string, object> { [LoggingSettings.RetentionDaysKey] = raw }
        );

        Assert.Equal(expected, settings.RetentionDays);
    }

    [Fact]
    public void RetentionDays_SetBelowOne_ThrowsArgumentOutOfRange()
    {
        LoggingSettings settings = new(new Dictionary<string, object>());

        Assert.Throws<ArgumentOutOfRangeException>(() => settings.RetentionDays = 0);
    }

    [Fact]
    public void Level_SetUndefined_ThrowsArgumentOutOfRange()
    {
        LoggingSettings settings = new(new Dictionary<string, object>());

        Assert.Throws<ArgumentOutOfRangeException>(() => settings.Level = (LogEventLevel)999);
    }
}
