namespace StarportAlliance.StarCrewLauncher.Services;

/// <summary>定位随启动器分发的 StarDepotDownloader。</summary>
internal sealed class StarDepotDownloaderLocator
{
    private readonly string _directory;

    public StarDepotDownloaderLocator()
        : this(Path.Combine(AppContext.BaseDirectory, "Tools", "StarDepotDownloader")) { }

    internal StarDepotDownloaderLocator(string directory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        _directory = directory;
    }

    /// <summary>随包可执行文件路径。</summary>
    /// <exception cref="FileNotFoundException">文件缺失（安装不完整、被杀软隔离）。</exception>
    internal string Find()
    {
        string path = Path.Combine(_directory, "StarDepotDownloader.exe");
        return File.Exists(path)
            ? path
            : throw new FileNotFoundException(
                $"未找到内置的 StarDepotDownloader：{path}。安装可能不完整，或文件被杀毒软件隔离。",
                path
            );
    }
}
