namespace GuiPiao.Model;

/// <summary>
///     票面预览窗口的打开场景：设置里的布局工作台 vs 主界面查看真实行程。
/// </summary>
public enum TicketPreviewSessionMode
{
    /// <summary>设置等入口：811 布局、JSON、示例/真实数据调版。</summary>
    LayoutWorkbench,

    /// <summary>主界面「查看」等：真实行程预览，车票字段只读，不显示布局工作台。</summary>
    UserTripPreview
}
