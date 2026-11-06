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
    /// <returns>文件缺失（被杀软隔离、安装不完整）时返回 null。</returns>
    internal string? Find()
    {
        string path = Path.Combine(_directory, "StarDepotDownloader.exe");
        return File.Exists(path) ? path : null;
    }
}
