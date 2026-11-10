using Microsoft.Extensions.Logging;
using StarportAlliance.StarCrewLauncher.Models;

namespace StarportAlliance.StarCrewLauncher.Services;

internal sealed class DownloadCoordinator : IDownloadCoordinator
{
    private const int AmongUsAppId = 945360;
    private const int AmongUsDepotId = 945361;
    private const string SddWorkDirectoryName = ".StarDepotDownloader";

    private readonly IInstanceStore _store;
    private readonly ISddClient _sdd;
    private readonly string _instancesRoot;
    private readonly IAppStrings _strings;
    private readonly ILogger<DownloadCoordinator> _logger;

    public DownloadCoordinator(
        IInstanceStore store,
        ISddClient sdd,
        IAppStrings strings,
        ILogger<DownloadCoordinator> logger
    )
        : this(store, sdd, AppPaths.InstancesDirectory, strings, logger) { }

    internal DownloadCoordinator(
        IInstanceStore store,
        ISddClient sdd,
        string instancesRoot,
        IAppStrings strings,
        ILogger<DownloadCoordinator> logger
    )
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(sdd);
        ArgumentNullException.ThrowIfNull(strings);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentException.ThrowIfNullOrWhiteSpace(instancesRoot);
        _store = store;
        _sdd = sdd;
        _strings = strings;
        _logger = logger;
        _instancesRoot = instancesRoot;
    }

    public async Task<GameInstallResult> InstallAsync(
        string instanceName,
        GameVersionEntry version,
        IProgress<DownloadProgressInfo>? progress = null,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(instanceName);
        ArgumentNullException.ThrowIfNull(version);

        LauncherState state = _store.Load();
        string? accountName = state.SteamAccountName;
        if (string.IsNullOrWhiteSpace(accountName))
        {
            _logger.LogWarning("下载 {Version} 失败：未登录 Steam", version.VersionLabel);
            return new GameInstallResult(false, _strings.GetString("Download_LoginRequired"));
        }

        _logger.LogInformation(
            "开始下载 {Version} 到实例 {Instance}（账号 {Account}）",
            version.VersionLabel,
            instanceName,
            LogRedaction.MaskAccountName(accountName)
        );

        progress?.Report(
            new DownloadProgressInfo(
                DownloadStage.PreparingTool,
                0,
                _strings.GetString("Install_CheckingSize")
            )
        );

        Guid instanceGuid = Guid.NewGuid();
        string instanceId = instanceGuid.ToString("N");
        string instanceDirectory = Path.Combine(_instancesRoot, instanceId);
        string metadataDirectory = Path.Combine(AppPaths.SddDirectory, instanceId);

        IProgress<SddProgress> lineProgress = new Progress<SddProgress>(value =>
            progress?.Report(
                new DownloadProgressInfo(
                    DownloadStage.DownloadingGame,
                    Math.Clamp(value.Percent, 0, 100),
                    _strings.Format(
                        "Install_DownloadingFormat",
                        version.VersionLabel,
                        value.Percent
                    ),
                    value.Total > 0 ? value.Total : null
                )
            )
        );

        try
        {
            Directory.CreateDirectory(instanceDirectory);

            SddDownloadResult result = await _sdd.DownloadAsync(
                new SddDownloadRequest(
                    accountName,
                    version.ManifestId,
                    instanceDirectory,
                    metadataDirectory
                ),
                lineProgress,
                cancellationToken
            );

            if (!result.IsSuccess)
            {
                return await FailAsync(ComposeFailureText(result));
            }

            string exePath = Path.Combine(instanceDirectory, "Among Us.exe");
            if (!File.Exists(exePath))
            {
                return await FailAsync(_strings.GetString("Install_ExeMissing"));
            }

            GameInstance ready = new(
                instanceGuid,
                instanceName,
                instanceDirectory,
                AmongUsAppId,
                AmongUsDepotId,
                version.ManifestId,
                version.VersionLabel,
                InstanceState.Ready,
                DateTimeOffset.UtcNow,
                null
            );

            LauncherState stateAfterDone = _store.Load();
            _store.Save(stateAfterDone with { Instances = [.. stateAfterDone.Instances, ready] });

            progress?.Report(
                new DownloadProgressInfo(
                    DownloadStage.Completed,
                    100,
                    _strings.Format("Install_CompletedFormat", version.VersionLabel)
                )
            );
            _logger.LogInformation(
                "下载 {Version} 完成，实例目录 {Directory}",
                version.VersionLabel,
                instanceDirectory
            );

            await Task.Run(
                () => CleanupWorkDirectories(instanceDirectory, metadataDirectory),
                CancellationToken.None
            );
            return new GameInstallResult(
                true,
                _strings.Format("Install_DownloadedFormat", version.VersionLabel),
                ready
            );
        }
        catch (OperationCanceledException)
        {
            return await FailAsync(_strings.GetString("Install_Cancelled"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "下载 {Version} 过程中出现异常", version.VersionLabel);
            return await FailAsync(_strings.Format("Download_FailedFormat", ex.Message));
        }

        async Task<GameInstallResult> FailAsync(string message)
        {
            _logger.LogError(
                "下载 {Version} 失败：{Reason}",
                version.VersionLabel,
                LogRedaction.Scrub(message)
            );
            await Task.Run(
                () =>
                {
                    TryDeleteDirectory(instanceDirectory);
                    TryDeleteDirectory(metadataDirectory);
                },
                CancellationToken.None
            );
            progress?.Report(new DownloadProgressInfo(DownloadStage.Failed, 0, message));
            return new GameInstallResult(false, message);
        }
    }

    private string ComposeFailureText(SddDownloadResult result)
    {
        if (result.ErrorCode is SddErrorCode.AuthRequired or SddErrorCode.AuthFailed)
        {
            return _strings.GetString("Download_LoginRequired");
        }

        if (result.ErrorCode == SddErrorCode.ToolMissing)
        {
            return _strings.GetString("Sdd_Missing");
        }

        if (result.ErrorCode == SddErrorCode.DepotUnavailable)
        {
            return _strings.GetString("Install_DepotUnavailable");
        }

        return string.IsNullOrWhiteSpace(result.ErrorMessage)
            ? _strings.Format("Install_FailedExitCodeFormat", result.ExitCode)
            : _strings.Format("Download_FailedFormat", result.ErrorMessage);
    }

    private static void CleanupWorkDirectories(string instanceDirectory, string metadataDirectory)
    {
        TryDeleteDirectory(Path.Combine(instanceDirectory, SddWorkDirectoryName));
        TryDeleteDirectory(metadataDirectory);
    }

    private static void TryDeleteDirectory(string directory)
    {
        try
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
        catch (IOException)
        {
            // 删不掉就算了，反正用户看不到。。
        }
        catch (UnauthorizedAccessException)
        {
            // 同上
        }
    }
}
