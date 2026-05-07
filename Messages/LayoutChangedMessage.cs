namespace GuiPiao.Messages
{
    /// <summary>
    /// 布局变更消息 - 用于实时通知主界面更新布局
    /// </summary>
    public class LayoutChangedMessage
    {
        public int LeftPanelWidth { get; }
        public bool LeftPanelLocked { get; }
        public int RightPanelWidth { get; }
        public bool RightPanelLocked { get; }
        public int BottomPanelHeight { get; }
        public bool BottomPanelLocked { get; }

        public LayoutChangedMessage(
            int leftPanelWidth,
            bool leftPanelLocked,
            int rightPanelWidth,
            bool rightPanelLocked,
            int bottomPanelHeight,
            bool bottomPanelLocked)
        {
            LeftPanelWidth = leftPanelWidth;
            LeftPanelLocked = leftPanelLocked;
            RightPanelWidth = rightPanelWidth;
            RightPanelLocked = rightPanelLocked;
            BottomPanelHeight = bottomPanelHeight;
            BottomPanelLocked = bottomPanelLocked;
        }
    }
}
