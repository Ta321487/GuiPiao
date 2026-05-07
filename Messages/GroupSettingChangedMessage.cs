namespace GuiPiao.Messages
{
    /// <summary>
    /// 分组设置变更消息
    /// </summary>
    public class GroupSettingChangedMessage
    {
        /// <summary>
        /// 分组选项
        /// </summary>
        public Model.GroupOption GroupOption { get; }

        public GroupSettingChangedMessage(Model.GroupOption groupOption)
        {
            GroupOption = groupOption;
        }
    }
}
