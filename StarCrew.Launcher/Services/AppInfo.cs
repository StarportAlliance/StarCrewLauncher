namespace StarCrew.Launcher.Services;

internal sealed class AppInfo : IAppInfo
{
    public string? Version
    {
        get
        {
            Version? version = typeof(App).Assembly.GetName().Version;
            return version is null ? null : $"{version.Major}.{version.Minor}.{version.Build}";
        }
    }
}
