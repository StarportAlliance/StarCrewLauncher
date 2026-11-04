using StarCrew.Launcher.Models;

namespace StarCrew.Launcher.Tests.Models;

public sealed class BuiltInBackgroundCatalogTests
{
    [Fact]
    public void All_ContainsUniqueIdsAndAssetSources()
    {
        Assert.Equal(4, BuiltInBackgroundCatalog.All.Count);
        Assert.Equal(
            BuiltInBackgroundCatalog.All.Count,
            BuiltInBackgroundCatalog.All.Select(background => background.Id).Distinct().Count()
        );
        Assert.Equal(
            BuiltInBackgroundCatalog.All.Count,
            BuiltInBackgroundCatalog
                .All.Select(background => background.ImageSource)
                .Distinct()
                .Count()
        );
        Assert.All(
            BuiltInBackgroundCatalog.All,
            background =>
            {
                Assert.False(string.IsNullOrWhiteSpace(background.NameKey));
                Assert.StartsWith(
                    "ms-appx:///Assets/BuiltinBackground/",
                    background.ImageSource,
                    StringComparison.Ordinal
                );
            }
        );
    }

    [Theory]
    [InlineData(null, "FungleSunset")]
    [InlineData("Unknown", "FungleSunset")]
    [InlineData("impostormonth", "ImpostorMonth")]
    public void Resolve_ReturnsMatchOrDefault(string? id, string expected)
    {
        Assert.Equal(expected, BuiltInBackgroundCatalog.Resolve(id).Id);
    }
}
