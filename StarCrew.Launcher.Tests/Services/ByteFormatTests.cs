using StarCrew.Launcher.Services;

namespace StarCrew.Launcher.Tests.Services;

public sealed class ByteFormatTests
{
    [Theory]
    [InlineData(0, "0 B")]
    [InlineData(999, "999 B")]
    [InlineData(1023, "1023 B")]
    [InlineData(1024, "1 KB")]
    [InlineData(1048575, "1024 KB")]
    [InlineData(1048576, "1.0 MB")]
    [InlineData(3L * 1024 * 1024, "3.0 MB")]
    [InlineData(512L * 1024 * 1024, "512.0 MB")]
    [InlineData(1024L * 1024 * 1024, "1.00 GB")]
    [InlineData(5L * 1024 * 1024 * 1024, "5.00 GB")]
    public void Describe_AtUnitBoundary_FormatsExpected(long bytes, string expected) =>
        Assert.Equal(expected, ByteFormat.Describe(bytes));

    [Theory]
    [InlineData(0, "0 B/s")]
    [InlineData(2048, "2 KB/s")]
    [InlineData(3L * 1024 * 1024, "3.0 MB/s")]
    [InlineData(1024L * 1024 * 1024, "1.00 GB/s")]
    public void Rate_AppendsPerSecond(double bytesPerSecond, string expected) =>
        Assert.Equal(expected, ByteFormat.Rate(bytesPerSecond));
}
