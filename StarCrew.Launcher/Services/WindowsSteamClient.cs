using System.Diagnostics;

namespace StarCrew.Launcher.Services;

/// <summary>基于 Windows 进程枚举与注册表的 Steam 客户端相关服务。</summary>
internal sealed class WindowsSteamClient : ISteamClient
{
    private readonly ISteamEnvironment _environment;
    private readonly IProcessStarter _starter;
    private readonly IAppStrings _strings;

    public WindowsSteamClient(
        ISteamEnvironment environment,
        IProcessStarter starter,
        IAppStrings strings
    )
    {
        ArgumentNullException.ThrowIfNull(environment);
        ArgumentNullException.ThrowIfNull(starter);
        ArgumentNullException.ThrowIfNull(strings);
        _environment = environment;
        _starter = starter;
        _strings = strings;
    }

    public bool IsSteamRunning() => Process.GetProcessesByName("steam").Length > 0;

    public bool TryStartSteam(out string? error)
    {
        string? exePath = FindSteamExe();
        if (string.IsNullOrEmpty(exePath))
        {
            error = _strings.GetString("Steam_NotFound");
            return false;
        }

        return _starter.TryStart(
            exePath,
            Path.GetDirectoryName(exePath) ?? string.Empty,
            out error
        );
    }

    private string? FindSteamExe()
    {
        List<string?> candidates = [.. _environment.GetRegistrySteamPaths()];
        candidates.Add(Path.Combine(_environment.GetProgramFilesX86(), "Steam"));
        candidates.Add(Path.Combine(_environment.GetProgramFiles(), "Steam"));

        foreach (string? dir in candidates)
        {
            if (!string.IsNullOrWhiteSpace(dir) && _environment.DirectoryExists(dir))
            {
                string candidate = Path.Combine(dir, "steam.exe");
                if (_environment.FileExists(candidate))
                {
                    return candidate;
                }
            }
        }

        return null;
    }
}
