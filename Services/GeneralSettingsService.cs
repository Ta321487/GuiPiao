using GuiPiao.Model;
using GuiPiao.Utils;
using System;

namespace GuiPiao.Services
{
    /// <summary>
    /// 常规设置服务
    /// </summary>
    public class GeneralSettingsService
    {
        private GeneralConfig _config;
        private const string ConfigFileName = "generalsettings.json";

        // 静态事件，当配置保存时触发
        public static event EventHandler<GeneralConfig>? ConfigSaved;

        public GeneralSettingsService()
        {
            _config = LoadConfig();
        }

        public GeneralConfig Config => _config;

        /// <summary>
        /// 从JSON文件加载配置，如果不存在则使用默认值
        /// </summary>
        private GeneralConfig LoadConfig()
        {
            var config = JsonConfigManager.Instance.LoadConfig(ConfigFileName, new GeneralConfig());
            return config;
        }

        /// <summary>
        /// 保存配置到JSON文件
        /// </summary>
        public void SaveConfig(GeneralConfig config)
        {
            _config = config;
            JsonConfigManager.Instance.SaveConfig(ConfigFileName, config);
            // 触发配置保存事件，通知所有监听者刷新配置
            ConfigSaved?.Invoke(this, config);
        }

        /// <summary>
        /// 获取默认配置
        /// </summary>
        public GeneralConfig GetDefaultConfig()
        {
            return new GeneralConfig();
        }

        /// <summary>
        /// 刷新配置（重新从文件加载）
        /// </summary>
        public void RefreshConfig()
        {
            _config = LoadConfig();
        }

        /// <summary>
        /// 保存上次关闭的页面
        /// </summary>
        public void SaveLastPage(LastPageOption lastPage)
        {
            // 使用 UpdateConfig 方法，确保在保存前重新加载最新配置
            // 避免覆盖其他设置项
            JsonConfigManager.Instance.UpdateConfig(ConfigFileName, new GeneralConfig(), config =>
            {
                config.LastPage = lastPage;
            });

            // 更新内存中的缓存
            _config.LastPage = lastPage;
        }

        /// <summary>
        /// 获取上次关闭的页面
        /// </summary>
        public LastPageOption GetLastPage()
        {
            // 从文件重新加载配置，确保获取最新的 LastPage 值
            var config = LoadConfig();
            return config.LastPage;
        }
    }
}
