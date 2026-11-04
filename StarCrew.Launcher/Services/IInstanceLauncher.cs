using StarCrew.Launcher.Models;

namespace StarCrew.Launcher.Services;

/// <summary>实例启动器。</summary>
internal interface IInstanceLauncher
{
    /// <summary>准备并启动该实例。</summary>
    Task<LaunchOutcome> LaunchAsync(
        GameInstance instance,
        CancellationToken cancellationToken = default
    );
}
