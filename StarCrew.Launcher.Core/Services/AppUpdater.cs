using StarCrew.Launcher.Models;
using Velopack.Exceptions;

namespace StarCrew.Launcher.Services;

/// <summary>应用更新器：先检查远端新版本，用户确认后再下载并安排在重启后应用。</summary>
internal sealed class AppUpdater
{
    private readonly IUpdateClient _client;

    public AppUpdater(IUpdateClient client)
    {
        ArgumentNullException.ThrowIfNull(client);
        _client = client;
    }

    /// <summary>当前已安装版本，未经安装时为 null。</summary>
    public string? CurrentVersion => _client.CurrentVersion;

    /// <summary>仅检查远端新版本；命中时返回 Available，调用方据此弹窗请用户确认下载。</summary>
    public async Task<UpdateCheckResult> CheckOnlyAsync(
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_client.IsInstalled)
        {
            return new UpdateCheckResult(
                UpdateState.NotInstalled,
                null,
                "当前为开发运行，未经安装，跳过更新检查。"
            );
        }

        try
        {
            string? version = await _client
                .CheckForUpdatesAsync(cancellationToken)
                .ConfigureAwait(false);
            if (version is null)
            {
                string current = _client.CurrentVersion ?? "未知";
                return new UpdateCheckResult(
                    UpdateState.UpToDate,
                    null,
                    $"已是最新版本（{current}）。"
                );
            }

            return new UpdateCheckResult(UpdateState.Available, version, $"发现新版本 {version}。");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (NotInstalledException)
        {
            return new UpdateCheckResult(
                UpdateState.NotInstalled,
                null,
                "当前未经 Velopack 安装，无法检查更新。"
            );
        }
        catch (Exception ex)
        {
            return new UpdateCheckResult(UpdateState.Failed, null, $"检查更新失败：{ex.Message}");
        }
    }

    /// <summary>下载指定版本并安排在重启后应用；调用方确认后再调，成功后直接退出即自动重启生效。</summary>
    public async Task<UpdateCheckResult> DownloadAndPrepareAsync(
        string version,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(version);
        cancellationToken.ThrowIfCancellationRequested();

        if (!_client.IsInstalled)
        {
            return new UpdateCheckResult(
                UpdateState.NotInstalled,
                null,
                "当前未经 Velopack 安装，无法下载更新。"
            );
        }

        try
        {
            await _client
                .DownloadUpdatesAsync(version, progress, cancellationToken)
                .ConfigureAwait(false);
            _client.PrepareUpdateForRestart(version);
            return new UpdateCheckResult(
                UpdateState.ReadyToRestart,
                version,
                $"已下载新版本 {version}，重启后生效。"
            );
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new UpdateCheckResult(UpdateState.Failed, null, $"下载更新失败：{ex.Message}");
        }
    }
}
