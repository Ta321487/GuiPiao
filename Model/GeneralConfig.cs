namespace GuiPiao.Model
{
    /// <summary>
    /// 主题模式枚举
    /// </summary>
    public enum ThemeMode
    {
        Light = 0,      // 浅色模式
        Dark = 1,       // 深色模式
        System = 2      // 跟随系统
    }

    /// <summary>
    /// 强调色枚举
    /// </summary>
    public enum AccentColor
    {
        MicrosoftBlue = 0,   // 微软蓝
        FreshGreen = 1,      // 清新绿
        VitalityOrange = 2,  // 活力橙
        DarkPurple = 3,      // 暗夜紫
        MinimalGray = 4,     // 极简灰
        Custom = 5           // 自定义
    }

    /// <summary>
    /// 字体大小枚举
    /// </summary>
    public enum FontSizeOption
    {
        Small = 0,      // 小 (12px)
        Medium = 1,     // 中 (14px)
        Large = 2,      // 大 (16px)
        ExtraLarge = 3  // 特大 (18px)
    }

    /// <summary>
    /// 列表行高枚举
    /// </summary>
    public enum RowHeightOption
    {
        Compact = 0,    // 紧凑
        Standard = 1,   // 标准
        Loose = 2       // 宽松
    }

    /// <summary>
    /// 启动页面枚举
    /// </summary>
    public enum StartupPageOption
    {
        MainList = 0,   // 主界面行程列表
        Map = 1,        // 车票地图
        LastPage = 2    // 上次关闭页面
    }

    /// <summary>
    /// 上次关闭页面枚举
    /// </summary>
    public enum LastPageOption
    {
        MainList = 0,   // 主界面行程列表
        Map = 1,        // 车票地图
        LogManager = 2  // 日志管理窗口
    }

    /// <summary>
    /// 窗口状态枚举
    /// </summary>
    public enum WindowStateOption
    {
        Normal = 0,         // 正常窗口
        Maximized = 1,      // 最大化
        MinimizedToTray = 2 // 最小化到系统托盘
    }

    /// <summary>
    /// 列表排序方式枚举
    /// </summary>
    public enum SortOption
    {
        DateDesc = 0,   // 按出发日期降序
        DateAsc = 1,    // 按出发日期升序
        TrainNo = 2,    // 按车次号
        Departure = 3   // 按出发站
    }

    /// <summary>
    /// 数据加载范围枚举
    /// </summary>
    public enum LoadRangeOption
    {
        ThisYear = 0,   // 本年度
        All = 1,        // 全部
        Last3Months = 2,// 近3个月
        Last6Months = 3 // 近6个月
    }

    /// <summary>
    /// 席别枚举
    /// </summary>
    public enum DefaultSeatTypeOption
    {
        SecondClass = 0,        // 二等座
        FirstClass = 1,         // 一等座
        BusinessClass = 2,      // 商务座
        PremiumClass = 3,       // 特等座
        NewACHardSeat = 4,      // 新空调硬座
        SoftSeat = 5,           // 软座
        NewACHardSleeper = 6,   // 新空调硬卧
        NewACSoftSleeper = 7,   // 新空调软卧
        HardSleeperAsSeat = 8   // 硬卧代硬座
    }

    /// <summary>
    /// 车票状态枚举
    /// </summary>
    public enum DefaultTicketStatusOption
    {
        NotTraveled = 0, // 未出行
        Completed = 1    // 已完成
    }

    /// <summary>
    /// 双击动作枚举
    /// </summary>
    public enum DoubleClickActionOption
    {
        Edit = 0,       // 打开编辑窗口
        Preview = 1,    // 打开票面预览
        Map = 2         // 地图定位
    }

    /// <summary>
    /// 常规设置配置类
    /// </summary>
    public class GeneralConfig
    {
        // 外观与显示
        public ThemeMode ThemeMode { get; set; } = ThemeMode.Light;
        public AccentColor AccentColor { get; set; } = AccentColor.MicrosoftBlue;
        public string CustomColor { get; set; } = "#0078D4";
        public FontSizeOption FontSize { get; set; } = FontSizeOption.Medium;
        public RowHeightOption RowHeight { get; set; } = RowHeightOption.Standard;

        // 程序启动与运行
        public bool SingleInstance { get; set; } = true;
        public StartupPageOption StartupPage { get; set; } = StartupPageOption.MainList;
        public LastPageOption LastPage { get; set; } = LastPageOption.MainList;
        public WindowStateOption WindowState { get; set; } = WindowStateOption.Maximized;
        public bool AutoRefreshOnStartup { get; set; } = true;
        // 注意：退出自动备份功能已移至数据库设置页面统一管理

        // 窗口位置和大小（仅在 WindowState 为 Normal 时使用）
        public double WindowLeft { get; set; } = double.NaN;  // NaN 表示使用默认位置
        public double WindowTop { get; set; } = double.NaN;
        public double WindowWidth { get; set; } = 1000;  // 默认宽度
        public double WindowHeight { get; set; } = 700;  // 默认高度

        // 核心业务默认设置
        public int PageSize { get; set; } = 20;
        public SortOption DefaultSort { get; set; } = SortOption.DateDesc;
        public LoadRangeOption LoadRange { get; set; } = LoadRangeOption.ThisYear;
        public DefaultSeatTypeOption DefaultSeatType { get; set; } = DefaultSeatTypeOption.SecondClass;
        public DefaultTicketStatusOption DefaultTicketStatus { get; set; } = DefaultTicketStatusOption.Completed;
        public bool OcrEditConfirm { get; set; } = true;  // true=编辑后确认, false=直接保存
        public DoubleClickActionOption DoubleClickAction { get; set; } = DoubleClickActionOption.Edit;

        // 操作防护与确认
        public bool ConfirmOnDelete { get; set; } = true;
        public bool ConfirmOnBatchDelete { get; set; } = true;
        public bool ConfirmOnRestore { get; set; } = true;
        public bool EnableUndo { get; set; } = true;
        public int MaxUndoSteps { get; set; } = 5;
    }
}