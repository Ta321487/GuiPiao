namespace GuiPiao.Messages;

/// <summary>
///     UI设置变更消息
/// </summary>
public class UISettingsChangedMessage
{
    public UISettingsChangedMessage(string scrollbarStyle, bool showActionButtonsOnHover, bool showViewButton,
        bool showEditButton, bool showRescheduleButton, bool showRefundButton, bool showDeleteButton,
        bool isTripListExpandedByDefault, bool showTimestamp, bool showModuleSource, string logRowHeight)
    {
        ScrollbarStyle = scrollbarStyle;
        ShowActionButtonsOnHover = showActionButtonsOnHover;
        ShowViewButton = showViewButton;
        ShowEditButton = showEditButton;
        ShowRescheduleButton = showRescheduleButton;
        ShowRefundButton = showRefundButton;
        ShowDeleteButton = showDeleteButton;
        IsTripListExpandedByDefault = isTripListExpandedByDefault;
        ShowTimestamp = showTimestamp;
        ShowModuleSource = showModuleSource;
        LogRowHeight = logRowHeight;
    }

    /// <summary>
    ///     滚动条样式
    /// </summary>
    public string ScrollbarStyle { get; }

    /// <summary>
    ///     是否悬停显示操作按钮
    /// </summary>
    public bool ShowActionButtonsOnHover { get; }

    /// <summary>
    ///     是否显示查看按钮
    /// </summary>
    public bool ShowViewButton { get; }

    /// <summary>
    ///     是否显示编辑按钮
    /// </summary>
    public bool ShowEditButton { get; }

    /// <summary>
    ///     是否显示改签按钮
    /// </summary>
    public bool ShowRescheduleButton { get; }

    /// <summary>
    ///     是否显示退票按钮
    /// </summary>
    public bool ShowRefundButton { get; }

    /// <summary>
    ///     是否显示删除按钮
    /// </summary>
    public bool ShowDeleteButton { get; }

    /// <summary>
    ///     默认展开行程列表
    /// </summary>
    public bool IsTripListExpandedByDefault { get; }

    /// <summary>
    ///     是否显示时间戳
    /// </summary>
    public bool ShowTimestamp { get; }

    /// <summary>
    ///     是否显示模块来源
    /// </summary>
    public bool ShowModuleSource { get; }

    /// <summary>
    ///     日志行高
    /// </summary>
    public string LogRowHeight { get; }
}