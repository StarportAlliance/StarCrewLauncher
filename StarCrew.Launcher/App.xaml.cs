using Microsoft.UI.Xaml;

namespace StarCrew.Launcher;

/// <summary>应用程序入口，负责创建并激活主窗口。</summary>
public sealed partial class App : Application
{
    private Window? _window;

    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _window = new MainWindow();
        _window.Activate();
    }
}
