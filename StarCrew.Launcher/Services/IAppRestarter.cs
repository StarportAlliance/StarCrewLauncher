namespace StarCrew.Launcher.Services;

/// <summary>应用自重启。</summary>
internal interface IAppRestarter
{
    /// <summary>拉起一个新的应用实例，并在其启动后退出当前进程。</summary>
    void Restart();
}
