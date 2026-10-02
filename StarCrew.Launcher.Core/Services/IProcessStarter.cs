namespace StarCrew.Launcher.Services;

/// <summary>进程拉起接缝：生产用 <see cref="ProcessStarter" />，测试用替身。</summary>
internal interface IProcessStarter
{
    /// <summary>尽力拉起进程，失败时返回 false 并带出错误信息。</summary>
    bool TryStart(string fileName, string workingDirectory, out string? error);
}
