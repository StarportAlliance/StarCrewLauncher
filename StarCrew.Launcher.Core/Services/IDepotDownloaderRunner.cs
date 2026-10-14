using StarCrew.Launcher.Models;

namespace StarCrew.Launcher.Services;

/// <summary>外部进程执行抽象：让下载/登录流程可测试。</summary>
internal interface IDepotDownloaderRunner
{
    /// <summary>运行外部程序，完成后返回退出码与输出行。</summary>
    Task<ExecutableOutcome> RunAsync(
        ExecutableCommand command,
        IProgress<string>? output = null,
        CancellationToken cancellationToken = default
    );
}
