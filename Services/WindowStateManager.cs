using GuiPiao.Model;
using GuiPiao.Utils;
using System;
using System.Collections.Generic;
using System.Windows;

namespace GuiPiao.Services
{
    /// <summary>
    /// 窗口状态管理器 - 统一管理需要跟踪状态的窗口
    /// </summary>
    public class WindowStateManager
    {
        private static WindowStateManager? _instance;
        public static WindowStateManager Instance => _instance ??= new WindowStateManager();

        // 存储窗口类型和对应的打开状态
        private readonly Dictionary<LastPageOption, bool> _windowStates = new();

        private WindowStateManager()
        {
            // 初始化所有状态为 false
            foreach (LastPageOption option in Enum.GetValues(typeof(LastPageOption)))
            {
                _windowStates[option] = false;
            }
        }

        /// <summary>
        /// 注册窗口（在打开窗口时调用）
        /// </summary>
        public void RegisterWindow(LastPageOption windowType, Window window)
        {
            _windowStates[windowType] = true;

            // 窗口关闭时自动更新状态
            window.Closed += (s, e) =>
            {
                _windowStates[windowType] = false;
            };
        }

        /// <summary>
        /// 获取当前打开的窗口类型（按优先级）
        /// </summary>
        public LastPageOption GetCurrentWindowType()
        {
            // 按优先级检查：LogManager > Map > MainList
            if (_windowStates.TryGetValue(LastPageOption.LogManager, out bool isLogOpen) && isLogOpen)
                return LastPageOption.LogManager;

            if (_windowStates.TryGetValue(LastPageOption.Map, out bool isMapOpen) && isMapOpen)
                return LastPageOption.Map;

            return LastPageOption.MainList;
        }

        /// <summary>
        /// 保存当前窗口状态到配置
        /// </summary>
        public void SaveCurrentWindowState()
        {
            var currentWindow = GetCurrentWindowType();

            JsonConfigManager.Instance.UpdateConfig("generalsettings.json", new GeneralConfig(), config =>
            {
                config.LastPage = currentWindow;
            });
        }
    }
}
