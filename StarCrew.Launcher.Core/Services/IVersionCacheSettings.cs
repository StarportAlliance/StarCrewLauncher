namespace StarCrew.Launcher.Services;

/// <summary>版本列表缓存设置。</summary>
internal interface IVersionCacheSettings
{
    /// <summary>版本列表缓存的有效时长（小时），范围 0-720；为 0 时禁用缓存。</summary>
    int MaxAgeHours { get; set; }
}
