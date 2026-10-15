using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.UIA3;

namespace StarCrew.Launcher.UITests;

/// <summary>主窗口冒烟：断言窗口+标题+启动按钮存在；全程零点击（点击会真拉起游戏）。</summary>
public sealed class SmokeTests
{
    private const string SolutionFileName = "StarCrew.Launcher.slnx";
    private const string LauncherExeFileName = "StarCrew.Launcher.exe";
    private const string DownloadButtonText = "下载游戏";
    private const string TargetFramework = "net10.0-windows10.0.26100.0";
    private const string Architecture = "win-x64";

    [Fact]
    public void MainWindow_Shows_WithLaunchButton()
    {
        string exePath = ResolveLauncherExe();
        Application app = Application.Launch(exePath, string.Empty);
        try
        {
            bool mainHandleAppeared = app.WaitWhileMainHandleIsMissing(TimeSpan.FromSeconds(60));
            Assert.True(mainHandleAppeared);
            using UIA3Automation automation = new();
            Window? mainWindow = app.GetMainWindow(automation, TimeSpan.FromSeconds(10));
            Assert.NotNull(mainWindow);
            Assert.Contains("StarCrew", mainWindow.Title, StringComparison.Ordinal);
            // Frame 导航异步完成：首屏内容比窗口句柄晚出现，轮询等按钮现身。
            AutomationElement? launchButton = null;
            DateTime deadline = DateTime.UtcNow.AddSeconds(10);
            while (launchButton is null && DateTime.UtcNow < deadline)
            {
                launchButton = mainWindow
                    .FindAllDescendants(cf => cf.ByControlType(ControlType.Button))
                    .FirstOrDefault(IsLaunchButton);
                if (launchButton is null)
                {
                    Thread.Sleep(200);
                }
            }

            Assert.NotNull(launchButton);
        }
        finally
        {
            if (!app.HasExited)
            {
                app.Kill();
            }

            app.Dispose();
        }
    }

    private const string LaunchButtonText = "启动游戏";

    // 启动 or 下载 文案随实例状态变化，两者之一即通过。
    private static bool IsLaunchButton(AutomationElement element) =>
        ContainsLaunchText(element.Name) || ContainsLaunchText(element.AutomationId);

    private static bool ContainsLaunchText(string? value) =>
        value is not null
        && (
            value.Contains(LaunchButtonText, StringComparison.Ordinal)
            || value.Contains(DownloadButtonText, StringComparison.Ordinal)
        );

    /// <summary>以 slnx 为标记找仓库根，拼主程序 exe 路径；按 Release、ReleaseGate、Debug 顺序找。</summary>
    private static string ResolveLauncherExe()
    {
        string repositoryRoot = FindRepositoryRoot();
        string releaseExe = Path.Combine(
            repositoryRoot,
            "StarCrew.Launcher",
            "bin",
            "Release",
            TargetFramework,
            Architecture,
            LauncherExeFileName
        );
        if (File.Exists(releaseExe))
        {
            return releaseExe;
        }

        string releaseGateExe = Path.Combine(
            repositoryRoot,
            "StarCrew.Launcher",
            "bin",
            "ReleaseGate",
            TargetFramework,
            Architecture,
            LauncherExeFileName
        );
        if (File.Exists(releaseGateExe))
        {
            return releaseGateExe;
        }

        string debugExe = Path.Combine(
            repositoryRoot,
            "StarCrew.Launcher",
            "bin",
            "Debug",
            TargetFramework,
            Architecture,
            LauncherExeFileName
        );
        Assert.True(
            File.Exists(debugExe),
            "StarCrew.Launcher.exe not found in Release, ReleaseGate or Debug output. Build the main project first (check.bat does this)."
        );
        return debugExe;
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? current = new(AppContext.BaseDirectory);
        while (
            current is not null && !File.Exists(Path.Combine(current.FullName, SolutionFileName))
        )
        {
            current = current.Parent;
        }

        Assert.NotNull(current);
        return current.FullName;
    }
}
