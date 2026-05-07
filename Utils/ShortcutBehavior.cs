using GuiPiao.Model;
using GuiPiao.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace GuiPiao.Utils
{
    /// <summary>
    /// 快捷键行为辅助类 - 用于为窗口动态绑定上下文相关的快捷键
    /// </summary>
    public static class ShortcutBehavior
    {
        /// <summary>
        /// 为窗口注册编辑操作相关的快捷键（撤销/重做）
        /// </summary>
        /// <param name="window">目标窗口</param>
        /// <param name="commandResolver">命令解析器，根据 actionId 返回对应的 ICommand</param>
        public static void RegisterEditShortcuts(Window window, Func<string, ICommand?> commandResolver)
        {
            var shortcutManager = ShortcutManager.Instance;
            var config = shortcutManager.GetConfig();

            // 查找 Undo 和 Redo 快捷键（不依赖 Category，直接通过 ActionId 查找）
            var editActionIds = new[] { "Undo", "Redo" };
            var shortcuts = new List<(string ActionId, string KeyString, ICommand Command)>();

            foreach (var actionId in editActionIds)
            {
                var shortcut = config.Shortcuts.FirstOrDefault(s => s.ActionId == actionId);
                if (shortcut == null || string.IsNullOrEmpty(shortcut.CurrentKey)) continue;

                var command = commandResolver(actionId);
                if (command == null) continue;

                shortcuts.Add((actionId, shortcut.CurrentKey, command));
            }

            // 使用 PreviewKeyDown 事件处理快捷键，确保即使焦点在文本框中也能触发
            window.PreviewKeyDown += (sender, e) =>
            {
                foreach (var (actionId, keyString, command) in shortcuts)
                {
                    if (IsKeyMatch(e, keyString) && command.CanExecute(null))
                    {
                        command.Execute(null);
                        e.Handled = true;
                        break;
                    }
                }
            };
        }

        /// <summary>
        /// 检查按键是否匹配快捷键字符串
        /// </summary>
        private static bool IsKeyMatch(KeyEventArgs e, string keyString)
        {
            var (expectedModifier, expectedKey) = ParseKeyString(keyString);

            // 检查修饰键 - 使用 Keyboard.Modifiers 获取当前修饰键状态
            var currentModifier = Keyboard.Modifiers;

            if (currentModifier != expectedModifier) return false;

            // 检查按键
            // 对于带修饰键的快捷键，需要检查 Key 而不是 OriginalKey
            // 因为 Ctrl+Z 这样的组合键，Key 会是 Z，而 OriginalKey 可能是别的
            var actualKey = e.Key;

            // 处理数字键的情况（D0-D9）
            if (expectedKey >= Key.D0 && expectedKey <= Key.D9)
            {
                return actualKey == expectedKey;
            }

            // 处理字母键的情况
            if (expectedKey >= Key.A && expectedKey <= Key.Z)
            {
                return actualKey == expectedKey;
            }

            return actualKey == expectedKey;
        }

        /// <summary>
        /// 解析快捷键字符串
        /// </summary>
        private static (ModifierKeys Modifier, Key Key) ParseKeyString(string keyString)
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
    }
}
