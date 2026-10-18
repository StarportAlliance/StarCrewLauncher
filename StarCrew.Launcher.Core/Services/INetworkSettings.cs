namespace StarCrew.Launcher.Services;

/// <summary>加速源可选项。</summary>
internal enum DownloadAccelerator
{
    Official,
    GhProxy,
    Custom,
}

/// <summary>下载加速设置。</summary>
internal interface INetworkSettings
{
    /// <summary>下载加速方式。</summary>
    DownloadAccelerator Accelerator { get; set; }

    /// <summary>自定义前缀，为空时不走任何加速。</summary>
    string CustomPrefix { get; set; }
}
