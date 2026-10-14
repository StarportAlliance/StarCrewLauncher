using System.Diagnostics;

namespace StarCrew.Launcher.Services;

/// <summary>基于 Windows 进程枚举与注册表的生产实现；Steam 未安装时返回 null。</summary>
internal sealed class WindowsSteamClient : ISteamClient
{
    private readonly ISteamEnvironment _environment;
    private readonly IProcessStarter _starter;

    public WindowsSteamClient(ISteamEnvironment environment, IProcessStarter starter)
    {
        ArgumentNullException.ThrowIfNull(environment);
        ArgumentNullException.ThrowIfNull(starter);
        _environment = environment;
        _starter = starter;
    }

    public bool IsSteamRunning() => Process.GetProcessesByName("steam").Length > 0;

    public bool TryStartSteam(out string? error)
    {
        string? exePath = FindSteamExe();
        if (string.IsNullOrEmpty(exePath))
        {
            error = "未找到 Steam 主程序，请确认已安装 Steam。";
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
