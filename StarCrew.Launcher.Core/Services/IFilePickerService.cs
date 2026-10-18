namespace StarCrew.Launcher.Services;

/// <summary>文件选择器。</summary>
internal interface IFilePickerService
{
    /// <summary>挂靠宿主窗口句柄。</summary>
    /// <remarks>需要挂靠宿主窗口句柄，选择框才能弹在当前窗口前。</remarks>
    void AttachHostWindow(nint windowHandle);

    /// <summary>让用户选择一个游戏 exe 文件。</summary>
    /// <returns>选择的 exe 文件路径，取消时返回 null。</returns>
    Task<string?> PickGameExeAsync();
}
