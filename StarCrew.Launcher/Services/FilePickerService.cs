using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace StarCrew.Launcher.Services;

/// <summary>系统文件选择框实现：需要宿主窗口句柄才能弹在当前窗口前。</summary>
internal sealed class FilePickerService : IFilePickerService
{
    private nint _windowHandle;

    public void AttachHostWindow(nint windowHandle) => _windowHandle = windowHandle;

    public async Task<string?> PickGameExeAsync()
    {
        FileOpenPicker picker = new() { SuggestedStartLocation = PickerLocationId.ComputerFolder };
        picker.FileTypeFilter.Add(".exe");
        InitializeWithWindow.Initialize(picker, _windowHandle);

        StorageFile? file = await picker.PickSingleFileAsync();
        return file?.Path;
    }
}
