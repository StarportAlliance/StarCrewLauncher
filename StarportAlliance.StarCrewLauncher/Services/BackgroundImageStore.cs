namespace StarportAlliance.StarCrewLauncher.Services;

internal sealed class BackgroundImageStore : IBackgroundImageStore
{
    private static readonly HashSet<string> AllowedExtensions = [".PNG", ".JPG", ".JPEG", ".BMP"];

    private readonly string _directory;

    public BackgroundImageStore(string directory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        _directory = directory;
    }

    public async Task<string> ImportAsync(string sourcePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);

        string extension = Path.GetExtension(sourcePath).ToUpperInvariant();
        if (!AllowedExtensions.Contains(extension))
        {
            throw new ArgumentException(
                "背景图片仅支持 PNG、JPG、JPEG 或 BMP 格式。",
                nameof(sourcePath)
            );
        }

        string destination = Path.Combine(_directory, "background" + extension);
        if (
            string.Equals(
                Path.GetFullPath(sourcePath),
                Path.GetFullPath(destination),
                StringComparison.OrdinalIgnoreCase
            )
        )
        {
            return destination;
        }

        await Task.Run(() =>
        {
            Directory.CreateDirectory(_directory);
            foreach (string existing in Directory.GetFiles(_directory, "background.*"))
            {
                File.Delete(existing);
            }

            File.Copy(sourcePath, destination, overwrite: true);
        });

        return destination;
    }

    public void Delete(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        try
        {
            File.Delete(path);
        }
        catch (IOException)
        {
            // 删不掉只会留下孤儿文件，下次导入会整体清理
        }
        catch (UnauthorizedAccessException) { }
    }
}
