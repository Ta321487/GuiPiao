using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GuiPiao.Model;
using GuiPiao.Services;
using GuiPiao.View;

namespace GuiPiao.ViewModel;

/// <summary>
///     快捷键设置视图模型
/// </summary>
public partial class ShortcutSettingsViewModel : ObservableObject, ISettingsViewModel
{
    private readonly ShortcutSettingsService _settingsService;

    /// <summary>
    ///     冲突消息
    /// </summary>
    [ObservableProperty] private string _conflictMessage = string.Empty;

    /// <summary>
    ///     当前正在编辑的快捷键
    /// </summary>
    [ObservableProperty] private ShortcutItemViewModel? _editingShortcut;

    /// <summary>
    ///     是否有冲突
    /// </summary>
    [ObservableProperty] private bool _hasConflict;

    /// <summary>
    ///     是否正在等待快捷键输入
    /// </summary>
    [ObservableProperty] private bool _isWaitingForKey;

    private ShortcutConfig _originalConfig;

    /// <summary>
    ///     搜索文本
    /// </summary>
    [ObservableProperty] private string _searchText = string.Empty;

    /// <summary>
    ///     快捷键列表
    /// </summary>
    [ObservableProperty] private ObservableCollection<ShortcutItemViewModel> _shortcuts = new();

    public ShortcutSettingsViewModel()
    {
        _settingsService = new ShortcutSettingsService();
        LoadSettings();
    }

    /// <summary>
    ///     是否有未保存的更改
    /// </summary>
    public bool HasUnsavedChanges
    {
        get
        {
            if (_originalConfig == null)
                return false;

            var currentConfig = GetCurrentConfig();
            return !ConfigsEqual(_originalConfig, currentConfig);
        }
    }

    /// <summary>
    ///     重新加载设置（放弃更改）
    /// </summary>
    public void ReloadSettings()
    {
        _settingsService.RefreshConfig();
        LoadSettings();
        OnPropertyChanged(nameof(HasUnsavedChanges));
    }

    /// <summary>
    ///     保存设置（接口实现）
    /// </summary>
    public async Task SaveSettingsAsync(bool showMessage = true)
    {
        SaveSettingsInternal(showMessage);
        await Task.CompletedTask;
    }

    /// <summary>
    ///     获取当前配置
    /// </summary>
    private ShortcutConfig GetCurrentConfig()
    {
        return new ShortcutConfig
        {
            Shortcuts = Shortcuts.Select(s => new ShortcutItem
            {
                ActionId = s.ActionId,
                ActionName = s.ActionName,
                Description = s.Description,
                Category = s.Category,
                DefaultKey = s.DefaultKey,
                CurrentKey = s.CurrentKey
            }).ToList()
        };
    }

    /// <summary>
    ///     比较两个配置是否相等
    /// </summary>
    private bool ConfigsEqual(ShortcutConfig a, ShortcutConfig b)
    {
        if (a.Shortcuts.Count != b.Shortcuts.Count)
            return false;

        var aDict = a.Shortcuts.ToDictionary(s => s.ActionId);
        var bDict = b.Shortcuts.ToDictionary(s => s.ActionId);

        foreach (var aItem in a.Shortcuts)
        {
            if (!bDict.TryGetValue(aItem.ActionId, out var bItem))
                return false;

            if (aItem.CurrentKey != bItem.CurrentKey)
                return false;
        }

        return true;
    }

    /// <summary>
    ///     加载设置
    /// </summary>
    private void LoadSettings()
    {
        _originalConfig = _settingsService.Config;
        var shortcuts = _originalConfig.Shortcuts.Select(s => new ShortcutItemViewModel(s)).ToList();
        Shortcuts = new ObservableCollection<ShortcutItemViewModel>(shortcuts);
        ClearConflict();
    }

