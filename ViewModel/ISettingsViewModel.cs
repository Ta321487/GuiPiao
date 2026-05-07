using System.Threading.Tasks;

namespace GuiPiao.ViewModel;

/// <summary>
///     设置视图模型接口
/// </summary>
public interface ISettingsViewModel
{
    /// <summary>
    ///     是否有未保存的更改
    /// </summary>
    bool HasUnsavedChanges { get; }

    /// <summary>
    ///     保存设置
    /// </summary>
    /// <param name="showMessage">是否显示保存成功提示</param>
    Task SaveSettingsAsync(bool showMessage = true);

    /// <summary>
    ///     重新加载设置（放弃更改）
    /// </summary>
    void ReloadSettings();
}