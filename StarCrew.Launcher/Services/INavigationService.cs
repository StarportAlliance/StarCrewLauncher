using StarCrew.Launcher.Models;

namespace StarCrew.Launcher.Services;

/// <summary>页面导航服务。</summary>
internal interface INavigationService
{
    /// <summary>导航到指定 <paramref name="target"/>。</summary>
    void NavigateTo(NavigationTarget target);
}
