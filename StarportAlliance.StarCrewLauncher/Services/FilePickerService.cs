using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace StarportAlliance.StarCrewLauncher.Services;

internal sealed class FilePickerService : IFilePickerService
{
    private nint _windowHandle;

    public void AttachHostWindow(nint windowHandle) => _windowHandle = windowHandle;

    public Task<string?> PickGameExeAsync() => PickSingleFileAsync(".exe");

    public Task<string?> PickBackgroundImageAsync() =>
        PickSingleFileAsync(".png", ".jpg", ".jpeg", ".bmp");

    private async Task<string?> PickSingleFileAsync(params string[] extensions)
    {
        FileOpenPicker picker = new() { SuggestedStartLocation = PickerLocationId.ComputerFolder };
        foreach (string extension in extensions)
        {
            picker.FileTypeFilter.Add(extension);
        }

        InitializeWithWindow.Initialize(picker, _windowHandle);

        StorageFile? file = await picker.PickSingleFileAsync();
        return file?.Path;
    }
}
