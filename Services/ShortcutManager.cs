using GuiPiao.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace GuiPiao.Services
{
    /// <summary>
    /// 快捷键管理器 - 管理应用程序的快捷键绑定
    /// </summary>
    public class ShortcutManager
    {
        private static readonly Lazy<ShortcutManager> _instance = new(() => new ShortcutManager());
        public static ShortcutManager Instance => _instance.Value;

        private readonly ShortcutSettingsService _settingsService;
        private readonly Dictionary<string, List<Action>> _actionHandlers = new();
        private Window? _mainWindow;

        private ShortcutManager()
        {
            _settingsService = new ShortcutSettingsService();
        }

        /// <summary>
        /// 初始化快捷键管理器
        /// </summary>
        public void Initialize(Window mainWindow)
        {
            _mainWindow = mainWindow;
            RegisterKeyBindings();
        }

        /// <summary>
        /// 注册快捷键绑定
        /// </summary>
        public void RegisterKeyBindings()
        {
            if (_mainWindow == null) return;

            // 清除现有绑定
            _mainWindow.InputBindings.Clear();

            var config = _settingsService.Config;

            foreach (var shortcut in config.Shortcuts)
            {
                if (string.IsNullOrEmpty(shortcut.CurrentKey)) continue;

                // 只注册全局快捷键到主窗口
                // 上下文相关的快捷键（如编辑操作）由各自的窗口处理
                if (IsGlobalShortcut(shortcut))
                {
                    var keyBinding = CreateKeyBinding(shortcut.ActionId, shortcut.CurrentKey);
                    if (keyBinding != null)
                    {
                        _mainWindow.InputBindings.Add(keyBinding);
                    }
                }
            }
        }

        /// <summary>
        /// 上下文相关的快捷键 ActionId 列表（这些快捷键不在主窗口注册）
        /// </summary>
        private static readonly HashSet<string> _contextualActionIds = new()
        {
            "Undo", "Redo"
        };

        /// <summary>
        /// 判断是否为全局快捷键
        /// </summary>
        private bool IsGlobalShortcut(ShortcutItem shortcut)
        {
            // Undo 和 Redo 是上下文相关的，不在主窗口注册
            return !_contextualActionIds.Contains(shortcut.ActionId);
        }

        /// <summary>
        /// 获取指定类别的快捷键配置
        /// </summary>
        public List<ShortcutItem> GetShortcutsByCategory(string category)
        {
            var config = _settingsService.Config;
            return config.Shortcuts.Where(s => s.Category == category).ToList();
        }

        /// <summary>
        /// 重新加载快捷键配置
        /// </summary>
        public void ReloadShortcuts()
        {
            _settingsService.RefreshConfig();
            RegisterKeyBindings();
        }

        /// <summary>
        /// 创建键绑定
        /// </summary>
        private KeyBinding? CreateKeyBinding(string actionId, string keyString)
        {
            try
            {
                var (modifier, key) = ParseKeyString(keyString);

                var command = new RelayCommand(_ => ExecuteAction(actionId));

                return new KeyBinding
                {
                    Key = key,
                    Modifiers = modifier,
                    Command = command
                };
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 解析快捷键字符串
        /// </summary>
        private (ModifierKeys Modifier, Key Key) ParseKeyString(string keyString)
        {
            var parts = keyString.Split('+', StringSplitOptions.RemoveEmptyEntries)
                                .Select(p => p.Trim())
                                .ToList();

            ModifierKeys modifier = ModifierKeys.None;
            Key key = Key.None;

            foreach (var part in parts)
            {
                switch (part.ToUpper())
                {
                    case "CTRL":
                        modifier |= ModifierKeys.Control;
                        break;
                    case "ALT":
                        modifier |= ModifierKeys.Alt;
                        break;
                    case "SHIFT":
                        modifier |= ModifierKeys.Shift;
                        break;
                    case "WIN":
                        modifier |= ModifierKeys.Windows;
                        break;
                    default:
                        // 尝试解析为Key
                        if (Enum.TryParse<Key>(part, true, out var parsedKey))
                        {
                            key = parsedKey;
                        }
                        else if (part.Equals("DEL", StringComparison.OrdinalIgnoreCase))
                        {
                            key = Key.Delete;
                        }
                        else if (part.Equals("PGUP", StringComparison.OrdinalIgnoreCase))
                        {
                            key = Key.PageUp;
                        }
                        else if (part.Equals("PGDN", StringComparison.OrdinalIgnoreCase))
                        {
                            key = Key.PageDown;
                        }
                        break;
                }
            }

            return (modifier, key);
        }

        /// <summary>
        /// 注册动作处理器
        /// </summary>
        public void RegisterAction(string actionId, Action handler)
        {
            if (!_actionHandlers.ContainsKey(actionId))
            {
                _actionHandlers[actionId] = new List<Action>();
            }
            _actionHandlers[actionId].Add(handler);
        }

        /// <summary>
        /// 注销动作处理器
        /// </summary>
        public void UnregisterAction(string actionId, Action handler)
        {
            if (_actionHandlers.ContainsKey(actionId))
            {
                _actionHandlers[actionId].Remove(handler);
            }
        }

        /// <summary>
        /// 执行动作
        /// </summary>
        private void ExecuteAction(string actionId)
        {
            if (_actionHandlers.TryGetValue(actionId, out var handlers))
            {
                foreach (var handler in handlers.ToList())
                {
                    handler?.Invoke();
                }
            }
        }

        /// <summary>
        /// 获取快捷键配置
        /// </summary>
        public ShortcutConfig GetConfig()
        {
            return _settingsService.Config;
        }
    }

    /// <summary>
    /// 简单的RelayCommand实现（用于快捷键）
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;
        private readonly Predicate<object?>? _canExecute;

        public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter)
        {
            return _canExecute?.Invoke(parameter) ?? true;
        }

        public void Execute(object? parameter)
        {
            _execute(parameter);
        }

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }
}
