using StarCrew.Launcher.Services;

namespace StarCrew.Launcher.Tests.Services;

public sealed class AccentPaletteTests
{
    private static int Sum(AccentRgb rgb) => rgb.R + rgb.G + rgb.B;

    [Theory]
    [InlineData("#1A83CB", 26, 131, 203)]
    [InlineData("1A83CB", 26, 131, 203)]
    [InlineData("#1a83cb", 26, 131, 203)]
    public void TryParse_WithValidHex_ReturnsRgb(string hex, byte r, byte g, byte b)
    {
        bool parsed = AccentPalette.TryParse(hex, out AccentRgb rgb);

        Assert.True(parsed);
        Assert.Equal(new AccentRgb(r, g, b), rgb);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("#12345")]
    [InlineData("#1234567")]
    [InlineData("#GGGGGG")]
    [InlineData("blue")]
    public void TryParse_WithInvalidHex_ReturnsFalse(string hex)
    {
        bool parsed = AccentPalette.TryParse(hex, out AccentRgb rgb);

        Assert.False(parsed);
        Assert.Equal(default, rgb);
    }

    [Fact]
    public void TryParse_WithNull_ReturnsFalse()
    {
        bool parsed = AccentPalette.TryParse(null, out _);

        Assert.False(parsed);
    }

    [Fact]
    public void ToHex_FormatsUppercaseWithHash()
    {
        string hex = AccentPalette.ToHex(new AccentRgb(26, 131, 203));

        Assert.Equal("#1A83CB", hex);
    }

    [Theory]
    [InlineData("1a83cb", "#1A83CB")]
    [InlineData("#1a83cb", "#1A83CB")]
    [InlineData(" 1A83CB ", "#1A83CB")]
    public void Normalize_WithValidHex_ReturnsCanonical(string input, string expected)
    {
        Assert.Equal(expected, AccentPalette.Normalize(input));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("oops")]
    [InlineData("#1234")]
    public void Normalize_WithInvalidHex_ReturnsNull(string? input)
    {
        Assert.Null(AccentPalette.Normalize(input));
    }

    [Fact]
    public void Derive_KeepsBaseColor()
    {
        AccentShades shades = AccentPalette.Derive("#1A83CB");

        Assert.Equal("#1A83CB", AccentPalette.ToHex(shades.Base));
    }

    [Fact]
    public void Derive_WithSaturatedColor_LightShadesGrowAndDarkShadesShrink()
    {
        AccentShades shades = AccentPalette.Derive("#1A83CB");

        Assert.True(Sum(shades.Light1) > Sum(shades.Base));
        Assert.True(Sum(shades.Light2) > Sum(shades.Light1));
        Assert.True(Sum(shades.Light3) > Sum(shades.Light2));
        Assert.True(Sum(shades.Dark1) < Sum(shades.Base));
        Assert.True(Sum(shades.Dark2) < Sum(shades.Dark1));
        Assert.True(Sum(shades.Dark3) < Sum(shades.Dark2));
    }

    [Fact]
    public void Derive_WithBlack_ReturnsGrayShadesWithoutThrowing()
    {
        AccentShades shades = AccentPalette.Derive("#000000");

        Assert.Equal(0, shades.Base.R);
        AssertGray(shades.Light1);
        AssertGray(shades.Light3);
        AssertGray(shades.Dark3);
        Assert.True(Sum(shades.Light3) > Sum(shades.Base));
    }

    [Fact]
    public void Derive_WithGray_KeepsChannelsBalanced()
    {
        AccentShades shades = AccentPalette.Derive("#808080");

        AssertGray(shades.Base);
        AssertGray(shades.Light1);
        AssertGray(shades.Light2);
        AssertGray(shades.Light3);
        AssertGray(shades.Dark1);
        AssertGray(shades.Dark2);
        AssertGray(shades.Dark3);
    }

    [Theory]
    [InlineData("oops")]
    [InlineData("")]
    [InlineData("#12345")]
    public void Derive_WithInvalidHex_ThrowsArgumentException(string hex)
    {
        Assert.Throws<ArgumentException>(() => AccentPalette.Derive(hex));
    }

    private static void AssertGray(AccentRgb rgb)
    {
        Assert.Equal(rgb.R, rgb.G);
        Assert.Equal(rgb.G, rgb.B);
    }
}
