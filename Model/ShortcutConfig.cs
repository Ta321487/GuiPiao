using System.Collections.Generic;

namespace GuiPiao.Model;

/// <summary>
///     快捷键配置
/// </summary>
public class ShortcutConfig
{
    /// <summary>
    ///     快捷键列表
    /// </summary>
    public List<ShortcutItem> Shortcuts { get; set; } = new();
}

/// <summary>
///     快捷键项
/// </summary>
public class ShortcutItem
{
    /// <summary>
    ///     操作ID
    /// </summary>
    public string ActionId { get; set; } = string.Empty;

    /// <summary>
    ///     操作名称
    /// </summary>
    public string ActionName { get; set; } = string.Empty;

    /// <summary>
    ///     操作描述
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    ///     分类
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    ///     默认快捷键
    /// </summary>
    public string DefaultKey { get; set; } = string.Empty;

    /// <summary>
    ///     当前快捷键
    /// </summary>
    public string CurrentKey { get; set; } = string.Empty;

    /// <summary>
    ///     是否已修改
    /// </summary>
    public bool IsModified => CurrentKey != DefaultKey;

    /// <summary>
    ///     是否已设置快捷键
    /// </summary>
    public bool IsSet => !string.IsNullOrEmpty(CurrentKey);
}