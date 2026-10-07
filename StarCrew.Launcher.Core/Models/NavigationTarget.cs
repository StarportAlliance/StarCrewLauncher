namespace StarCrew.Launcher.Models;

/// <summary>可导航的页面目标，导航实现负责映射到具体页面与视图模型。</summary>
internal enum NavigationTarget
{
    /// <summary>首页（启动游戏）。</summary>
    Home,

    /// <summary>设置主页。</summary>
    Settings,

    /// <summary>设置 - 通用。</summary>
    Appearance,

    /// <summary>设置 - 关于。</summary>
    About,
}
