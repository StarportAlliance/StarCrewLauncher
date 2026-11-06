using System.Diagnostics;

namespace StarportAlliance.StarCrewLauncher.Services;

/// <summary>基于 Windows 进程枚举与注册表的 Steam 客户端相关服务。</summary>
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

    public bool IsSteamRunning()
    {
        Process[] processes = Process.GetProcessesByName("steam");
        try
        {
            return processes.Length > 0;
        }
        finally
        {
            foreach (Process process in processes)
            {
                process.Dispose();
            }
        }
    }

    public bool TryStartSteam()
    {
        string? exePath = FindSteamExe();
        if (string.IsNullOrEmpty(exePath))
        {
            return false;
        }

        return _starter.TryStart(exePath, Path.GetDirectoryName(exePath) ?? string.Empty, out _);
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
