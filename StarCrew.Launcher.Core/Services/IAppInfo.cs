namespace StarCrew.Launcher.Services;

/// <summary>应用信息接缝：提供启动器自身版本。</summary>
internal interface IAppInfo
{
    /// <summary>当前启动器版本（Major.Minor.Build），无法获知时为 null。</summary>
    string? Version { get; }
}
