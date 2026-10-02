using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.UIA3;

namespace StarCrew.Launcher.UITests;

/// <summary>
/// 主窗口冒烟测试：只确认窗口出现、标题正确、“启动游戏”按钮存在。
/// 全程不点击任何按钮（点了会真的拉起游戏）。
/// </summary>
public sealed class SmokeTests
{
    private const string SolutionFileName = "StarCrew.Launcher.slnx";
    private const string LauncherExeFileName = "StarCrew.Launcher.exe";
    private const string LaunchButtonText = "启动游戏";

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
            AutomationElement? launchButton = mainWindow
                .FindAllDescendants(cf => cf.ByControlType(ControlType.Button))
                .FirstOrDefault(IsLaunchButton);
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

    private static bool IsLaunchButton(AutomationElement element)
    {
        return ContainsLaunchText(element.Name) || ContainsLaunchText(element.AutomationId);
    }

    private static bool ContainsLaunchText(string? value)
    {
        return value is not null && value.Contains(LaunchButtonText, StringComparison.Ordinal);
    }

    /// <summary>
    /// 从测试输出目录向上找到仓库根（以 slnx 为标记），再拼出主程序 exe 路径。
    /// 优先 Release，不存在则用 Debug；都不存在则失败并提示先构建主工程。
    /// </summary>
    private static string ResolveLauncherExe()
    {
        string repositoryRoot = FindRepositoryRoot();
        string releaseExe = Path.Combine(
            repositoryRoot,
            "StarCrew.Launcher",
            "bin",
            "Release",
            "net10.0-windows10.0.26100.0",
            "win-x64",
            LauncherExeFileName
        );
        if (File.Exists(releaseExe))
        {
            return releaseExe;
        }

        string debugExe = Path.Combine(
            repositoryRoot,
            "StarCrew.Launcher",
            "bin",
            "Debug",
            "net10.0-windows10.0.26100.0",
            "win-x64",
            LauncherExeFileName
        );
        Assert.True(
            File.Exists(debugExe),
            "StarCrew.Launcher.exe not found in Release or Debug output. Build the main project first (test-ui.bat does this)."
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
