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
    private readonly IAppStrings _strings;

    public InstanceLauncher(IProcessStarter starter, ISteamClient steam, IAppStrings strings)
    {
        ArgumentNullException.ThrowIfNull(starter);
        ArgumentNullException.ThrowIfNull(steam);
        ArgumentNullException.ThrowIfNull(strings);
        _starter = starter;
        _steam = steam;
        _strings = strings;
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
            return new LaunchOutcome(false, _strings.GetString("Launch_NotReady"));
        }

        string exePath = Path.Combine(instance.InstallDirectory, GameExecutableName);
        if (!File.Exists(exePath))
        {
            return new LaunchOutcome(false, _strings.GetString("Launch_GameFileMissing"));
        }

        await EnsureSteamOnlineAsync(cancellationToken);

        try
        {
            return _starter.TryStart(exePath, instance.InstallDirectory, out string? error)
                ? new LaunchOutcome(true, _strings.GetString("Launch_Started"))
                : new LaunchOutcome(false, error ?? _strings.GetString("Launch_Failed"));
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
