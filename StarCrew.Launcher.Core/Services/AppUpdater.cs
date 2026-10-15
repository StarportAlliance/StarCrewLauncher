using StarCrew.Launcher.Models;
using Velopack.Exceptions;

namespace StarCrew.Launcher.Services;

/// <summary>应用更新器：先检查远端新版本，用户确认后再下载并安排在重启后应用。</summary>
internal sealed class AppUpdater
{
    private readonly IUpdateClient _client;
    private readonly IAppStrings _strings;

    public AppUpdater(IUpdateClient client, IAppStrings strings)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(strings);
        _client = client;
        _strings = strings;
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
                _strings.GetString("Update_DevRunSkip")
            );
        }

        try
        {
            string? version = await _client
                .CheckForUpdatesAsync(cancellationToken)
                .ConfigureAwait(false);
            if (version is null)
            {
                string current =
                    _client.CurrentVersion ?? _strings.GetString("Update_UnknownVersion");
                return new UpdateCheckResult(
                    UpdateState.UpToDate,
                    null,
                    _strings.Format("Update_UpToDateFormat", current)
                );
            }

            return new UpdateCheckResult(
                UpdateState.Available,
                version,
                _strings.Format("Update_AvailableFormat", version)
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
                _strings.GetString("Update_NotInstalledCheck")
            );
        }
        catch (Exception ex)
        {
            return new UpdateCheckResult(
                UpdateState.Failed,
                null,
                _strings.Format("Update_CheckFailedFormat", ex.Message)
            );
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
                _strings.GetString("Update_NotInstalledDownload")
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
                _strings.Format("Update_DownloadedFormat", version)
            );
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new UpdateCheckResult(
                UpdateState.Failed,
                null,
                _strings.Format("Update_DownloadFailedFormat", ex.Message)
            );
        }
    }
}
