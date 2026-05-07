using GuiPiao.Model;
using GuiPiao.Services;
using System;
using System.Collections.Generic;
using System.IO;
namespace GuiPiao.Utils
{
    public class ConfigManager
    {
        // 单例实例
        private static readonly Lazy<ConfigManager> _instance = new Lazy<ConfigManager>(() => new ConfigManager());

        // 配置属性
        public string DatabaseConnectionString { get; private set; }
        public string TesseractDataPath { get; private set; }
        public string LogFilePath { get; private set; }
        public int DefaultPageSize { get; private set; }

        // UI设置服务
        private UISettingsService _uiSettingsService;
        public UISettingsService UISettingsService
        {
            get
            {
                if (_uiSettingsService == null)
                {
                    _uiSettingsService = new UISettingsService();
                }
                return _uiSettingsService;
            }
        }

        // 私有构造函数
        private ConfigManager()
        {
            InitializeConfig();
        }

        // 公共实例
        public static ConfigManager Instance => _instance.Value;

        // 初始化配置
        private void InitializeConfig()
        {
            // 数据库连接字符串
            string dbPath = GetDatabasePath();
            DatabaseConnectionString = $"Data Source={dbPath}";

            // Tesseract数据路径
            TesseractDataPath = Path.Combine(Directory.GetCurrentDirectory(), "tessdata");

            // 日志文件路径
            string logDir = Path.Combine(Directory.GetCurrentDirectory(), "logs");
            if (!Directory.Exists(logDir))
            {
                Directory.CreateDirectory(logDir);
            }
            LogFilePath = Path.Combine(logDir, $"app_{DateTime.Now:yyyyMMdd}.log");

            // 默认分页大小
            DefaultPageSize = 20;
        }

        /// <summary>
        /// 获取数据库路径（优先从配置文件读取，然后查找多个可能的位置）
        /// </summary>
        private string GetDatabasePath()
        {
            try
            {
                // 尝试从配置文件读取
                var config = JsonConfigManager.Instance.LoadConfig<DatabaseConfig>("databasesettings.json", new DatabaseConfig());
                if (config.UseCustomPath && !string.IsNullOrEmpty(config.DatabasePath))
                {
                    return config.DatabasePath;
                }
            }
            catch
            {
                // 配置文件读取失败，使用默认路径
            }

            // 获取程序集所在目录
            var exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            var exeDir = Path.GetDirectoryName(exePath)!;

            // 搜索路径列表（按优先级）
            var searchPaths = new List<string>
            {
                // 1. 当前工作目录
                Directory.GetCurrentDirectory(),
                // 2. 程序集所在目录
                exeDir,
                // 3. 项目目录（开发环境）
                Path.GetFullPath(Path.Combine(exeDir, "..", "..", "..")),
                // 4. 程序集父目录
                Path.GetFullPath(Path.Combine(exeDir, "..")),
            };

            // 查找已存在的数据库文件
            foreach (var path in searchPaths)
            {
                var dbPath = Path.Combine(path, "guipiao.db");
                if (File.Exists(dbPath))
                {
                    return dbPath;
                }
            }

            // 如果没有找到，使用程序集所在目录作为默认位置
            return Path.Combine(exeDir, "guipiao.db");
        }

        // 重新加载配置
        public void ReloadConfig()
        {
            InitializeConfig();
        }

        /// <summary>
        /// 刷新UI设置配置（从文件重新加载）
        /// </summary>
        public void RefreshUISettingsConfig()
        {
            _uiSettingsService?.RefreshConfig();
        }
    }
}
