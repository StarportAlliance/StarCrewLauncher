namespace StarCrew.Launcher.Services;

/// <summary>文件选择接缝：生产用系统选择框，测试用替身。</summary>
internal interface IFilePickerService
{
    /// <summary>挂靠宿主窗口句柄，选择框才能弹在当前窗口前。</summary>
    void AttachHostWindow(nint windowHandle);

    /// <summary>让用户选择一个游戏 exe，取消时返回 null。</summary>
    Task<string?> PickGameExeAsync();
}
