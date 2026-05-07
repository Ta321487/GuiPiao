namespace GuiPiao.Model
{
    /// <summary>
    /// 地图设置配置类
    /// </summary>
    public class MapSettingsConfig
    {
        #region 行程显示与样式自定义

        /// <summary>
        /// 地图底图源
        /// </summary>
        public string MapTileSource { get; set; } = "osm";

        /// <summary>
        /// 打开地图默认显示选项
        /// </summary>
        public string DefaultMapDisplay { get; set; } = "AllSavedTrips";

        /// <summary>
        /// 已完成历史行程线路颜色
        /// </summary>
        public string CompletedTripColor { get; set; } = "#2E7D32";

        /// <summary>
        /// 未出行待乘行程线路颜色
        /// </summary>
        public string PendingTripColor { get; set; } = "#1976D2";

        /// <summary>
        /// 已改签行程线路颜色
        /// </summary>
        public string RescheduledTripColor { get; set; } = "#FF9800";

        /// <summary>
        /// 已退票行程线路颜色
        /// </summary>
        public string RefundedTripColor { get; set; } = "#9E9E9E";

        /// <summary>
        /// 已完成行程线条粗细
        /// </summary>
        public int CompletedLineWidth { get; set; } = 3;

        /// <summary>
        /// 未出行行程线条粗细
        /// </summary>
        public int PendingLineWidth { get; set; } = 4;

        /// <summary>
        /// 已改签行程线条粗细
        /// </summary>
        public int RescheduledLineWidth { get; set; } = 2;

        /// <summary>
        /// 已退票行程线条粗细
        /// </summary>
        public int RefundedLineWidth { get; set; } = 2;

        /// <summary>
        /// 选中行程高亮颜色
        /// </summary>
        public string SelectedTripColor { get; set; } = "#FF5722";

        /// <summary>
        /// 选中行程线条粗细
        /// </summary>
        public int SelectedLineWidth { get; set; } = 6;

        /// <summary>
        /// 站点标记颜色
        /// </summary>
        public string StationMarkerColor { get; set; } = "#D32F2F";

        /// <summary>
        /// 标记大小
        /// </summary>
        public int MarkerSize { get; set; } = 12;

        /// <summary>
        /// 显示车站名称标签
        /// </summary>
        public bool ShowStationLabels { get; set; } = true;

        /// <summary>
        /// 显示行程出发/到达日期标注
        /// </summary>
        public bool ShowDateLabels { get; set; } = true;

        /// <summary>
        /// 选中行程高亮置顶
        /// </summary>
        public bool HighlightSelectedTrip { get; set; } = true;

        /// <summary>
        /// 鼠标悬停显示车票信息卡片
        /// </summary>
        public bool ShowHoverCard { get; set; } = true;

        #endregion

        #region 地图交互行为设置

        /// <summary>
        /// 鼠标滚轮缩放
        /// </summary>
        public bool EnableMouseWheelZoom { get; set; } = true;

        /// <summary>
        /// 左键拖拽平移
        /// </summary>
        public bool EnableLeftDragPan { get; set; } = true;

        /// <summary>
        /// 右键重置视角
        /// </summary>
        public bool EnableRightClickReset { get; set; } = true;

        /// <summary>
        /// 滚轮缩放灵敏度 (%)
        /// </summary>
        public int ZoomSensitivity { get; set; } = 120;

        /// <summary>
        /// 开启平移惯性效果
        /// </summary>
        public bool EnablePanInertia { get; set; } = true;

        /// <summary>
        /// 左键双击行程线路行为
        /// </summary>
        public string DoubleClickTripAction { get; set; } = "OpenTicketEdit";

        /// <summary>
        /// 左键双击车站标记行为
        /// </summary>
        public string DoubleClickStationAction { get; set; } = "ShowStationTickets";

        /// <summary>
        /// 左键双击地图空白处行为
        /// </summary>
        public string DoubleClickBlankAction { get; set; } = "ZoomInMap";

        #endregion

        #region WebView2性能与存储管理

        /// <summary>
        /// 启用WebView2硬件加速
        /// </summary>
        public bool EnableHardwareAcceleration { get; set; } = true;

        /// <summary>
        /// 预加载地图页面资源
        /// </summary>
        public bool PreloadMapResources { get; set; } = true;

        /// <summary>
        /// 自动清理30天未访问的Web缓存
        /// </summary>
        public bool AutoCleanCache { get; set; } = true;

        #endregion

        #region 开发者选项

        /// <summary>
        /// 启用F12开发者工具
        /// </summary>
        public bool EnableDevTools { get; set; } = false;

        #endregion
    }
}
