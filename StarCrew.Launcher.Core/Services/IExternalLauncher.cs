namespace StarCrew.Launcher.Services;

/// <summary>外部打开接缝：生产用系统默认程序打开链接或文件夹，测试用替身。</summary>
internal interface IExternalLauncher
{
    /// <summary>用系统默认浏览器打开 URI。</summary>
    Task OpenUriAsync(string uri);

    /// <summary>用系统默认文件管理器打开文件夹。</summary>
    Task OpenFolderAsync(string folderPath);
}
