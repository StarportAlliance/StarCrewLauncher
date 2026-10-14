namespace StarCrew.Launcher.Services;

/// <summary>加速源可选项：官方直连、gh-proxy、自定义前缀。</summary>
internal enum DownloadAccelerator
{
    Official,
    GhProxy,
    Custom,
}

/// <summary>下载加速设置：用于 GitHub 资源（首次获取 DepotDownloader 等）的下载链路。</summary>
internal interface INetworkSettings
{
    DownloadAccelerator Accelerator { get; set; }

    /// <summary>自定义前缀（如 "https://example.com"），为空时不走任何加速。</summary>
    string CustomPrefix { get; set; }
}
