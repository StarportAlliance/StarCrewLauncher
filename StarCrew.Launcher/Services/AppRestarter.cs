using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;

namespace StarCrew.Launcher.Services;

internal sealed class AppRestarter : IAppRestarter
{
    private readonly ILogger<AppRestarter> _logger;

    public AppRestarter(ILogger<AppRestarter> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    public void Restart()
    {
        string path =
            Environment.ProcessPath
            ?? throw new InvalidOperationException("无法确定当前进程路径。");
        _logger.LogInformation("重启应用以应用日志设置");
        Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        Application.Current.Exit();
    }
}
