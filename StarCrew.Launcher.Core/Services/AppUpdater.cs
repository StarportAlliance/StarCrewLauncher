using StarCrew.Launcher.Models;
using Velopack.Exceptions;

namespace StarCrew.Launcher.Services;

/// <summary>应用更新器。</summary>
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
