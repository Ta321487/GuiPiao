namespace GuiPiao.Model;

/// <summary>
///     票面窗口打开场景：设置中的版式编辑，或主界面查看行程票面。
/// </summary>
public enum TicketPreviewSessionMode
{
    /// <summary>设置入口：编辑 811 版式、字体与示例数据。</summary>
    LayoutWorkbench,

    /// <summary>主界面：查看真实行程票面；字段只读，不显示版式编辑。</summary>
    UserTripPreview
}
