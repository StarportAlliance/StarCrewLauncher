using System.Diagnostics;

namespace StarCrew.Launcher.Services;

/// <summary>用系统默认程序打开链接或文件夹。</summary>
internal sealed class ExternalLauncher : IExternalLauncher
{
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
            throw new DirectoryNotFoundException($"文件夹不存在：{folderPath}");
        }

        await Task.Run(() =>
        {
            Process.Start(new ProcessStartInfo(folderPath) { UseShellExecute = true });
        });
    }
}
