namespace GuiPiao.Messages;

/// <summary>
///     视图类型
/// </summary>
public enum ViewType
{
    List,
    Card
}

/// <summary>
///     切换视图消息
/// </summary>
public class SwitchViewMessage
{
    public SwitchViewMessage(ViewType viewType)
    {
        ViewType = viewType;
    }

    /// <summary>
    ///     目标视图类型
    /// </summary>
    public ViewType ViewType { get; }
}