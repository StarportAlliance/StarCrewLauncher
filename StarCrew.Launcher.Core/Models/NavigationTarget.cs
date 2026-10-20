namespace StarCrew.Launcher.Models;

/// <summary>可导航的页面目标。</summary>
internal enum NavigationTarget
{
    /// <summary>首页。</summary>
    Home,

    /// <summary>实例管理页。</summary>
    Instances,

    /// <summary>游戏下载页。</summary>
    Download,

    /// <summary>游戏下载 - 安装详情。</summary>
    Install,

    /// <summary>设置主页。</summary>
    Settings,

    /// <summary>设置 - 通用。</summary>
    Appearance,

    /// <summary>设置 - 关于。</summary>
    About,
}
