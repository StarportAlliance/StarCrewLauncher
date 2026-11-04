namespace StarCrew.Launcher.Services;

/// <summary>进程拉起器。</summary>
internal interface IProcessStarter
{
    /// <summary>尝试拉起进程。</summary>
    /// <param name="fileName">可执行文件名。</param>
    /// <param name="workingDirectory">工作目录。</param>
    /// <param name="error">拉起进程失败时的错误信息。</param>
    /// <returns>是否成功拉起进程。</returns>
    bool TryStart(string fileName, string workingDirectory, out string? error);
}
