namespace StarportAlliance.StarCrewLauncher.Services;

/// <summary>网络请求相关设置。</summary>
internal interface INetworkSettings
{
    /// <summary>SDD 在线登录校验结果的缓存时长（秒），范围 0-86400；为 0 时禁用缓存。</summary>
    int AuthCacheSeconds { get; set; }

    /// <summary>短时网络请求的超时时间（秒），范围 1-30；长任务（下载、扫码登录）不受影响。</summary>
    int TimeoutSeconds { get; set; }
}
