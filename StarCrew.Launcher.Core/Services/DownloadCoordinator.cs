using System.Globalization;
using Microsoft.Extensions.Logging;
using StarCrew.Launcher.Models;

namespace StarCrew.Launcher.Services;

internal sealed class DownloadCoordinator : IDownloadCoordinator
{
    private const int AmongUsAppId = 945360;
    private const int AmongUsDepotId = 945361;

    private readonly IInstanceStore _store;
    private readonly DepotDownloaderLocator _locator;
    private readonly IDepotDownloaderRunner _runner;
    private readonly string _instancesRoot;
    private readonly IAppStrings _strings;
    private readonly ILogger<DownloadCoordinator> _logger;

    public DownloadCoordinator(
        IInstanceStore store,
        DepotDownloaderLocator locator,
        IDepotDownloaderRunner runner,
        IAppStrings strings,
        ILogger<DownloadCoordinator> logger
    )
        : this(store, locator, runner, AppPaths.InstancesDirectory, strings, logger) { }

    internal DownloadCoordinator(
        IInstanceStore store,
        DepotDownloaderLocator locator,
        IDepotDownloaderRunner runner,
        string instancesRoot,
        IAppStrings strings,
        ILogger<DownloadCoordinator> logger
    )
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(locator);
        ArgumentNullException.ThrowIfNull(runner);
        ArgumentNullException.ThrowIfNull(strings);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentException.ThrowIfNullOrWhiteSpace(instancesRoot);
        _store = store;
        _locator = locator;
        _runner = runner;
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
            return new GameInstallResult(false, _strings.GetString("Install_LoginRequired"));
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
        string? depotDownloaderPath = _locator.Find();
        if (depotDownloaderPath is null)
        {
            return new GameInstallResult(false, _strings.GetString("Dd_Missing"));
        }

        Guid instanceGuid = Guid.NewGuid();
        string instanceId = instanceGuid.ToString("N");
        string instanceDirectory = Path.Combine(_instancesRoot, instanceId);
        Directory.CreateDirectory(instanceDirectory);

        long? totalBytes = null;
        List<string> arguments =
        [
            "-app",
            AmongUsAppId.ToString(CultureInfo.InvariantCulture),
            "-depot",
            AmongUsDepotId.ToString(CultureInfo.InvariantCulture),
            "-manifest",
            version.ManifestId.ToString(CultureInfo.InvariantCulture),
            "-dir",
            instanceDirectory,
            "-username",
            accountName,
            "-remember-password",
        ];

        IProgress<string> lineProgress = new Progress<string>(line =>
        {
            double? percent = TryParsePercent(line);
            if (percent is not null)
            {
                progress?.Report(
                    new DownloadProgressInfo(
                        DownloadStage.DownloadingGame,
                        percent.Value,
                        _strings.Format(
                            "Install_DownloadingFormat",
                            version.VersionLabel,
                            percent.Value
                        ),
                        totalBytes
                    )
                );
            }
        });

        try
        {
            totalBytes = await ReadTotalBytesAsync(
                depotDownloaderPath,
                accountName,
                version,
                instanceDirectory,
                cancellationToken
            );

            ExecutableOutcome outcome = await _runner.RunAsync(
                new ExecutableCommand(depotDownloaderPath, arguments, null),
                lineProgress,
                cancellationToken
            );

            if (outcome.ExitCode != 0)
            {
                return await FailAsync(ComposeFailureText(outcome));
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
            return await FailAsync(_strings.Format("Install_FailedFormat", ex.Message));
        }

        async Task<GameInstallResult> FailAsync(string message)
        {
            _logger.LogError(
                "下载 {Version} 失败：{Reason}",
                version.VersionLabel,
                LogRedaction.Scrub(message)
            );
            TryDeleteDirectory(instanceDirectory);
            progress?.Report(new DownloadProgressInfo(DownloadStage.Failed, 0, message));
            return new GameInstallResult(false, message);
        }
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

    private string ComposeFailureText(ExecutableOutcome outcome)
    {
        string? tail = outcome.OutputLines.LastOrDefault(l => !string.IsNullOrWhiteSpace(l));
        return tail is null
            ? _strings.Format("Install_FailedExitCodeFormat", outcome.ExitCode)
            : _strings.Format("Install_FailedWithReasonFormat", tail);
    }

    private async Task<long?> ReadTotalBytesAsync(
        string executablePath,
        string accountName,
        GameVersionEntry version,
        string instanceDirectory,
        CancellationToken cancellationToken
    )
    {
        List<string> arguments =
        [
            "-app",
            AmongUsAppId.ToString(CultureInfo.InvariantCulture),
            "-depot",
            AmongUsDepotId.ToString(CultureInfo.InvariantCulture),
            "-manifest",
            version.ManifestId.ToString(CultureInfo.InvariantCulture),
            "-manifest-only",
            "-dir",
            instanceDirectory,
            "-username",
            accountName,
            "-remember-password",
            "-no-mobile",
        ];

        ExecutableOutcome outcome = await _runner.RunAsync(
            new ExecutableCommand(executablePath, arguments, null),
            null,
            cancellationToken
        );

        return outcome.ExitCode == 0 ? ExtractTotalBytes(instanceDirectory) : null;
    }

    private static long? ExtractTotalBytes(string instanceDirectory)
    {
        const string Prefix = "Total bytes on disk";

        try
        {
            foreach (
                string manifestFile in Directory.EnumerateFiles(instanceDirectory, "manifest_*.txt")
            )
            {
                foreach (string line in File.ReadLines(manifestFile))
                {
                    if (!line.StartsWith(Prefix, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    string text = line[(line.IndexOf(':', StringComparison.Ordinal) + 1)..].Trim();
                    if (
                        text.Length > 0
                        && long.TryParse(
                            text,
                            NumberStyles.AllowThousands,
                            CultureInfo.InvariantCulture,
                            out long total
                        )
                        && total > 0
                    )
                    {
                        return total;
                    }
                }
            }
        }
        catch (IOException)
        {
            // 摘要缺失或占用中时放弃大小展示，不影响下载本身
        }
        catch (UnauthorizedAccessException)
        {
            // 同上
        }

        return null;
    }

    /// <summary>尝试从字符串中解析百分比。</summary>
    /// <remarks>DepotDownloader 的输出中大部分行是 "  12.34% 文件名"，这里摘出百分比。</remarks>
    private static double? TryParsePercent(string? line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return null;
        }

        int marker = line.IndexOf('%', StringComparison.Ordinal);
        if (marker <= 0)
        {
            return null;
        }

        string text = line[..marker].Trim();
        if (
            double.TryParse(
                text,
                NumberStyles.Any,
                CultureInfo.InvariantCulture,
                out double percent
            )
        )
        {
            return Math.Clamp(percent, 0, 100);
        }

        return null;
    }
}