    /// <summary>
    ///     搜索命令
    /// </summary>
    [RelayCommand]
    private void Search()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            // 显示所有
            var shortcuts = _originalConfig.Shortcuts.Select(s => new ShortcutItemViewModel(s)).ToList();
            Shortcuts = new ObservableCollection<ShortcutItemViewModel>(shortcuts);
        }
        else
        {
            // 过滤
            var searchLower = SearchText.ToLower();
            var filtered = _originalConfig.Shortcuts
                .Where(s => s.ActionName.ToLower().Contains(searchLower) ||
                            s.Category.ToLower().Contains(searchLower) ||
                            s.CurrentKey.ToLower().Contains(searchLower))
                .Select(s => new ShortcutItemViewModel(s))
                .ToList();
            Shortcuts = new ObservableCollection<ShortcutItemViewModel>(filtered);
        }
    }

    /// <summary>
    ///     开始编辑快捷键
    /// </summary>
    [RelayCommand]
    private void StartEdit(ShortcutItemViewModel shortcut)
    {
        // 清除之前的编辑状态
        if (EditingShortcut != null) EditingShortcut.IsEditing = false;

        EditingShortcut = shortcut;
        EditingShortcut.IsEditing = true;
        IsWaitingForKey = true;
        ClearConflict();
    }

    /// <summary>
    ///     取消编辑
    /// </summary>
    [RelayCommand]
    private void CancelEdit()
    {
        if (EditingShortcut != null)
        {
            EditingShortcut.IsEditing = false;
            EditingShortcut = null;
        }

        IsWaitingForKey = false;
        ClearConflict();
    }

    /// <summary>
    ///     处理按键输入
    /// </summary>
    public void HandleKeyInput(Key key, ModifierKeys modifiers)
    {
        if (!IsWaitingForKey || EditingShortcut == null)
            return;

        // 忽略单独的修饰键
        if (key == Key.LeftCtrl || key == Key.RightCtrl ||
            key == Key.LeftAlt || key == Key.RightAlt ||
            key == Key.LeftShift || key == Key.RightShift ||
            key == Key.LWin || key == Key.RWin)
            return;

        // 构建快捷键字符串
        var keyString = BuildKeyString(key, modifiers);
        if (string.IsNullOrEmpty(keyString))
            return;

        // 检查冲突
        var conflicts = CheckConflicts(keyString, EditingShortcut.ActionId);
        if (conflicts.Any())
        {
            ShowConflict(conflicts);
            return;
        }

        // 设置快捷键
        EditingShortcut.CurrentKey = keyString;
        EditingShortcut.IsEditing = false;
        EditingShortcut = null;
        IsWaitingForKey = false;
        ClearConflict();
        OnPropertyChanged(nameof(HasUnsavedChanges));
    }

    /// <summary>
    ///     构建快捷键字符串
    /// </summary>
    private string BuildKeyString(Key key, ModifierKeys modifiers)
    {
        var parts = new List<string>();

        if (modifiers.HasFlag(ModifierKeys.Control))
            parts.Add("Ctrl");
        if (modifiers.HasFlag(ModifierKeys.Alt))
            parts.Add("Alt");
        if (modifiers.HasFlag(ModifierKeys.Shift))
            parts.Add("Shift");

        // 转换键名为标准格式
        var keyName = key.ToString();
        switch (key)
        {
            case Key.Delete:
                keyName = "Del";
                break;
            case Key.PageUp:
                keyName = "PageUp";
                break;
            case Key.PageDown:
                keyName = "PageDown";
                break;
            case Key.Left:
            case Key.Right:
            case Key.Up:
            case Key.Down:
                // 方向键需要修饰键
                if (parts.Count == 0)
                    return string.Empty;
                break;
        }

        parts.Add(keyName);
        return string.Join("+", parts);
    }

    /// <summary>
    ///     检查快捷键冲突
    /// </summary>
    private List<ShortcutItemViewModel> CheckConflicts(string key, string excludeActionId)
    {
        return Shortcuts
            .Where(s => s.CurrentKey == key && s.ActionId != excludeActionId)
            .ToList();
    }

    /// <summary>
    ///     显示冲突信息
    /// </summary>
    private void ShowConflict(List<ShortcutItemViewModel> conflicts)
    {
        HasConflict = true;
        var conflictNames = string.Join("、", conflicts.Select(c => $"\"{c.ActionName}\""));
        ConflictMessage = $"该快捷键已被 {conflictNames} 使用，请选择其他快捷键。";
    }

    /// <summary>
    ///     清除冲突信息
    /// </summary>
    private void ClearConflict()
    {
        HasConflict = false;
        ConflictMessage = string.Empty;
    }

    /// <summary>
    ///     恢复默认快捷键
    /// </summary>
    [RelayCommand]
    private void RestoreDefault(ShortcutItemViewModel shortcut)
    {
        if (shortcut != null)
        {
            shortcut.CurrentKey = shortcut.DefaultKey;
            OnPropertyChanged(nameof(HasUnsavedChanges));
        }
    }

    /// <summary>
    ///     保存设置命令
    /// </summary>
    [RelayCommand]
    private void SaveSettings()
    {
        SaveSettingsInternal(true);
    }

    /// <summary>
    ///     保存设置内部实现
    /// </summary>
    public void SaveSettings(bool showMessage = true)
    {
        SaveSettingsInternal(showMessage);
    }

    /// <summary>
    ///     保存设置内部实现
    /// </summary>
    private void SaveSettingsInternal(bool showMessage)
    {
        // 获取设置窗口作为父窗口
        var settingsWindow = Application.Current.Windows
            .OfType<Window>()
            .FirstOrDefault(w => w.DataContext is SettingsViewModel);

        try
        {
            // 获取默认配置以保留正确的DefaultKey
            var defaultConfig = _settingsService.GetDefaultConfig();
            var defaultShortcuts = defaultConfig.Shortcuts.ToDictionary(s => s.ActionId);

            var config = new ShortcutConfig
            {
                Shortcuts = Shortcuts.Select(s => new ShortcutItem
                {
                    ActionId = s.ActionId,
                    ActionName = s.ActionName,
                    Description = s.Description,
                    Category = s.Category,
                    DefaultKey = defaultShortcuts.TryGetValue(s.ActionId, out var defaultItem)
                        ? defaultItem.DefaultKey
                        : s.DefaultKey,
                    CurrentKey = s.CurrentKey
                }).ToList()
            };

            _settingsService.SaveConfig(config);
            _originalConfig = config;

            OnPropertyChanged(nameof(HasUnsavedChanges));

            // 重新加载快捷键到应用程序
            ShortcutManager.Instance.ReloadShortcuts();

            if (showMessage) MessageBoxWindow.Show(settingsWindow, "快捷键设置已保存", "成功");
        }
        catch (Exception ex)
        {
            MessageBoxWindow.Show(settingsWindow, $"保存失败：{ex.Message}", "错误", MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    /// <summary>
    ///     恢复所有默认设置
    /// </summary>
    [RelayCommand]
    private void RestoreAllDefaults()
    {
        // 获取设置窗口作为父窗口
        var settingsWindow = Application.Current.Windows
            .OfType<Window>()
            .FirstOrDefault(w => w.DataContext is SettingsViewModel);

        var result = MessageBoxWindow.Show(settingsWindow, "确定要恢复所有快捷键的默认设置吗？", "确认", MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        if (result != MessageBoxResult.Yes) return;

        try
        {
            _settingsService.RestoreDefaults();
            LoadSettings();
            OnPropertyChanged(nameof(HasUnsavedChanges));

            // 重新加载快捷键到应用程序
            ShortcutManager.Instance.ReloadShortcuts();

            MessageBoxWindow.Show(settingsWindow, "已恢复默认快捷键设置", "成功");
        }
        catch (Exception ex)
        {
            MessageBoxWindow.Show(settingsWindow, $"恢复默认设置失败：{ex.Message}", "错误", MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
}

/// <summary>
///     快捷键项视图模型
/// </summary>
public partial class ShortcutItemViewModel : ObservableObject
{
    private readonly ShortcutItem _item;

    /// <summary>
    ///     是否正在编辑
    /// </summary>
    [ObservableProperty] private bool _isEditing;

    public ShortcutItemViewModel(ShortcutItem item)
    {
        _item = item;
    }

    public string ActionId => _item.ActionId;
    public string ActionName => _item.ActionName;
    public string Description => _item.Description;
    public string Category => _item.Category;
    public string DefaultKey => _item.DefaultKey;

    /// <summary>
    ///     当前快捷键
    /// </summary>
    public string CurrentKey
    {
        get => _item.CurrentKey;
        set
        {
            if (_item.CurrentKey != value)
            {
                _item.CurrentKey = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DisplayKey));
                OnPropertyChanged(nameof(IsModified));
                OnPropertyChanged(nameof(IsSet));
            }
        }
    }

    /// <summary>
    ///     显示的快捷键文本
    /// </summary>
    public string DisplayKey => string.IsNullOrEmpty(CurrentKey) ? "[点击设置]" : CurrentKey;

    /// <summary>
    ///     是否已修改
    /// </summary>
    public bool IsModified => CurrentKey != DefaultKey;

    /// <summary>
    ///     是否已设置快捷键
    /// </summary>
    public bool IsSet => !string.IsNullOrEmpty(CurrentKey);
}