using System;
using GuiPiao.Utils;

namespace GuiPiao.Model;

public enum LogLevel
{
    ALL = 0,
    INFO = 1,
    WARN = 2,
    ERROR = 3,
    FATAL = 4
}

public class LogItem
{
    public int Id { get; set; }
    public string Time { get; set; }
    public string Content { get; set; }
    public LogLevel Level { get; set; }
    public string Module { get; set; }
    public DateTime CreatedAt { get; set; }

    public string LevelDisplay => Level switch
    {
        LogLevel.INFO => "INFO",
        LogLevel.WARN => "WARN",
        LogLevel.ERROR => "ERROR",
        LogLevel.FATAL => "FATAL",
        _ => "ALL"
    };

    public string LevelColor
    {
        get
        {
            var config = ConfigManager.Instance.UISettingsService.Config;
            return Level switch
            {
                LogLevel.INFO => config.InfoColor,
                LogLevel.WARN => config.WarningColor,
                LogLevel.ERROR => config.ErrorColor,
                LogLevel.FATAL => config.FatalColor,
                _ => "#6c757d"
            };
        }
    }
}

public class LogConfig
{
    public LogLevel MinLogLevel { get; set; } = LogLevel.INFO;
    public bool AutoCleanup { get; set; } = true;
    public int RetentionDays { get; set; } = 7;
    public int MaxLogCount { get; set; } = 1000;
    public string LogFilePath { get; set; }
    public long CurrentLogFileSize { get; set; }
}