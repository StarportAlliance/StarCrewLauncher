using StarCrew.Launcher.Models;

namespace StarCrew.Launcher.Services;

/// <summary>全局实例状态存储接缝：生产用 <see cref="InstanceStore" />，测试用替身。</summary>
internal interface IInstanceStore
{
    /// <summary>读取当前状态；状态文件不存在或损坏时回退到空态。</summary>
    LauncherState Load();

    /// <summary>整体写入当前状态，写失败由调用方决定是否报错。</summary>
    void Save(LauncherState state);
}
