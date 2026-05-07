using GuiPiao.Model;
using GuiPiao.Utils;
using System.Collections.Generic;
using System.Linq;

namespace GuiPiao.Services
{
    /// <summary>
    /// 界面设置服务
    /// </summary>
    public class UISettingsService
    {
        private UISettingsConfig _config;
        private const string ConfigFileName = "uisettings.json";

        public UISettingsService()
        {
            _config = LoadConfig();
        }

        public UISettingsConfig Config => _config;

        /// <summary>
        /// 从JSON文件加载配置，如果不存在则使用默认值
        /// 自动合并新增的列配置
        /// </summary>
        private UISettingsConfig LoadConfig()
        {
            var config = JsonConfigManager.Instance.LoadConfig(ConfigFileName, new UISettingsConfig());
            
            // 自动合并新增的列配置
            MergeDataGridColumns(config);
            
            return config;
        }

        /// <summary>
        /// 合并DataGrid列配置，将新增的列追加到现有配置中
        /// </summary>
        private void MergeDataGridColumns(UISettingsConfig config)
        {
            if (config.DataGridColumns == null || config.DataGridColumns.Count == 0)
            {
                config.DataGridColumns = DataGridColumnConfig.GetDefaultColumns();
                return;
            }

            var defaultColumns = DataGridColumnConfig.GetDefaultColumns();
            var existingFieldNames = config.DataGridColumns.Select(c => c.FieldName).ToHashSet();
            bool hasNewColumns = false;

            // 找出新增的列（在默认配置中存在但在当前配置中不存在的列）
            foreach (var defaultColumn in defaultColumns)
            {
                if (!existingFieldNames.Contains(defaultColumn.FieldName))
                {
                    // 新增列，追加到列表中
                    config.DataGridColumns.Add(defaultColumn);
                    hasNewColumns = true;
                }
            }

            // 如果有新增列，重新排序并保存
            if (hasNewColumns)
            {
                // 按 DisplayOrder 排序
                config.DataGridColumns = config.DataGridColumns
                    .OrderBy(c => c.DisplayOrder)
                    .ToList();
                
                // 保存更新后的配置
                SaveConfig(config);
            }
        }

        /// <summary>
        /// 保存配置到JSON文件
        /// </summary>
        public void SaveConfig(UISettingsConfig config)
        {
            _config = config;
            JsonConfigManager.Instance.SaveConfig(ConfigFileName, config);
        }

        /// <summary>
        /// 获取默认配置
        /// </summary>
        public UISettingsConfig GetDefaultConfig()
        {
            return new UISettingsConfig();
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
