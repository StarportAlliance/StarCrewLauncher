namespace StarportAlliance.StarCrewLauncher.Services;

internal sealed class AppInfo(IUpdateClient updateClient) : IAppInfo
{
    public bool IsDevelopment => !updateClient.IsInstalled;

    public string Version
    {
        get
        {
            Version version = typeof(App).Assembly.GetName().Version ?? new(0, 0, 0);
            return $"{version.Major}.{version.Minor}.{version.Build}";
        }
    }
}
