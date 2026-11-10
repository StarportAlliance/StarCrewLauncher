using Velopack;

namespace StarportAlliance.StarCrewLauncher.Services;

/// <summary>基于 Velopack 的客户端更新检查相关接口。</summary>
internal sealed class VelopackUpdateClient : IUpdateClient
{
    /// <summary>默认更新源 URL。</summary>
    public const string DefaultFeedUrl = "https://asset.starbridge.ink/launcher/updates";

    private readonly UpdateManager _manager;
    private readonly INetworkSettings _network;
    private readonly IAppStrings _strings;
    private UpdateInfo? _pendingUpdate;

    public VelopackUpdateClient(string feedUrl, INetworkSettings network, IAppStrings strings)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(feedUrl);
        ArgumentNullException.ThrowIfNull(network);
        ArgumentNullException.ThrowIfNull(strings);
        _manager = new UpdateManager(feedUrl);
        _network = network;
        _strings = strings;
    }

    public bool IsInstalled => _manager.IsInstalled;

    public string? CurrentVersion => _manager.CurrentVersion?.ToString();

    public async Task<string?> CheckForUpdatesAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        // Velopack 不支持取消，超时只能放弃等待
        Task<UpdateInfo?> check = _manager.CheckForUpdatesAsync();
        Task timeout = Task.Delay(TimeSpan.FromSeconds(_network.TimeoutSeconds), cancellationToken);
        if (await Task.WhenAny(check, timeout).ConfigureAwait(false) != check)
        {
            cancellationToken.ThrowIfCancellationRequested();
            throw new TimeoutException(_strings.GetString("Network_Timeout"));
        }

        _pendingUpdate = await check.ConfigureAwait(false);
        return _pendingUpdate?.TargetFullRelease.Version.ToString();
    }

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

    public void PrepareUpdateForRestart(string version)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(version);
        UpdateInfo pending = RequirePending(version);
        _manager.WaitExitThenApplyUpdates(pending.TargetFullRelease, restart: true);
    }

    /// <summary>取出与指定版本匹配的待应用更新。</summary>
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
