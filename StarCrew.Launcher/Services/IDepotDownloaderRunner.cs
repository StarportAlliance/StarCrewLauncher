using StarCrew.Launcher.Models;

namespace StarCrew.Launcher.Services;

/// <summary>外部进程运行器。</summary>
internal interface IDepotDownloaderRunner
{
    /// <summary>运行一个外部程序。</summary>
    /// <returns>程序运行结果。</returns>
    Task<ExecutableOutcome> RunAsync(
        ExecutableCommand command,
        IProgress<string>? output = null,
        CancellationToken cancellationToken = default
    );
}
