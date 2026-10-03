using Velopack;

namespace StarCrew.Launcher.Services;

/// <summary>基于 Velopack 的更新客户端：把远端静态更新源的检查/下载/应用转接为字符串版本语义。</summary>
internal sealed class VelopackUpdateClient : IUpdateClient
{
    /// <summary>默认更新源：自建静态 HTTP，vpk 打包产物（Releases/ 下的 releases.win.json 与 nupkg）部署到该目录即生效。</summary>
    public const string DefaultFeedUrl = "https://asset.starbridge.ink/launcher/updates";

    private readonly UpdateManager _manager;
    private UpdateInfo? _pendingUpdate;

    /// <summary>使用指定的更新源地址构造客户端。</summary>
    public VelopackUpdateClient(string feedUrl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(feedUrl);
        _manager = new UpdateManager(feedUrl);
    }

    /// <inheritdoc />
    public bool IsInstalled => _manager.IsInstalled;

    /// <inheritdoc />
    public string? CurrentVersion => _manager.CurrentVersion?.ToString();

    /// <inheritdoc />
    public async Task<string?> CheckForUpdatesAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _pendingUpdate = await _manager.CheckForUpdatesAsync().ConfigureAwait(false);
        return _pendingUpdate?.TargetFullRelease.Version.ToString();
    }

    /// <inheritdoc />
    public async Task DownloadUpdatesAsync(
        string version,
        IProgress<int>? progress,
        CancellationToken cancellationToken
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(version);
        UpdateInfo pending = RequirePending(version);
        Action<int>? report = null;
        if (progress is not null)
        {
            report = progress.Report;
        }

        await _manager
            .DownloadUpdatesAsync(pending, report, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public void PrepareUpdateForRestart(string version)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(version);
        UpdateInfo pending = RequirePending(version);
        _manager.WaitExitThenApplyUpdates(pending.TargetFullRelease, restart: true);
    }

    /// <summary>取出与指定版本匹配的待应用更新，对不上时说明调用顺序错误。</summary>
    private UpdateInfo RequirePending(string version)
    {
        if (
            _pendingUpdate is not null
            && _pendingUpdate.TargetFullRelease.Version.ToString() == version
        )
        {
            return _pendingUpdate;
        }

        throw new InvalidOperationException($"没有版本 {version} 的待应用更新，请先检查更新。");
    }
}
