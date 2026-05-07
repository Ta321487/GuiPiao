using GuiPiao.Model;
using GuiPiao.Utils;
using System.Collections.Generic;
using System.Linq;

namespace GuiPiao.Services
{
    /// <summary>
    /// 快捷键设置服务
    /// </summary>
    public class ShortcutSettingsService
    {
        private ShortcutConfig _config;
        private const string ConfigFileName = "shortcutsettings.json";

        public ShortcutSettingsService()
        {
            _config = LoadConfig();
        }

        public ShortcutConfig Config => _config;

        /// <summary>
        /// 从JSON文件加载配置，如果不存在则使用默认值
        /// </summary>
        private ShortcutConfig LoadConfig()
        {
            var config = JsonConfigManager.Instance.LoadConfig(ConfigFileName, GetDefaultConfig());
            return config;
        }

        /// <summary>
        /// 保存配置到JSON文件
        /// </summary>
        public void SaveConfig(ShortcutConfig config)
        {
            _config = config;
            JsonConfigManager.Instance.SaveConfig(ConfigFileName, config);
        }

        /// <summary>
        /// 获取默认配置
        /// </summary>
        public ShortcutConfig GetDefaultConfig()
        {
            return new ShortcutConfig
            {
                Shortcuts = new List<ShortcutItem>
                {
                    // 票务操作
                    new ShortcutItem { ActionId = "NewTicket", ActionName = "新增票务记录", Description = "添加新的火车票记录", Category = "票务操作", DefaultKey = "Ctrl+N", CurrentKey = "Ctrl+N" },
                    new ShortcutItem { ActionId = "OcrTicket", ActionName = "OCR识别车票", Description = "OCR识别车票信息", Category = "票务操作", DefaultKey = "Ctrl+O", CurrentKey = "Ctrl+O" },
                    new ShortcutItem { ActionId = "EditTicket", ActionName = "编辑选中票务", Description = "编辑选中的票务记录", Category = "票务操作", DefaultKey = "Ctrl+E", CurrentKey = "Ctrl+E" },
                    new ShortcutItem { ActionId = "DeleteTicket", ActionName = "删除选中票务", Description = "删除选中的票务记录", Category = "票务操作", DefaultKey = "Del", CurrentKey = "Del" },
                    new ShortcutItem { ActionId = "PreviewTicket", ActionName = "票面预览", Description = "预览车票票面", Category = "票务操作", DefaultKey = "Ctrl+P", CurrentKey = "Ctrl+P" },
                    new ShortcutItem { ActionId = "BatchUpdateStatus", ActionName = "批量修改状态", Description = "批量修改票务状态", Category = "票务操作", DefaultKey = "", CurrentKey = "" },
                    new ShortcutItem { ActionId = "BatchUpdateSeat", ActionName = "批量更新席别", Description = "批量更新座位类型", Category = "票务操作", DefaultKey = "", CurrentKey = "" },
                    new ShortcutItem { ActionId = "BatchDelete", ActionName = "批量删除", Description = "批量删除记录", Category = "票务操作", DefaultKey = "", CurrentKey = "" },
                    new ShortcutItem { ActionId = "NewTag", ActionName = "新建标签", Description = "创建新标签", Category = "票务操作", DefaultKey = "", CurrentKey = "" },
                    new ShortcutItem { ActionId = "ManageTags", ActionName = "管理标签", Description = "管理票务标签", Category = "票务操作", DefaultKey = "", CurrentKey = "" },

                    // 行程管理
                    new ShortcutItem { ActionId = "OpenMap", ActionName = "车票地图", Description = "打开车票地图", Category = "行程管理", DefaultKey = "Ctrl+M", CurrentKey = "Ctrl+M" },
                    new ShortcutItem { ActionId = "RefreshData", ActionName = "刷新数据", Description = "刷新行程列表数据", Category = "行程管理", DefaultKey = "F5", CurrentKey = "F5" },
                    new ShortcutItem { ActionId = "FilterThisYear", ActionName = "按时间筛选-本年度", Description = "筛选当年行程", Category = "行程管理", DefaultKey = "", CurrentKey = "" },
                    new ShortcutItem { ActionId = "FilterLastYear", ActionName = "按时间筛选-上年度", Description = "筛选去年行程", Category = "行程管理", DefaultKey = "", CurrentKey = "" },
                    new ShortcutItem { ActionId = "FilterUpcoming", ActionName = "未出行行程", Description = "显示未出行行程", Category = "行程管理", DefaultKey = "", CurrentKey = "" },

                    // 工具操作
                    new ShortcutItem { ActionId = "OpenLogManager", ActionName = "日志管理", Description = "打开日志管理器", Category = "工具操作", DefaultKey = "Ctrl+L", CurrentKey = "Ctrl+L" },
                    new ShortcutItem { ActionId = "DataVerify", ActionName = "数据校验", Description = "校验票务数据", Category = "工具操作", DefaultKey = "", CurrentKey = "" },
                    new ShortcutItem { ActionId = "DbCompact", ActionName = "数据库碎片整理", Description = "优化数据库", Category = "工具操作", DefaultKey = "", CurrentKey = "" },

                    // 系统设置
                    new ShortcutItem { ActionId = "OpenSettings", ActionName = "打开设置", Description = "打开系统设置", Category = "系统设置", DefaultKey = "Ctrl+,", CurrentKey = "Ctrl+," },

                    // 文件存储
                    new ShortcutItem { ActionId = "ImportData", ActionName = "数据导入", Description = "从Excel/CSV导入数据", Category = "文件存储", DefaultKey = "", CurrentKey = "" },
                    new ShortcutItem { ActionId = "BackupDatabase", ActionName = "备份数据库", Description = "备份数据库", Category = "文件存储", DefaultKey = "", CurrentKey = "" },
                    new ShortcutItem { ActionId = "ExitApp", ActionName = "退出程序", Description = "退出程序", Category = "文件存储", DefaultKey = "Alt+F4", CurrentKey = "Alt+F4" },

                    // 帮助操作
                    new ShortcutItem { ActionId = "HelpDoc", ActionName = "帮助文档", Description = "查看帮助文档", Category = "帮助操作", DefaultKey = "F1", CurrentKey = "F1" },
                    new ShortcutItem { ActionId = "CheckUpdate", ActionName = "检查更新", Description = "检查软件更新", Category = "帮助操作", DefaultKey = "", CurrentKey = "" },

                    // 视图导航
                    new ShortcutItem { ActionId = "PreviousPage", ActionName = "上一页", Description = "分页导航-上一页", Category = "视图导航", DefaultKey = "PageUp", CurrentKey = "PageUp" },
                    new ShortcutItem { ActionId = "NextPage", ActionName = "下一页", Description = "分页导航-下一页", Category = "视图导航", DefaultKey = "PageDown", CurrentKey = "PageDown" },
                    new ShortcutItem { ActionId = "GotoPage", ActionName = "跳转到页", Description = "跳转到指定页", Category = "视图导航", DefaultKey = "Ctrl+G", CurrentKey = "Ctrl+G" },

                    // 编辑操作
                    new ShortcutItem { ActionId = "Undo", ActionName = "撤销", Description = "撤销上一步操作", Category = "编辑操作", DefaultKey = "Ctrl+Z", CurrentKey = "Ctrl+Z" },
                    new ShortcutItem { ActionId = "Redo", ActionName = "重做", Description = "重做上一步操作", Category = "编辑操作", DefaultKey = "Ctrl+Y", CurrentKey = "Ctrl+Y" },
                }
            };
        }

