namespace StarCrew.Launcher.Services;

/// <summary>启动器版本信息。</summary>
internal interface IAppInfo
{
    /// <summary>当前启动器版本，无法获知时为 null。</summary>
    string? Version { get; }
}
