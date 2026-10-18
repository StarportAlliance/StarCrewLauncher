using StarCrew.Launcher.Services;

namespace StarCrew.Launcher.Tests.Services;

public sealed class QrAsciiParserTests
{
    [Fact]
    public void TryParse_WithSquareBlock_ReturnsModuleMatrix()
    {
        bool[][] expected = BuildCheckerMatrix(29);

        bool parsed = QrAsciiParser.TryParse(ToAsciiLines(expected), out bool[][]? matrix);

        Assert.True(parsed);
        Assert.NotNull(matrix);
        Assert.Equal(expected.Length, matrix!.Length);
        Assert.Equal(expected, matrix);
    }

    [Fact]
    public void TryParse_WithAllDarkModules_ReturnsFullMatrix()
    {
        bool[][] expected =
        [
            .. Enumerable.Range(0, 29).Select(_ => Enumerable.Repeat(true, 29).ToArray()),
        ];

        bool parsed = QrAsciiParser.TryParse(ToAsciiLines(expected), out bool[][]? matrix);

        Assert.True(parsed);
        Assert.Equal(expected, matrix);
    }

    [Fact]
    public void TryParse_WithTooFewLines_ReturnsFalse()
    {
        bool[][] tiny = BuildCheckerMatrix(20);

        bool parsed = QrAsciiParser.TryParse(ToAsciiLines(tiny), out bool[][]? matrix);

        Assert.False(parsed);
        Assert.Null(matrix);
    }

    [Fact]
    public void TryParse_WithNonSquareLines_ReturnsFalse()
    {
        List<string> lines = [.. ToAsciiLines(BuildCheckerMatrix(29)), "short"];

        bool parsed = QrAsciiParser.TryParse(lines, out bool[][]? matrix);

        Assert.False(parsed);
        Assert.Null(matrix);
    }

    [Fact]
    public void TryParse_WithForeignCharacters_ReturnsFalse()
    {
        List<string> lines = [.. ToAsciiLines(BuildCheckerMatrix(29))];
        lines[3] = lines[3].Replace("█", "▀", StringComparison.Ordinal);

        bool parsed = QrAsciiParser.TryParse(lines, out bool[][]? matrix);

        Assert.False(parsed);
        Assert.Null(matrix);
    }

    [Fact]
    public void TryParse_WithStatusLines_ReturnsFalse()
    {
        string[] lines = ["Use the Steam Mobile App to sign in:", "Logging in with QR code..."];

        bool parsed = QrAsciiParser.TryParse(lines, out bool[][]? matrix);

        Assert.False(parsed);
        Assert.Null(matrix);
    }

    private static bool[][] BuildCheckerMatrix(int side) =>
        [
            .. Enumerable
                .Range(0, side)
                .Select(y => Enumerable.Range(0, side).Select(x => (x + y) % 2 == 0).ToArray()),
        ];

    private static string[] ToAsciiLines(bool[][] matrix) =>
        [.. matrix.Select(row => string.Concat(row.Select(dark => dark ? "██" : "  ")))];
}
