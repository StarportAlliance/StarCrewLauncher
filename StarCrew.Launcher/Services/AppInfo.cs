namespace StarCrew.Launcher.Services;

/// <summary>启动器自身版本，取自主程序集。</summary>
internal sealed class AppInfo : IAppInfo
{
    public string? Version
    {
        get
        {
            System.Version? version = typeof(App).Assembly.GetName().Version;
            return version is null ? null : $"{version.Major}.{version.Minor}.{version.Build}";
        }
    }
}