        /// <summary>
        /// 刷新配置（重新从文件加载）
        /// </summary>
        public void RefreshConfig()
        {
            _config = LoadConfig();
        }

        /// <summary>
        /// 恢复默认配置
        /// </summary>
        public void RestoreDefaults()
        {
            _config = GetDefaultConfig();
            SaveConfig(_config);
        }

        /// <summary>
        /// 更新单个快捷键
        /// </summary>
        public void UpdateShortcut(string actionId, string newKey)
        {
            var shortcut = _config.Shortcuts.FirstOrDefault(s => s.ActionId == actionId);
            if (shortcut != null)
            {
                shortcut.CurrentKey = newKey;
                SaveConfig(_config);
            }
        }

        /// <summary>
        /// 恢复单个快捷键为默认
        /// </summary>
        public void RestoreDefault(string actionId)
        {
            var shortcut = _config.Shortcuts.FirstOrDefault(s => s.ActionId == actionId);
            if (shortcut != null)
            {
                shortcut.CurrentKey = shortcut.DefaultKey;
                SaveConfig(_config);
            }
        }

        /// <summary>
        /// 检查快捷键冲突
        /// </summary>
        public List<ShortcutItem> CheckConflicts(string key, string excludeActionId = null)
        {
            if (string.IsNullOrEmpty(key))
                return new List<ShortcutItem>();

            return _config.Shortcuts
                .Where(s => s.CurrentKey == key && s.ActionId != excludeActionId)
                .ToList();
        }
    }
}
