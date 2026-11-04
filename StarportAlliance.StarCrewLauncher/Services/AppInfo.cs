namespace StarportAlliance.StarCrewLauncher.Services;

internal sealed class AppInfo(IUpdateClient updateClient) : IAppInfo
{
    public bool IsDevelopment => !updateClient.IsInstalled;

    public string? Version
    {
        get
        {
            Version? version = typeof(App).Assembly.GetName().Version;
            return version is null ? null : $"{version.Major}.{version.Minor}.{version.Build}";
        }
    }
}
