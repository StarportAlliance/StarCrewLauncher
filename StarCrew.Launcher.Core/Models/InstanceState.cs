namespace StarCrew.Launcher.Models;

/// <summary>实例状态：就绪可用，或因异常中断/删除而不完整。</summary>
internal enum InstanceState
{
    /// <summary>实例完整可启动。</summary>
    Ready,

    /// <summary>安装未完成或部分损坏，可尝试修复。</summary>
    Incomplete,
}
