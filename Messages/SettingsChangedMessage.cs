namespace GuiPiao.Messages;

/// <summary>
///     设置变更消息
/// </summary>
public class SettingsChangedMessage
{
    public SettingsChangedMessage(string settingType)
    {
        SettingType = settingType;
    }

    public string SettingType { get; }
}