namespace StarCrew.Launcher.Services;

/// <summary>更新客户端接缝：生产用 <see cref="VelopackUpdateClient" />，测试用替身。</summary>
internal interface IUpdateClient
{
    /// <summary>当前是否经 Velopack 安装，开发直跑时为 false。</summary>
    bool IsInstalled { get; }

    /// <summary>当前已安装版本，未安装时为 null。</summary>
    string? CurrentVersion { get; }

    /// <summary>检查远端新版本，有更新时返回版本号，否则返回 null。</summary>
    Task<string?> CheckForUpdatesAsync(CancellationToken cancellationToken);

    /// <summary>下载指定版本，下载完成后需再安排应用才会生效。</summary>
    Task DownloadUpdatesAsync(
        string version,
        IProgress<int>? progress,
        CancellationToken cancellationToken
    );

    /// <summary>安排已下载版本在程序退出后应用，重启后生效。</summary>
    void PrepareUpdateForRestart(string version);
}
