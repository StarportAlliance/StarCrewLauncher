using StarCrew.Launcher.Models;

namespace StarCrew.Launcher.Services;

/// <summary>全局实例状态存储器。</summary>
internal interface IInstanceStore
{
    /// <summary>读取实例当前状态。</summary>
    LauncherState Load();

    /// <summary>写入实例当前状态。</summary>
    void Save(LauncherState state);
}
