namespace StarCrew.Launcher.Services;

/// <summary>外部打开接缝：生产用系统默认程序打开链接，测试用替身。</summary>
internal interface IExternalLauncher
{
    /// <summary>用系统默认浏览器打开 URI。</summary>
    Task OpenUriAsync(string uri);
}
