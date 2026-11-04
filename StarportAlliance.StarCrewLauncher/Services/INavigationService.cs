using StarportAlliance.StarCrewLauncher.Models;

namespace StarportAlliance.StarCrewLauncher.Services;

/// <summary>页面导航服务。</summary>
internal interface INavigationService
{
    /// <summary>导航到指定 <paramref name="target"/>。</summary>
    void NavigateTo(NavigationTarget target);
}
