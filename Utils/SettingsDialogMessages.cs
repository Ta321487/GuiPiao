namespace GuiPiao.Utils;

/// <summary>
///     各设置子页「保存 / 恢复默认」相关弹窗文案，保持用语一致。
/// </summary>
public static class SettingsDialogMessages
{
    public const string SuccessTitle = "成功";
    public const string ErrorTitle = "错误";
    public const string ConfirmTitle = "确认";

    public const string SaveFailedPrefix = "保存失败：";
    public const string RestoreFailedPrefix = "恢复默认设置失败：";

    public const string RestoreConfirmBody = "确定要恢复默认设置吗？";

    /// <summary>恢复后仅更新界面，需用户点击「保存设置」持久化。</summary>
    public const string RestoreNeedSaveHint = "已恢复默认设置，请点击「保存设置」保存更改。";

    /// <summary>恢复时已写入配置文件（或等价持久化）。</summary>
    public const string RestoreSavedHint = "已恢复默认设置并已保存。";
}
