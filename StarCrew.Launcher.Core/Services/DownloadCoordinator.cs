using System.Globalization;
using StarCrew.Launcher.Models;

namespace StarCrew.Launcher.Services;

/// <summary>下载协调者：选定版本 → 确认工具可用 → 创建目录与实例 → 跑 DD。</summary>
internal sealed class DownloadCoordinator : IDownloadCoordinator
{
    private const int AmongUsAppId = 945360;
    private const int AmongUsDepotId = 945361;

    private readonly IInstanceStore _store;
    private readonly IDepotDownloaderProvisioner _provisioner;
    private readonly IDepotDownloaderRunner _runner;
    private readonly string _instancesRoot;

    public DownloadCoordinator(
        IInstanceStore store,
        IDepotDownloaderProvisioner provisioner,
        IDepotDownloaderRunner runner
    )
        : this(store, provisioner, runner, AppPaths.InstancesDirectory) { }

    internal DownloadCoordinator(
        IInstanceStore store,
        IDepotDownloaderProvisioner provisioner,
        IDepotDownloaderRunner runner,
        string instancesRoot
    )
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(provisioner);
        ArgumentNullException.ThrowIfNull(runner);
        ArgumentException.ThrowIfNullOrWhiteSpace(instancesRoot);
        _store = store;
        _provisioner = provisioner;
        _runner = runner;
        _instancesRoot = instancesRoot;
    }

    /// <inheritdoc />
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
            return new GameInstallResult(false, "请先在「游戏下载」页登录 Steam 账号。");
        }

        progress?.Report(
            new DownloadProgressInfo(DownloadStage.PreparingTool, 0, "准备下载工具...")
        );
        DepotDownloaderEnsureResult ensure = await _provisioner.EnsureAsync(
            progress,
            cancellationToken
        );
        if (!ensure.IsSuccess || string.IsNullOrEmpty(ensure.ExecutablePath))
        {
            return new GameInstallResult(false, ensure.Message);
        }

        // 新实例：直接用 GUID 当目录。记录只在下载完成后落库：任何原因打断都不留记录。
        Guid instanceGuid = Guid.NewGuid();
        string instanceId = instanceGuid.ToString("N");
        string instanceDirectory = Path.Combine(_instancesRoot, instanceId);
        Directory.CreateDirectory(instanceDirectory);

        // manifest-only 预跑是唯一能提前拿到总大小的通道：DD 只在那一模式把 manifest 摘要落成文本。
        progress?.Report(
            new DownloadProgressInfo(DownloadStage.PreparingTool, 0, "正在核对资源大小...")
        );
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
                        $"正在下载：{version.VersionLabel}（{percent.Value:F1}%）",
                        totalBytes
                    )
                );
            }
        });

        try
        {
            totalBytes = await ReadTotalBytesAsync(
                ensure.ExecutablePath,
                accountName,
                version,
                instanceDirectory,
                cancellationToken
            );

            ExecutableOutcome outcome = await _runner.RunAsync(
                new ExecutableCommand(ensure.ExecutablePath, arguments, null),
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
                return await FailAsync("下载完成，但未找到 Among Us.exe。");
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
                    $"「{version.VersionLabel}」安装完成。"
                )
            );
            return new GameInstallResult(true, $"{version.VersionLabel} 已下载完成。", ready);
        }
        catch (OperationCanceledException)
        {
            return await FailAsync("下载已取消。");
        }
        catch (Exception ex)
        {
            return await FailAsync($"下载失败：{ex.Message}");
        }

        // 取消、报错、校验失败一律清掉目录且不落库：实例列表里看不到，重装即是重新开始。
        async Task<GameInstallResult> FailAsync(string message)
        {
            TryDeleteDirectory(instanceDirectory);
            progress?.Report(new DownloadProgressInfo(DownloadStage.Failed, 0, message));
            return new GameInstallResult(false, message);
        }
    }

    /// <summary>尽力删除中断下载留下的目录；失败（文件被占用等）只留下无记录的孤儿目录，不影响流程。</summary>
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
            // 文件被占用等：删不掉就算了，目录里没有实例记录，用户侧不可见。
        }
        catch (UnauthorizedAccessException)
        {
            // 同上。
        }
    }

    private static string ComposeFailureText(ExecutableOutcome outcome)
    {
        string? tail = outcome.OutputLines.LastOrDefault(l => !string.IsNullOrWhiteSpace(l));
        return tail is null ? $"下载失败，退出码：{outcome.ExitCode}。" : $"下载失败：{tail}";
    }

    /// <summary>
    /// 跑一次 -manifest-only：DD 会把 manifest 摘要写成 manifest_{depot}_{manifest}.txt，
    /// 其中的 "Total bytes on disk" 就是本次要下载的总字节数（与 DD 算百分比的分母同源）。
    /// 失败一律返回 null，调用方退回只显示百分比。
    /// </summary>
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

    /// <summary>从实例目录的 manifest 摘要文本读总大小；读不到或解析不出都返回 null。</summary>
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
            // 摘要缺失或占用中时放弃大小展示，不影响下载本身。
        }
        catch (UnauthorizedAccessException)
        {
            // 同上。
        }

        return null;
    }

    /// <summary>DD 的输出中大部分行是 "  12.34% 文件名"，这里摘出百分比。</summary>
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
