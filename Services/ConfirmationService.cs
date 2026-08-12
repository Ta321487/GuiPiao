using System.Linq;
using System.Windows;
using GuiPiao.View;

namespace GuiPiao.Services;

/// <summary>
///     确认对话框服务：统一读取常规设置中的确认开关。
/// </summary>
public class ConfirmationService
{
    private readonly GeneralSettingsService _settingsService;
    private Window? _ownerWindow;

    public ConfirmationService()
    {
        _settingsService = new GeneralSettingsService();
    }

    public void SetOwnerWindow(Window owner) => _ownerWindow = owner;

    private Window? GetOwnerWindow() =>
        _ownerWindow ?? Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);

    /// <summary>单条删除确认（车票、车站等）。受 <c>ConfirmOnDelete</c> 控制。</summary>
    public bool ConfirmDelete(string itemName)
    {
        _settingsService.RefreshConfig();
        if (!_settingsService.Config.ConfirmOnDelete)
            return true;

        var result = MessageBoxWindow.Show(
            GetOwnerWindow(),
            $"确定要删除 {itemName} 吗？",
            "确认删除",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        return result == MessageBoxResult.Yes;
    }

    /// <summary>批量删除/清空类确认。受 <c>ConfirmOnBatchDelete</c> 控制。</summary>
    public bool ConfirmBatchDelete(string description, bool isDangerous = false)
    {
        _settingsService.RefreshConfig();
        if (!_settingsService.Config.ConfirmOnBatchDelete)
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
    ///     高危批量操作（清空/重置）：开启确认时两次 Yes/No；关闭则直接通过。
    ///     受 <c>ConfirmOnBatchDelete</c> 控制。
    /// </summary>
    public bool ConfirmDangerousBatch(string firstMessage, string secondMessage,
        string firstTitle = "危险操作确认", string secondTitle = "最终确认")
    {
        _settingsService.RefreshConfig();
        if (!_settingsService.Config.ConfirmOnBatchDelete)
            return true;

        var first = MessageBoxWindow.Show(
            GetOwnerWindow(),
            firstMessage,
            firstTitle,
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);
        if (first != MessageBoxResult.Yes)
            return false;

        var second = MessageBoxWindow.Show(
            GetOwnerWindow(),
            secondMessage,
            secondTitle,
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);
        return second == MessageBoxResult.Yes;
    }

    /// <summary>恢复备份确认。受 <c>ConfirmOnRestore</c> 控制。</summary>
    public bool ConfirmRestore(string message)
    {
        _settingsService.RefreshConfig();
        if (!_settingsService.Config.ConfirmOnRestore)
            return true;

        var result = MessageBoxWindow.Show(
            GetOwnerWindow(),
            message,
            "确认恢复",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        return result == MessageBoxResult.Yes;
    }
}
