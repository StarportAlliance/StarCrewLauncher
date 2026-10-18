using System.Diagnostics;

namespace StarCrew.Launcher.Services;

internal sealed class ExternalLauncher : IExternalLauncher
{
    private readonly IAppStrings _strings;

    public ExternalLauncher(IAppStrings strings)
    {
        ArgumentNullException.ThrowIfNull(strings);
        _strings = strings;
    }

    public async Task OpenUriAsync(string uri)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(uri);
        await Windows.System.Launcher.LaunchUriAsync(new Uri(uri, UriKind.Absolute));
    }

    public async Task OpenFolderAsync(string folderPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(folderPath);
        if (!Directory.Exists(folderPath))
        {
            throw new DirectoryNotFoundException(
                _strings.Format("OpenFolder_NotFoundFormat", folderPath)
            );
        }

        await Task.Run(() =>
        {
            Process.Start(new ProcessStartInfo(folderPath) { UseShellExecute = true });
        });
    }
}
