using StarCrew.Launcher.Services;

namespace StarCrew.Launcher.Tests.Services;

public sealed class DepotDownloaderLocatorTests : IDisposable
{
    private readonly string _directory = Path.Combine(
        Path.GetTempPath(),
        Guid.NewGuid().ToString("N")
    );

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    [Fact]
    public void Find_WhenExecutableMissing_ReturnsNull() =>
        Assert.Null(new DepotDownloaderLocator(_directory).Find());

    [Fact]
    public void Find_WhenExecutablePresent_ReturnsPath()
    {
        Directory.CreateDirectory(_directory);
        string expected = Path.Combine(_directory, "DepotDownloader.exe");
        File.WriteAllBytes(expected, []);

        Assert.Equal(expected, new DepotDownloaderLocator(_directory).Find());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Ctor_WithBlankDirectory_ThrowsArgumentException(string directory) =>
        Assert.Throws<ArgumentException>(() => new DepotDownloaderLocator(directory));
}
