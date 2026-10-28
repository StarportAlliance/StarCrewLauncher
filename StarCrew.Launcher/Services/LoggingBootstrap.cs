using System.Globalization;
using Serilog;

namespace StarCrew.Launcher.Services;

internal static class LoggingBootstrap
{
    /// <summary>按设置初始化全局日志；未启用时保持静默，不产生任何日志文件。</summary>
    public static void Configure(ILoggingSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        Directory.CreateDirectory(AppPaths.LogDirectory);

        LoggerConfiguration config = new();
        if (settings.Enabled)
        {
            config = config
                .MinimumLevel.Is(settings.Level)
                .WriteTo.File(
                    Path.Combine(AppPaths.LogDirectory, "scl-.log"),
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: settings.RetentionDays,
                    formatProvider: CultureInfo.InvariantCulture,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}"
                );
        }

        Log.Logger = config.CreateLogger();
    }
}
