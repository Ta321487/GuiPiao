using System.Collections.Generic;
using System.Text.Json.Serialization;
using GuiPiao.Messages;

namespace GuiPiao.Model;

/// <summary>
///     界面设置配置类
/// </summary>
public class UISettingsConfig
{
    #region 高级界面选项

    /// <summary>
    ///     DPI缩放适配
    /// </summary>
    public string DpiScaling { get; set; } = "System";

    #endregion

    #region 票面预览显示设置

    /// <summary>
    ///     默认缩放
    /// </summary>
    public string DefaultZoom { get; set; } = "FitWindow";

    /// <summary>
    ///     允许鼠标滚轮缩放
    /// </summary>
    public bool AllowMouseWheelZoom { get; set; } = true;

    /// <summary>
    ///     显示亮度 (50-150)
    /// </summary>
    public int DisplayBrightness { get; set; } = 100;

    /// <summary>
    ///     票面居中显示
    /// </summary>
    public bool TicketCentered { get; set; } = true;

    #endregion

    #region 主界面布局配置

    /// <summary>
    ///     左侧检索区宽度 (120-300)
    /// </summary>
    public int LeftPanelWidth { get; set; } = 180;

    /// <summary>
    ///     左侧检索区宽度锁定
    /// </summary>
    public bool LeftPanelLocked { get; set; } = true;

    /// <summary>
    ///     右侧日志面板宽度 (180-350)
    /// </summary>
    public int RightPanelWidth { get; set; } = 220;

    /// <summary>
    ///     右侧日志面板宽度锁定
    /// </summary>
    public bool RightPanelLocked { get; set; } = true;

    /// <summary>
    ///     底部行程列表高度 (150-400)
    /// </summary>
    public int BottomPanelHeight { get; set; } = 250;

    /// <summary>
    ///     底部行程列表高度锁定
    /// </summary>
    public bool BottomPanelLocked { get; set; } = true;

    #endregion

    #region 行程列表显示设置

    /// <summary>
    ///     默认分组
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public GroupOption DefaultGroup { get; set; } = GroupOption.None;

    /// <summary>
    ///     记住字段顺序（列的显示顺序）
    /// </summary>
    public bool RememberColumnOrder { get; set; } = true;

    /// <summary>
    ///     记住数据排序（升序/降序）
    /// </summary>
    public bool RememberDataSort { get; set; } = true;

    /// <summary>
    ///     上次数据排序的列名
    /// </summary>
    public string? LastSortColumn { get; set; }

    /// <summary>
    ///     上次数据排序方向（Ascending/Descending）
    /// </summary>
    public string? LastSortDirection { get; set; }

    /// <summary>
    ///     滚动条样式
    /// </summary>
    public string ScrollbarStyle { get; set; } = "Normal";

    /// <summary>
    ///     鼠标悬停时显示操作按钮
    /// </summary>
    public bool ShowActionButtonsOnHover { get; set; } = true;

    /// <summary>
    ///     显示查看按钮
    /// </summary>
    public bool ShowViewButton { get; set; } = true;

    /// <summary>
    ///     显示编辑按钮
    /// </summary>
    public bool ShowEditButton { get; set; } = true;

    /// <summary>
    ///     显示改签按钮
    /// </summary>
    public bool ShowRescheduleButton { get; set; } = true;

    /// <summary>
    ///     显示退票按钮
    /// </summary>
    public bool ShowRefundButton { get; set; } = true;

    /// <summary>
    ///     显示删除按钮
    /// </summary>
    public bool ShowDeleteButton { get; set; } = true;

    /// <summary>
    ///     默认展开行程区域
    /// </summary>
    public bool IsTripListExpandedByDefault { get; set; } = true;

    /// <summary>
    ///     DataGrid列配置列表
    /// </summary>
    public List<DataGridColumnConfig> DataGridColumns { get; set; }

    /// <summary>
    ///     默认行程列表视图类型（List/Card）
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ViewType DefaultTripListView { get; set; } = ViewType.List;

    #endregion

    #region 卡片视图显示设置

    /// <summary>
    ///     每行卡片数 (0=自动, 2-5)
    /// </summary>
    public int CardsPerRow { get; set; } = 0;

    /// <summary>
    ///     卡片宽度 (240-320像素)
    /// </summary>
    public int CardWidth { get; set; } = 280;

    /// <summary>
    ///     卡片间距 (4-16像素)
    /// </summary>
    public int CardSpacing { get; set; } = 8;

    /// <summary>
    ///     卡片圆角 (0-16像素)
    /// </summary>
    public int CardCornerRadius { get; set; } = 8;

    public bool CardShowViewAction { get; set; } = true;

    public bool CardShowEditAction { get; set; } = true;

    public bool CardShowRescheduleAction { get; set; } = true;

    public bool CardShowRefundAction { get; set; } = true;

    public bool CardShowDeleteAction { get; set; } = true;

    public bool CardEnableMultiSelect { get; set; } = true;

    /// <summary>
    ///     批量工具栏显示查看
    /// </summary>
    public bool CardBatchShowView { get; set; } = true;

    /// <summary>
    ///     批量工具栏显示编辑
    /// </summary>
    public bool CardBatchShowEdit { get; set; } = true;

    /// <summary>
    ///     批量工具栏显示改签
    /// </summary>
    public bool CardBatchShowReschedule { get; set; } = true;

    /// <summary>
    ///     批量工具栏显示退票
    /// </summary>
    public bool CardBatchShowRefund { get; set; } = true;

    /// <summary>
    ///     批量工具栏显示删除
    /// </summary>
    public bool CardBatchShowDelete { get; set; } = true;

    /// <summary>
    ///     状态标签位置 (TopRight/TopCenter/Hidden)
    /// </summary>
    public string CardStatusPosition { get; set; } = "TopRight";

    /// <summary>
    ///     悬停时高亮边框
    /// </summary>
    public bool CardHoverHighlight { get; set; } = true;

    /// <summary>
    ///     显示卡片阴影效果
    /// </summary>
    public bool CardShowShadow { get; set; } = true;

    /// <summary>
    ///     卡片悬停放大效果
    /// </summary>
    public bool CardHoverScale { get; set; } = false;

    #endregion

    #region 日志面板显示设置

    /// <summary>
    ///     日志行高
    /// </summary>
    public string LogRowHeight { get; set; } = "Standard";

    /// <summary>
    ///     信息颜色
    /// </summary>
    public string InfoColor { get; set; } = "#0078D4";

    /// <summary>
    ///     警告颜色
    /// </summary>
    public string WarningColor { get; set; } = "#FD7E14";

    /// <summary>
    ///     错误颜色
    /// </summary>
    public string ErrorColor { get; set; } = "#DC3545";

    /// <summary>
    ///     致命错误颜色
    /// </summary>
    public string FatalColor { get; set; } = "#6F42C1";

    /// <summary>
    ///     显示时间戳
    /// </summary>
    public bool ShowTimestamp { get; set; } = true;

    /// <summary>
    ///     显示模块来源
    /// </summary>
    public bool ShowModuleSource { get; set; } = true;

    #endregion
}