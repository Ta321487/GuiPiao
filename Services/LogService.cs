using GuiPiao.DataAccess;
using GuiPiao.Model;
using GuiPiao.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace GuiPiao.Services
{
    public class LogService
    {
        private readonly Lazy<LogRepository> _logRepository;
        private LogConfig _config;
        private const string ConfigFileName = "logsettings.json";

        /// <summary>
        /// 日志变更事件，当日志被添加、删除或清空时触发
        /// </summary>
        public event EventHandler? LogsChanged;

        public LogService()
        {
            _logRepository = new Lazy<LogRepository>(() => new LogRepository());
            _config = LoadConfig();
        }

        /// <summary>
        /// 触发日志变更事件
        /// </summary>
        private void OnLogsChanged()
        {
            LogsChanged?.Invoke(this, EventArgs.Empty);
        }

        public LogConfig Config => _config;

        /// <summary>
        /// 从JSON文件加载配置，如果不存在则使用默认值
        /// </summary>
        private LogConfig LoadConfig()
        {
            // 从JSON加载配置
            var config = JsonConfigManager.Instance.LoadConfig(ConfigFileName, new LogConfig());

            // 设置日志文件路径（固定路径，不保存到配置）
            config.LogFilePath = ConfigManager.Instance.LogFilePath;

            // 计算当前日志文件大小
            try
            {
                if (File.Exists(config.LogFilePath))
                {
                    var fileInfo = new FileInfo(config.LogFilePath);
                    config.CurrentLogFileSize = fileInfo.Length;
                }
            }
            catch
            {
                config.CurrentLogFileSize = 0;
            }

            return config;
        }

        /// <summary>
        /// 保存配置到JSON文件
        /// </summary>
        public void SaveConfig(LogConfig config)
        {
            // 更新内存中的配置
            _config.MinLogLevel = config.MinLogLevel;
            _config.AutoCleanup = config.AutoCleanup;
            _config.RetentionDays = config.RetentionDays;
            _config.MaxLogCount = config.MaxLogCount;

            // 保存到JSON文件（不包含运行时属性）
            var configToSave = new LogConfig
            {
                MinLogLevel = config.MinLogLevel,
                AutoCleanup = config.AutoCleanup,
                RetentionDays = config.RetentionDays,
                MaxLogCount = config.MaxLogCount
            };

            JsonConfigManager.Instance.SaveConfig(ConfigFileName, configToSave);
        }

        public async Task LogAsync(LogLevel level, string module, string content)
        {
            if (level < _config.MinLogLevel)
                return;

            var log = new LogItem
            {
                Time = DateTime.Now.ToString("HH:mm:ss"),
                Level = level,
                Module = module,
                Content = content,
                CreatedAt = DateTime.Now
            };

            await _logRepository.Value.AddLogAsync(log);

            await WriteToFileAsync(log);

            // 触发日志变更事件
            OnLogsChanged();
        }

        public void Log(LogLevel level, string module, string content)
        {
            _ = LogAsync(level, module, content);
        }

        public void Info(string module, string content)
        {
            Log(LogLevel.INFO, module, content);
        }

        public void Warn(string module, string content)
        {
            Log(LogLevel.WARN, module, content);
        }

        public void Error(string module, string content)
        {
            Log(LogLevel.ERROR, module, content);
        }

        public void Fatal(string module, string content)
        {
            Log(LogLevel.FATAL, module, content);
        }

        private async Task WriteToFileAsync(LogItem log)
        {
            try
            {
                string logDir = Path.GetDirectoryName(_config.LogFilePath);
                if (!Directory.Exists(logDir))
                {
                    Directory.CreateDirectory(logDir);
                }

                string logLine = $"[{log.Time}] [{log.LevelDisplay}] [{log.Module}] {log.Content}{Environment.NewLine}";
                await File.AppendAllTextAsync(_config.LogFilePath, logLine);
            }
            catch
            {
            }
        }

        public async Task<IEnumerable<LogItem>> GetLogsAsync(
            LogLevel? level = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            string keyword = null,
            int limit = 1000)
        {
            return await _logRepository.Value.GetLogsAsync(level, startDate, endDate, keyword, limit);
        }

        public async Task<int> GetLogCountAsync(
            LogLevel? level = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            string keyword = null)
        {
            return await _logRepository.Value.GetLogCountAsync(level, startDate, endDate, keyword);
        }

        public async Task<int> DeleteLogsOlderThanAsync(int days)
        {
            int deleted = await _logRepository.Value.DeleteLogsOlderThanAsync(days);
            // 清理日志文件中对应的记录
            await CleanupLogFileAsync(days);
            // 触发日志变更事件
            OnLogsChanged();
            return deleted;
        }

        public async Task<int> DeleteAllLogsAsync()
        {
            int deleted = await _logRepository.Value.DeleteAllLogsAsync();
            // 清空日志文件
            await ClearLogFileAsync();
            // 触发日志变更事件
            OnLogsChanged();
            return deleted;
        }

        /// <summary>
        /// 清空日志文件内容
        /// </summary>
        private async Task ClearLogFileAsync()
        {
            try
            {
                if (File.Exists(_config.LogFilePath))
                {
                    // 清空文件内容（保留文件）
                    await File.WriteAllTextAsync(_config.LogFilePath, string.Empty);
                }
            }
            catch
            {
                // 文件操作失败不抛出异常
            }
        }

        /// <summary>
        /// 清理日志文件中超过指定天数的记录
        /// </summary>
        private async Task CleanupLogFileAsync(int days)
        {
            try
            {
                if (!File.Exists(_config.LogFilePath))
                    return;

                DateTime cutoffDate = DateTime.Now.AddDays(-days);
                var lines = await File.ReadAllLinesAsync(_config.LogFilePath);
                var validLines = new List<string>();

                foreach (var line in lines)
                {
                    // 尝试解析日志行中的日期
                    if (TryParseLogDate(line, out DateTime logDate))
                    {
                        if (logDate >= cutoffDate)
                        {
                            validLines.Add(line);
                        }
                    }
                    else
                    {
                        // 无法解析日期的行保留
                        validLines.Add(line);
                    }
                }

                await File.WriteAllLinesAsync(_config.LogFilePath, validLines);
            }
            catch
            {
                // 文件操作失败不抛出异常
            }
        }

        /// <summary>
        /// 尝试从日志行解析日期
        /// </summary>
        private bool TryParseLogDate(string logLine, out DateTime date)
        {
            date = DateTime.MinValue;
            try
            {
                // 日志格式: [HH:mm:ss] [LEVEL] [Module] Content
                // 需要结合文件创建日期和日志时间
                if (logLine.Length > 10 && logLine[0] == '[')
                {
                    int endIndex = logLine.IndexOf(']');
                    if (endIndex > 1)
                    {
                        string timeStr = logLine.Substring(1, endIndex - 1);
                        if (TimeSpan.TryParse(timeStr, out TimeSpan time))
                        {
                            // 使用今天的日期 + 日志时间
                            date = DateTime.Today.Add(time);
                            return true;
                        }
                    }
                }
            }
            catch
            {
            }
            return false;
        }

        public async Task<int> KeepRecentLogsAsync(int count)
        {
            return await _logRepository.Value.KeepRecentLogsAsync(count);
        }

        public async Task ExportLogsToCsvAsync(string filePath, IEnumerable<int> ids = null)
        {
            await _logRepository.Value.ExportLogsToCsvAsync(filePath, ids);
        }

        public async Task AutoCleanupAsync()
        {
            if (!_config.AutoCleanup)
                return;

            if (_config.RetentionDays > 0)
            {
                await DeleteLogsOlderThanAsync(_config.RetentionDays);
            }

            if (_config.MaxLogCount > 0)
            {
                await KeepRecentLogsAsync(_config.MaxLogCount);
            }
        }

        public void RefreshConfig()
        {
            _config = LoadConfig();
        }

        public string GetLogDirectory()
        {
            return Path.GetDirectoryName(_config.LogFilePath);
        }

        public string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            int order = 0;
            double size = bytes;

            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size /= 1024;
            }

            return $"{size:0.##} {sizes[order]}";
        }
    }
}
