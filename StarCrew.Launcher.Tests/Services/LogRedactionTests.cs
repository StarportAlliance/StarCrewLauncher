using StarCrew.Launcher.Services;

namespace StarCrew.Launcher.Tests.Services;

public sealed class LogRedactionTests
{
    [Theory]
    [InlineData("", "")]
    [InlineData("a", "*")]
    [InlineData("ab", "**")]
    [InlineData("abc", "a*c")]
    [InlineData("abcde", "a***e")]
    [InlineData("abcdef", "abc*ef")]
    [InlineData("abcdefg", "abc**fg")]
    public void MaskAccountName_MasksMiddleByLength(string input, string expected) =>
        Assert.Equal(expected, LogRedaction.MaskAccountName(input));

    [Fact]
    public void MaskAccountName_WithNull_ThrowsArgumentNullException() =>
        Assert.Throws<ArgumentNullException>(() => LogRedaction.MaskAccountName(null!));

    [Theory]
    [InlineData("-username abcdefg", "-username abc**fg")]
    [InlineData("retry: -username abcdef -qr", "retry: -username abc*ef -qr")]
    [InlineData("nothing to hide", "nothing to hide")]
    public void Scrub_MasksUsernameToken(string line, string expected) =>
        Assert.Equal(expected, LogRedaction.Scrub(line));

    [Fact]
    public void Scrub_WithNull_ThrowsArgumentNullException() =>
        Assert.Throws<ArgumentNullException>(() => LogRedaction.Scrub(null!));
}
