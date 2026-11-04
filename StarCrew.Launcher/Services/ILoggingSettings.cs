using Serilog.Events;

namespace StarCrew.Launcher.Services;

/// <summary>日志设置接口。</summary>
internal interface ILoggingSettings
{
    /// <summary>是否启用日志记录；关闭后不产生任何日志文件。</summary>
    bool Enabled { get; set; }

    /// <summary>写入日志的最低等级。</summary>
    LogEventLevel Level { get; set; }

    /// <summary>按天滚动的日志保留份数，最小为 1。</summary>
    int RetentionDays { get; set; }
}
