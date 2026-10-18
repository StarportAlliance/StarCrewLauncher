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

    public InstanceLauncher(IProcessStarter starter, ISteamClient steam, IAppStrings strings)
    {
        ArgumentNullException.ThrowIfNull(starter);
        ArgumentNullException.ThrowIfNull(steam);
        ArgumentNullException.ThrowIfNull(strings);
        _starter = starter;
        _steam = steam;
        _strings = strings;
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

    /// <summary>静默拉起 Steam 客户端。</summary>
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
