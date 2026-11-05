using StarportAlliance.StarCrewLauncher.Models;

namespace StarportAlliance.StarCrewLauncher.Services;

/// <summary>全局实例状态存储器。</summary>
internal interface IInstanceStore
{
    /// <summary>读取实例当前状态。</summary>
    LauncherState Load();

    /// <summary>写入实例当前状态。</summary>
    /// <returns>保存成功返回 true；IO 失败时返回 false 并保留原文件。</returns>
    bool Save(LauncherState state);
}
