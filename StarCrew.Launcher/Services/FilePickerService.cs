using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace StarCrew.Launcher.Services;

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
