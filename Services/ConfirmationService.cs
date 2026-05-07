using System.Linq;
using System.Windows;
using GuiPiao.View;

namespace GuiPiao.Services;

/// <summary>
///     确认对话框服务
///     根据设置决定是否弹出确认对话框
/// </summary>
public class ConfirmationService
{
    private readonly GeneralSettingsService _settingsService;
    private Window? _ownerWindow;

    public ConfirmationService()
    {
        _settingsService = new GeneralSettingsService();
    }

    /// <summary>
    ///     设置对话框的父窗口
    /// </summary>
    public void SetOwnerWindow(Window owner)
    {
        _ownerWindow = owner;
    }

    /// <summary>
    ///     获取对话框的父窗口
    /// </summary>
    private Window? GetOwnerWindow()
    {
        return _ownerWindow ?? Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);
    }

    /// <summary>
    ///     确认删除操作
    /// </summary>
    /// <param name="itemName">要删除的项名称</param>
    /// <returns>true表示确认删除，false表示取消</returns>
    public bool ConfirmDelete(string itemName)
    {
        // 刷新配置，确保获取最新设置
        _settingsService.RefreshConfig();
        var config = _settingsService.Config;

        // 如果设置关闭确认，直接返回true
        if (!config.ConfirmOnDelete)
            return true;

        var result = MessageBoxWindow.Show(
            GetOwnerWindow(),
            $"确定要删除 {itemName} 吗？",
            "确认删除",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        return result == MessageBoxResult.Yes;
    }

    /// <summary>
    ///     确认批量删除/清空操作
    /// </summary>
    /// <param name="description">操作描述</param>
    /// <param name="isDangerous">是否为危险操作（显示警告图标）</param>
    /// <returns>true表示确认操作，false表示取消</returns>
    public bool ConfirmBatchDelete(string description, bool isDangerous = false)
    {
        // 刷新配置，确保获取最新设置
        _settingsService.RefreshConfig();
        var config = _settingsService.Config;

        // 如果设置关闭确认，直接返回true
        if (!config.ConfirmOnBatchDelete)
            return true;

        var icon = isDangerous ? MessageBoxImage.Warning : MessageBoxImage.Question;
        var result = MessageBoxWindow.Show(
            GetOwnerWindow(),
            description,
            "确认操作",
            MessageBoxButton.YesNo,
            icon);

        return result == MessageBoxResult.Yes;
    }

    /// <summary>
    ///     确认恢复数据库备份
    /// </summary>
    /// <param name="backupName">备份名称</param>
    /// <returns>true表示确认恢复，false表示取消</returns>
    public bool ConfirmRestore(string backupName)
    {
        // 刷新配置，确保获取最新设置
        _settingsService.RefreshConfig();
        var config = _settingsService.Config;

        // 如果设置关闭确认，直接返回true
        if (!config.ConfirmOnRestore)
            return true;

        var result = MessageBoxWindow.Show(
            GetOwnerWindow(),
            $"确定要恢复备份 {backupName} 吗？\n此操作将覆盖当前数据，请谨慎操作！",
            "确认恢复备份",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        return result == MessageBoxResult.Yes;
    }

    /// <summary>
    ///     通用确认对话框
    /// </summary>
    /// <param name="message">消息内容</param>
    /// <param name="title">标题</param>
    /// <param name="icon">图标</param>
    /// <returns>true表示确认，false表示取消</returns>
    public bool Confirm(string message, string title = "确认", MessageBoxImage icon = MessageBoxImage.Question)
    {
        var result = MessageBoxWindow.Show(GetOwnerWindow(), message, title, MessageBoxButton.YesNo, icon);
        return result == MessageBoxResult.Yes;
    }
}