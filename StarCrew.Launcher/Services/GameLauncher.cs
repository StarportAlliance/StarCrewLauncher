using System.Diagnostics;
using StarCrew.Launcher.Models;

namespace StarCrew.Launcher.Services;

/// <summary>游戏启动器：优先走 Steam 协议拉起，失败时回退到直接启动 exe。</summary>
internal sealed class GameLauncher
{
    private readonly SteamGameLocator _locator = new();

    /// <summary>按 Steam 协议 → 自动定位 exe 的顺序尝试启动，全部失败时返回失败结果。</summary>
    public Task<LaunchResult> LaunchAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (TryLaunchBySteamProtocol(out string? steamError))
        {
            return Task.FromResult(
                new LaunchResult(
                    true,
                    LaunchMethod.SteamProtocol,
                    "已通过 Steam 发起启动，如果游戏没有弹出请确认 Steam 已登录。"
                )
            );
        }

        string? exe = _locator.FindGameExe();
        if (exe is not null && TryLaunchExe(exe, out _))
        {
            return Task.FromResult(
                new LaunchResult(
                    true,
                    LaunchMethod.DirectExe,
                    $"已直接启动 {Path.GetFileName(exe)}。"
                )
            );
        }

        string reason = exe is null
            ? "没有找到 Steam，也没有在 Steam 游戏库中找到 Among Us，请手动选择游戏文件。"
            : "通过 Steam 与直接启动均失败，请手动选择游戏文件后重试。";
        if (steamError is not null)
        {
            reason += $"（Steam 拉起失败：{steamError}）";
        }

        return Task.FromResult(new LaunchResult(false, LaunchMethod.None, reason));
    }

    /// <summary>直接启动指定路径的 exe，调用方需保证路径来自用户选择或自动定位。</summary>
    public bool TryLaunchExe(string exePath, out string? error)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(exePath);
        error = null;

        try
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = exePath,
                WorkingDirectory = Path.GetDirectoryName(exePath) ?? string.Empty,
                UseShellExecute = true,
            };
            Process.Start(startInfo);
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    /// <summary>通过 steam:// 协议拉起游戏，返回是否已成功移交启动请求。</summary>
    public bool TryLaunchBySteamProtocol(out string? error)
    {
        error = null;

        try
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = $"steam://rungameid/{SteamGameLocator.AmongUsAppId}",
                UseShellExecute = true,
            };
            Process.Start(startInfo);
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }
}
