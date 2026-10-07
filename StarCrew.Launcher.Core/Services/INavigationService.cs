using StarCrew.Launcher.Models;

namespace StarCrew.Launcher.Services;

/// <summary>页面导航接缝：调用方只声明目标，页面类型与转场由界面层决定。</summary>
internal interface INavigationService
{
    /// <summary>导航到指定目标；目标已是当前页时忽略。</summary>
    void NavigateTo(NavigationTarget target);
}
