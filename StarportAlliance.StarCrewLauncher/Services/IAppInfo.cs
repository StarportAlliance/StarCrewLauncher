namespace StarportAlliance.StarCrewLauncher.Services;

/// <summary>启动器版本信息。</summary>
internal interface IAppInfo
{
    /// <summary>当前启动器版本，无法获知时为 null。</summary>
    string? Version { get; }

    /// <summary>当前是否为开发版（未经 Velopack 安装），此时标题栏与关于页显示 Alpha 标记。</summary>
    bool IsDevelopment { get; }
}
