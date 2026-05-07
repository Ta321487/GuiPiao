using GuiPiao.Model;
using GuiPiao.Utils;

namespace GuiPiao.Services
{
    /// <summary>
    /// 导出设置服务
    /// </summary>
    public class ExportSettingsService
    {
        private ExportConfig _config;
        private const string ConfigFileName = "exportsettings.json";

        public ExportSettingsService()
        {
            _config = LoadConfig();
        }

        public ExportConfig Config => _config;

        /// <summary>
        /// 从JSON文件加载配置，如果不存在则使用默认值
        /// </summary>
        private ExportConfig LoadConfig()
        {
            var config = JsonConfigManager.Instance.LoadConfig(ConfigFileName, new ExportConfig());
            return config;
        }

        /// <summary>
        /// 保存配置到JSON文件
        /// </summary>
        public void SaveConfig(ExportConfig config)
        {
            _config = config;
            JsonConfigManager.Instance.SaveConfig(ConfigFileName, config);
        }

        /// <summary>
        /// 获取默认配置
        /// </summary>
        public ExportConfig GetDefaultConfig()
        {
            return new ExportConfig();
        }

        /// <summary>
        /// 刷新配置（重新从文件加载）
        /// </summary>
        public void RefreshConfig()
        {
            _config = LoadConfig();
        }
    }
}
