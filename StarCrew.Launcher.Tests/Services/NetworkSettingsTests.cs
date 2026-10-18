using StarCrew.Launcher.Services;

namespace StarCrew.Launcher.Tests.Services;

public sealed class NetworkSettingsTests
{
    [Fact]
    public void Ctor_WithNullStore_ThrowsArgumentNullException() =>
        Assert.Throws<ArgumentNullException>(() => new NetworkSettings(null!));

    [Fact]
    public void Accelerator_WithEmptyStore_ReturnsGhProxy()
    {
        NetworkSettings settings = new(new Dictionary<string, object>());

        Assert.Equal(DownloadAccelerator.GhProxy, settings.Accelerator);
    }

    [Theory]
    [InlineData("Official")]
    [InlineData("GhProxy")]
    [InlineData("Custom")]
    public void Accelerator_RoundTrip_PersistsString(string acceleratorName)
    {
        DownloadAccelerator accelerator = Enum.Parse<DownloadAccelerator>(acceleratorName);
        Dictionary<string, object> store = new();
        NetworkSettings settings = new(store);

        settings.Accelerator = accelerator;

        Assert.Equal(accelerator, settings.Accelerator);
        Assert.Equal(accelerator.ToString(), store["DownloadAccelerator"]);
    }

    [Theory]
    [InlineData("Official")]
    [InlineData("official")]
    [InlineData("CUSTOM")]
    public void Accelerator_WithStoredString_ReturnsParsed(string stored)
    {
        Dictionary<string, object> store = new() { ["DownloadAccelerator"] = stored };
        NetworkSettings settings = new(store);
        DownloadAccelerator expected = Enum.Parse<DownloadAccelerator>(stored, ignoreCase: true);

        Assert.Equal(expected, settings.Accelerator);
    }

    [Theory]
    [InlineData("Turbo")]
    [InlineData("")]
    [InlineData("   ")]
    public void Accelerator_WithInvalidString_ReturnsGhProxy(string stored)
    {
        Dictionary<string, object> store = new() { ["DownloadAccelerator"] = stored };
        NetworkSettings settings = new(store);

        Assert.Equal(DownloadAccelerator.GhProxy, settings.Accelerator);
    }

    [Fact]
    public void Accelerator_WithNonStringValue_ReturnsGhProxy()
    {
        Dictionary<string, object> store = new() { ["DownloadAccelerator"] = 42 };
        NetworkSettings settings = new(store);

        Assert.Equal(DownloadAccelerator.GhProxy, settings.Accelerator);
    }

    [Fact]
    public void Accelerator_SetUndefined_ThrowsArgumentOutOfRange()
    {
        NetworkSettings settings = new(new Dictionary<string, object>());

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            settings.Accelerator = (DownloadAccelerator)99
        );
    }

    [Fact]
    public void CustomPrefix_WithEmptyStore_ReturnsEmpty()
    {
        NetworkSettings settings = new(new Dictionary<string, object>());

        Assert.Equal(string.Empty, settings.CustomPrefix);
    }

    [Fact]
    public void CustomPrefix_RoundTrip_PersistsValue()
    {
        Dictionary<string, object> store = [];
        NetworkSettings settings = new(store) { CustomPrefix = "https://acc.example.com" };

        Assert.Equal("https://acc.example.com", settings.CustomPrefix);
        Assert.Equal("https://acc.example.com", store["DownloadAcceleratorCustomPrefix"]);
    }

    [Fact]
    public void CustomPrefix_SetNull_StoresEmpty()
    {
        NetworkSettings settings = new(new Dictionary<string, object>()) { CustomPrefix = null! };

        Assert.Equal(string.Empty, settings.CustomPrefix);
    }
}
