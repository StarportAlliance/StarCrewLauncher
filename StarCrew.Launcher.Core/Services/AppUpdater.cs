using StarCrew.Launcher.Models;
using Velopack.Exceptions;

namespace StarCrew.Launcher.Services;

/// <summary>应用更新器：检查远端新版本，命中时自动下载并安排在重启后应用。</summary>
internal sealed class AppUpdater
{
    private readonly IUpdateClient _client;

    /// <summary>使用指定的更新客户端构造更新器。</summary>
    public AppUpdater(IUpdateClient client)
    {
        ArgumentNullException.ThrowIfNull(client);
        _client = client;
    }

    /// <summary>当前已安装版本，未安装时为 null，关于页展示用。</summary>
    public string? CurrentVersion => _client.CurrentVersion;

    /// <summary>检查更新并在命中时下载、安排应用；调用方按返回状态提示用户重启。</summary>
    public async Task<UpdateCheckResult> CheckAndPrepareUpdateAsync(
        IProgress<int>? progress = null,
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
}
