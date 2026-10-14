using StarCrew.Launcher.Models;

namespace StarCrew.Launcher.Services;

/// <summary>在当前平台上的实例启动器：检测/启动 Steam 客户端，再直接跑游戏 exe。</summary>
internal sealed class InstanceLauncher : IInstanceLauncher
{
    private const string GameExecutableName = "Among Us.exe";

    // 给 Steam 客户端一点启动和注册表初始化的时间，最长等 10 秒
    private const int SteamWaitAttempts = 20;
    private static readonly TimeSpan SteamWaitInterval = TimeSpan.FromMilliseconds(500);

    private readonly IProcessStarter _starter;
    private readonly ISteamClient _steam;

    public InstanceLauncher(IProcessStarter starter, ISteamClient steam)
    {
        ArgumentNullException.ThrowIfNull(starter);
        ArgumentNullException.ThrowIfNull(steam);
        _starter = starter;
        _steam = steam;
    }

    /// <inheritdoc />
    public async Task<LaunchOutcome> LaunchAsync(
        GameInstance instance,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(instance);
        cancellationToken.ThrowIfCancellationRequested();

        if (instance.State != InstanceState.Ready)
        {
            return new LaunchOutcome(false, "该实例尚未就绪，请先完成安装。");
        }

        string exePath = Path.Combine(instance.InstallDirectory, GameExecutableName);
        if (!File.Exists(exePath))
        {
            return new LaunchOutcome(false, "未找到游戏文件，请重新下载或修复该实例。");
        }

        await EnsureSteamOnlineAsync(cancellationToken);

        try
        {
            return _starter.TryStart(exePath, instance.InstallDirectory, out string? error)
                ? new LaunchOutcome(true, "已启动。")
                : new LaunchOutcome(false, error ?? "启动失败。");
        }
        catch (Exception ex)
        {
            return new LaunchOutcome(false, ex.Message);
        }
    }

    // Steam 客户端不在时尝试静默拉起；它跑不起来也不阻断游戏的启动，只是联机可能离线工作
    private async Task EnsureSteamOnlineAsync(CancellationToken cancellationToken)
    {
        if (_steam.IsSteamRunning())
        {
            return;
        }

        if (!_steam.TryStartSteam(out _))
        {
            return;
        }

        for (int attempt = 0; attempt < SteamWaitAttempts && !_steam.IsSteamRunning(); attempt++)
        {
            await Task.Delay(SteamWaitInterval, cancellationToken);
        }
    }
}
