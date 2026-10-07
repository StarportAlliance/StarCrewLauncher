namespace StarCrew.Launcher.Services;

/// <summary>用系统默认程序打开链接。</summary>
internal sealed class ExternalLauncher : IExternalLauncher
{
    public async Task OpenUriAsync(string uri)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(uri);
        await Windows.System.Launcher.LaunchUriAsync(new Uri(uri, UriKind.Absolute));
    }
}
