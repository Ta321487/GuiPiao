using GuiPiao.Model;

namespace GuiPiao.Messages;

/// <summary>
///     分组设置变更消息
/// </summary>
public class GroupSettingChangedMessage
{
    public GroupSettingChangedMessage(GroupOption groupOption)
    {
        GroupOption = groupOption;
    }

    /// <summary>
    ///     分组选项
    /// </summary>
    public GroupOption GroupOption { get; }
}