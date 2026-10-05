using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using StarCrew.Launcher.Models;
using StarCrew.Launcher.Services;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace StarCrew.Launcher.Views;

public sealed partial class HomePage : Page
{
    private readonly GameLauncher _launcher = new(
        new SteamGameLocator(new WindowsSteamEnvironment()),
        new ProcessStarter()
    );

    /// <summary>宿主窗口句柄，由宿主在导航完成后设置，供文件选择框挂靠。</summary>
    internal nint WindowHandle { get; set; }

    public HomePage()
    {
        InitializeComponent();
    }

    /// <summary>需要右上 Toast 时通知宿主窗口，参数依次为标题、内容、级别。</summary>
    internal event Action<string, string, InfoBarSeverity>? NotifyRequested;

    private async void LaunchButton_Click(object sender, RoutedEventArgs e)
    {
        LaunchButton.IsEnabled = false;
        LaunchButton.Content = "正在启动...";

        try
        {
            LaunchResult result = await _launcher.LaunchAsync();
            if (result.IsSuccess)
            {
                NotifyRequested?.Invoke("启动成功", result.Message, InfoBarSeverity.Success);
                return;
            }

            await LaunchByManualPickAsync();
        }
        finally
        {
            LaunchButton.IsEnabled = true;
            LaunchButton.Content = "启动游戏";
        }
    }

    /// <summary>自动定位失败时让用户手动选 exe 并启动。</summary>
    private async Task LaunchByManualPickAsync()
    {
        string? picked = await PickGameExeAsync();
        if (picked is null)
        {
            NotifyRequested?.Invoke(
                "已取消",
                "已取消选择，点击启动游戏可重试。",
                InfoBarSeverity.Informational
            );
            return;
        }

        if (_launcher.TryLaunchExe(picked, out string? error))
        {
            NotifyRequested?.Invoke("启动成功", "已启动手动选择的游戏。", InfoBarSeverity.Success);
        }
        else
        {
            NotifyRequested?.Invoke(
                "启动失败",
                $"启动所选文件时发生错误：{error}",
                InfoBarSeverity.Error
            );
        }
    }

    private async Task<string?> PickGameExeAsync()
    {
        FileOpenPicker picker = new() { SuggestedStartLocation = PickerLocationId.ComputerFolder };
        picker.FileTypeFilter.Add(".exe");
        InitializeWithWindow.Initialize(picker, WindowHandle);

        StorageFile? file = await picker.PickSingleFileAsync();
        return file?.Path;
    }
}
