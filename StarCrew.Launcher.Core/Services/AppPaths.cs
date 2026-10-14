namespace StarCrew.Launcher.Services;

/// <summary>应用数据目录常量，所有持久化数据都放在用户的 LocalAppData 下。</summary>
internal static class AppPaths
{
    /// <summary>状态文件（instances.json）所在目录。</summary>
    public static string DataDirectory { get; } =
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "StarCrewLauncher"
        );

    /// <summary>所有游戏实例的安装根目录。</summary>
    public static string InstancesDirectory { get; } = Path.Combine(DataDirectory, "Instances");

    /// <summary>实例状态持久化文件路径。</summary>
    public static string StateFile { get; } = Path.Combine(DataDirectory, "instances.json");

    /// <summary>下载工具存放目录（批次 3 使用）。</summary>
    public static string ToolsDirectory { get; } = Path.Combine(DataDirectory, "tools");

    /// <summary>远端资源缓存目录（版本列表 JSON 等）。</summary>
    public static string CacheDirectory { get; } = Path.Combine(DataDirectory, "cache");
}
