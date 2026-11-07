namespace StarportAlliance.StarCrewLauncher.Services;

/// <summary>应用数据目录常量。</summary>
internal static class AppPaths
{
    /// <summary>应用数据存储目录。</summary>
    public static string DataDirectory { get; } =
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "StarportAlliance",
            "StarCrewLauncher"
        );

    /// <summary>所有游戏实例的安装根目录。</summary>
    public static string InstancesDirectory { get; } = Path.Combine(DataDirectory, "Instances");

    /// <summary>实例状态持久化文件路径。</summary>
    public static string StateFile { get; } = Path.Combine(DataDirectory, "instances.json");

    /// <summary>远端资源缓存目录。</summary>
    public static string CacheDirectory { get; } = Path.Combine(DataDirectory, "cache");

    /// <summary>用户自定义背景图片目录。</summary>
    public static string BackgroundDirectory { get; } = Path.Combine(DataDirectory, "Background");

    /// <summary>日志文件目录。</summary>
    public static string LogDirectory { get; } = Path.Combine(DataDirectory, "Logs");

    /// <summary>StarDepotDownloader 的凭证与元数据目录。</summary>
    public static string SddDirectory { get; } = Path.Combine(DataDirectory, "Sdd");

    /// <summary>由启动器统一管理的 SDD 凭证文件。</summary>
    public static string SddAccountFile { get; } = Path.Combine(SddDirectory, "account.config");
}
