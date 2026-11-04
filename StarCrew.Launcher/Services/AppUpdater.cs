using Microsoft.Extensions.Logging;
using StarCrew.Launcher.Models;
using Velopack.Exceptions;

namespace StarCrew.Launcher.Services;

/// <summary>应用更新器。</summary>
internal sealed class AppUpdater
{
    private readonly IUpdateClient _client;
    private readonly IAppStrings _strings;
    private readonly ILogger<AppUpdater> _logger;

    public AppUpdater(IUpdateClient client, IAppStrings strings, ILogger<AppUpdater> logger)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(strings);
        ArgumentNullException.ThrowIfNull(logger);
        _client = client;
        _strings = strings;
        _logger = logger;
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
            _logger.LogInformation("更新检查跳过：当前不是安装版");
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
                _logger.LogInformation("更新检查完成：已是最新 {Version}", current);
                return new UpdateCheckResult(
                    UpdateState.UpToDate,
                    null,
                    _strings.Format("Update_UpToDateFormat", current)
                );
            }

            _logger.LogInformation("更新检查完成：发现新版本 {Version}", version);
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
            _logger.LogInformation("更新检查跳过：当前不是安装版");
            return new UpdateCheckResult(
                UpdateState.NotInstalled,
                null,
                _strings.GetString("Update_DevRunSkip")
            );
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "更新检查失败");
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
            _logger.LogInformation("更新下载跳过：当前不是安装版");
            return new UpdateCheckResult(
                UpdateState.NotInstalled,
                null,
                _strings.GetString("Update_DevRunSkip")
            );
        }

        try
        {
            _logger.LogInformation("开始下载更新 {Version}", version);
            await _client
                .DownloadUpdatesAsync(version, progress, cancellationToken)
                .ConfigureAwait(false);
            _client.PrepareUpdateForRestart(version);
            _logger.LogInformation("更新 {Version} 已就绪，等待重启安装", version);
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
            _logger.LogError(ex, "下载更新 {Version} 失败", version);
            return new UpdateCheckResult(
                UpdateState.Failed,
                null,
                _strings.Format("Update_DownloadFailedFormat", ex.Message)
            );
        }
    }
}
