using StarCrew.Launcher.Models;

namespace StarCrew.Launcher.Services;

/// <summary>游戏启动接缝：生产用 <see cref="GameLauncher" />，测试用替身。</summary>
internal interface IGameLauncher
{
    /// <summary>按 Steam 协议 → 自动定位 exe 的顺序尝试启动，全部失败时返回失败结果。</summary>
    Task<LaunchResult> LaunchAsync(CancellationToken cancellationToken = default);

    /// <summary>直接启动指定路径的 exe，调用方需保证路径来自用户选择或自动定位。</summary>
    bool TryLaunchExe(string exePath, out string? error);
}
