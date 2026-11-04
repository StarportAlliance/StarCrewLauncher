using Microsoft.Extensions.Logging;
using StarCrew.Launcher.Models;

namespace StarCrew.Launcher.Services;

internal sealed class InstanceLauncher : IInstanceLauncher
{
    private const string GameExecutableName = "Among Us.exe";
    private const int SteamWaitAttempts = 20;
    private static readonly TimeSpan SteamWaitInterval = TimeSpan.FromMilliseconds(500);

    private readonly IProcessStarter _starter;
    private readonly ISteamClient _steam;
    private readonly IAppStrings _strings;
    private readonly ILogger<InstanceLauncher> _logger;

    public InstanceLauncher(
        IProcessStarter starter,
        ISteamClient steam,
        IAppStrings strings,
        ILogger<InstanceLauncher> logger
    )
    {
        ArgumentNullException.ThrowIfNull(starter);
        ArgumentNullException.ThrowIfNull(steam);
        ArgumentNullException.ThrowIfNull(strings);
        ArgumentNullException.ThrowIfNull(logger);
        _starter = starter;
        _steam = steam;
        _strings = strings;
        _logger = logger;
    }

    public async Task<LaunchOutcome> LaunchAsync(
        GameInstance instance,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(instance);
        cancellationToken.ThrowIfCancellationRequested();

        if (instance.State != InstanceState.Ready)
        {
            _logger.LogWarning("实例 {Instance} 状态不是就绪，无法启动", instance.Name);
            return new LaunchOutcome(false, _strings.GetString("Launch_NotReady"));
        }

        string exePath = Path.Combine(instance.InstallDirectory, GameExecutableName);
        if (!File.Exists(exePath))
        {
            _logger.LogWarning("实例 {Instance} 缺少游戏可执行文件 {Path}", instance.Name, exePath);
            return new LaunchOutcome(false, _strings.GetString("Launch_GameFileMissing"));
        }

        await EnsureSteamOnlineAsync(cancellationToken);

        try
        {
            if (_starter.TryStart(exePath, instance.InstallDirectory, out string? error))
            {
                _logger.LogInformation("实例 {Instance} 启动成功", instance.Name);
                return new LaunchOutcome(true, _strings.GetString("Launch_Started"));
            }

            _logger.LogError("实例 {Instance} 启动失败：{Error}", instance.Name, error);
            return new LaunchOutcome(false, error ?? _strings.GetString("Launch_Failed"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "实例 {Instance} 启动异常", instance.Name);
            return new LaunchOutcome(false, ex.Message);
        }
    }

    /// <summary>静默拉起 Steam 客户端。</summary>
    private async Task EnsureSteamOnlineAsync(CancellationToken cancellationToken)
    {
        if (_steam.IsSteamRunning())
        {
            return;
        }

        _logger.LogDebug("Steam 未运行，尝试拉起");
        if (!_steam.TryStartSteam(out _))
        {
            _logger.LogDebug("Steam 拉起失败");
            return;
        }

        for (int attempt = 0; attempt < SteamWaitAttempts && !_steam.IsSteamRunning(); attempt++)
        {
            await Task.Delay(SteamWaitInterval, cancellationToken);
        }
    }
}
