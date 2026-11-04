namespace StarCrew.Launcher.Services;

/// <summary>外部应用启动器。</summary>
internal interface IExternalLauncher
{
    /// <summary>通过系统默认程序打开给定的 <paramref name="uri"/>。</summary>
    Task OpenUriAsync(string uri);

    /// <summary>通过文件管理器打开给定的文件夹。</summary>
    Task OpenFolderAsync(string folderPath);
}
