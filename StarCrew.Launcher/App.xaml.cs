using Microsoft.UI.Xaml;
using Velopack;

namespace StarCrew.Launcher;

/// <summary>应用程序入口，负责创建并激活主窗口。</summary>
public sealed partial class App : Application
{
    private Window? _window;

    public App()
    {
        // 必须是第一行：Velopack 靠它处理安装/更新/卸载钩子并初始化定位器，
        // 更新客户端（UpdateManager）依赖此次初始化，挪后或遗漏会导致更新不可用。
        VelopackApp.Build().Run();
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _window = new MainWindow();
        _window.Activate();
    }
}
