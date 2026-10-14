using StarCrew.Launcher.Models;

namespace StarCrew.Launcher.Services;

/// <summary>实例启动接缝：生产用 <see cref="InstanceLauncher" />，测试用替身。</summary>
internal interface IInstanceLauncher
{
    /// <summary>准备并启动该实例：确保 Steam 客户端在线、拉起游戏 exe。</summary>
    Task<LaunchOutcome> LaunchAsync(
        GameInstance instance,
        CancellationToken cancellationToken = default
    );
}
