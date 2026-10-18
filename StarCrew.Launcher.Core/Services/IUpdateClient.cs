namespace StarCrew.Launcher.Services;

/// <summary>客户端更新相关接口。</summary>
internal interface IUpdateClient
{
    /// <summary>当前是否经 Velopack 安装。</summary>
    bool IsInstalled { get; }

    /// <summary>当前已安装版本，未安装时为 null。</summary>
    string? CurrentVersion { get; }

    /// <summary>检查远端新版本。</summary>
    /// <returns>新版本号，无新版本时返回 null。</returns>
    Task<string?> CheckForUpdatesAsync(CancellationToken cancellationToken);

    /// <summary>下载指定版本。</summary>
    Task DownloadUpdatesAsync(
        string version,
        IProgress<int>? progress,
        CancellationToken cancellationToken
    );

    /// <summary>安排已下载版本在程序退出后应用，重启后生效。</summary>
    void PrepareUpdateForRestart(string version);
}
