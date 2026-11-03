using StarCrew.Launcher.Services;

namespace StarCrew.Launcher.Tests.Services;

public sealed class BackgroundImageStoreTests
{
    [Fact]
    public void Ctor_WithNullDirectory_ThrowsArgumentNullException() =>
        Assert.Throws<ArgumentNullException>(() => new BackgroundImageStore(null!));

    [Fact]
    public void Ctor_WithWhitespaceDirectory_ThrowsArgumentException() =>
        Assert.Throws<ArgumentException>(() => new BackgroundImageStore("   "));

    [Fact]
    public async Task ImportAsync_WithUnsupportedExtension_ThrowsArgumentException() =>
        await Assert.ThrowsAsync<ArgumentException>(() =>
            new BackgroundImageStore(CreateTempDirectory()).ImportAsync("picture.gif")
        );

    [Fact]
    public async Task ImportAsync_CopiesFileIntoDirectory()
    {
        string root = CreateTempDirectory();
        try
        {
            string source = Path.Combine(root, "photo.png");
            await File.WriteAllTextAsync(source, "pixels");
            BackgroundImageStore store = new(Path.Combine(root, "Background"));

            string destination = await store.ImportAsync(source);

            Assert.Equal(
                Path.Combine(root, "Background", "background.PNG"),
                Path.GetFullPath(destination)
            );
            Assert.True(File.Exists(destination));
            Assert.Equal("pixels", await File.ReadAllTextAsync(destination));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task ImportAsync_ReplacesPreviousCopy()
    {
        string root = CreateTempDirectory();
        try
        {
            string first = Path.Combine(root, "first.png");
            string second = Path.Combine(root, "second.jpg");
            await File.WriteAllTextAsync(first, "first");
            await File.WriteAllTextAsync(second, "second");
            BackgroundImageStore store = new(Path.Combine(root, "Background"));

            await store.ImportAsync(first);
            string destination = await store.ImportAsync(second);

            Assert.Equal("second", await File.ReadAllTextAsync(destination));
            Assert.Single(Directory.GetFiles(Path.Combine(root, "Background"), "background.*"));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task ImportAsync_WithSameDestinationPath_KeepsFile()
    {
        string root = CreateTempDirectory();
        try
        {
            string directory = Path.Combine(root, "Background");
            Directory.CreateDirectory(directory);
            string destination = Path.Combine(directory, "background.PNG");
            await File.WriteAllTextAsync(destination, "pixels");
            BackgroundImageStore store = new(directory);

            string result = await store.ImportAsync(destination);

            Assert.Equal(destination, result);
            Assert.Equal("pixels", await File.ReadAllTextAsync(destination));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Delete_RemovesFile()
    {
        string root = CreateTempDirectory();
        try
        {
            string file = Path.Combine(root, "background.png");
            File.WriteAllText(file, "pixels");
            BackgroundImageStore store = new(root);

            store.Delete(file);

            Assert.False(File.Exists(file));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Delete_WhenMissing_DoesNotThrow()
    {
        string root = CreateTempDirectory();
        try
        {
            BackgroundImageStore store = new(Path.Combine(root, "Background"));

            store.Delete(Path.Combine(root, "missing.png"));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static string CreateTempDirectory()
    {
        string directory = Path.Combine(
            Path.GetTempPath(),
            "StarCrewLauncherTests",
            Guid.NewGuid().ToString("N")
        );
        Directory.CreateDirectory(directory);
        return directory;
    }
}
