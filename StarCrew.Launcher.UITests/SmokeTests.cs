using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.UIA3;

namespace StarCrew.Launcher.UITests;

// 全程零点击
public sealed class SmokeTests
{
    private const string SolutionFileName = "StarCrew.Launcher.slnx";
    private const string LauncherExeFileName = "StarCrew.Launcher.exe";
    private const string DownloadButtonText = "下载游戏";

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
    private const string LaunchButtonAutomationId = "LaunchButton";

    // WinUI 部分元素不暴露 Name 属性，直接读会抛 PropertyNotSupportedException。
    private static bool IsLaunchButton(AutomationElement element) =>
        element.Properties.AutomationId.ValueOrDefault == LaunchButtonAutomationId
        || ContainsLaunchText(element.Properties.Name.ValueOrDefault)
        || ContainsLaunchText(element.Properties.AutomationId.ValueOrDefault);

    private static bool ContainsLaunchText(string? value) =>
        value is not null
        && (
            value.Contains(LaunchButtonText, StringComparison.Ordinal)
            || value.Contains(DownloadButtonText, StringComparison.Ordinal)
        );

    private static string ResolveLauncherExe()
    {
        string repositoryRoot = FindRepositoryRoot();
        string[] configurations = ["Release", "ReleaseGate", "Debug"];
        foreach (string configuration in configurations)
        {
            string binDirectory = Path.Combine(
                repositoryRoot,
                "StarCrew.Launcher",
                "bin",
                configuration
            );
            if (!Directory.Exists(binDirectory))
            {
                continue;
            }

            string? exe = Directory
                .EnumerateFiles(binDirectory, LauncherExeFileName, SearchOption.AllDirectories)
                .FirstOrDefault();
            if (exe is not null)
            {
                return exe;
            }
        }

        throw new Xunit.Sdk.XunitException(
            "StarCrew.Launcher.exe not found in Release, ReleaseGate or Debug output. Build the main project first (test.bat does this)."
        );
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
