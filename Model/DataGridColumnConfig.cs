using System.Collections.Generic;

namespace GuiPiao.Model
{
    /// <summary>
    /// DataGrid列配置
    /// </summary>
    public class DataGridColumnConfig
    {
        /// <summary>
        /// 列名（绑定字段）
        /// </summary>
        public string FieldName { get; set; }

        /// <summary>
        /// 显示标题
        /// </summary>
        public string Header { get; set; }

        /// <summary>
        /// 是否显示
        /// </summary>
        public bool IsVisible { get; set; }

        /// <summary>
        /// 显示顺序
        /// </summary>
        public int DisplayOrder { get; set; }

        /// <summary>
        /// 列宽
        /// </summary>
        public string Width { get; set; }

        /// <summary>
        /// 最小列宽
        /// </summary>
        public double MinWidth { get; set; }

        /// <summary>
        /// 是否允许排序
        /// </summary>
        public bool CanSort { get; set; }

        /// <summary>
        /// 是否只读
        /// </summary>
        public bool IsReadOnly { get; set; }

        /// <summary>
        /// 获取默认列配置
        /// </summary>
        public static List<DataGridColumnConfig> GetDefaultColumns()
        {
            return new List<DataGridColumnConfig>
            {
                new DataGridColumnConfig { FieldName = "Id", Header = "序号", IsVisible = true, DisplayOrder = 0, Width = "50", MinWidth = 50, CanSort = true, IsReadOnly = true },
                new DataGridColumnConfig { FieldName = "TrainNo", Header = "车次", IsVisible = true, DisplayOrder = 1, Width = "80", MinWidth = 80, CanSort = true, IsReadOnly = true },
                new DataGridColumnConfig { FieldName = "DepartStation", Header = "出发站", IsVisible = true, DisplayOrder = 2, Width = "100", MinWidth = 100, CanSort = true, IsReadOnly = true },
                new DataGridColumnConfig { FieldName = "ArriveStation", Header = "到达站", IsVisible = true, DisplayOrder = 3, Width = "100", MinWidth = 100, CanSort = true, IsReadOnly = true },
                new DataGridColumnConfig { FieldName = "DepartDate", Header = "出发日期", IsVisible = true, DisplayOrder = 4, Width = "95", MinWidth = 95, CanSort = true, IsReadOnly = true },
                new DataGridColumnConfig { FieldName = "DepartTime", Header = "出发时间", IsVisible = true, DisplayOrder = 5, Width = "75", MinWidth = 75, CanSort = true, IsReadOnly = true },
                new DataGridColumnConfig { FieldName = "SeatType", Header = "席别", IsVisible = true, DisplayOrder = 6, Width = "80", MinWidth = 80, CanSort = true, IsReadOnly = true },
                new DataGridColumnConfig { FieldName = "Money", Header = "金额", IsVisible = true, DisplayOrder = 7, Width = "65", MinWidth = 65, CanSort = true, IsReadOnly = true },
                new DataGridColumnConfig { FieldName = "StatusDisplay", Header = "状态", IsVisible = true, DisplayOrder = 8, Width = "70", MinWidth = 70, CanSort = true, IsReadOnly = true },
                new DataGridColumnConfig { FieldName = "Tags", Header = "标签", IsVisible = true, DisplayOrder = 9, Width = "*", MinWidth = 120, CanSort = false, IsReadOnly = true },
                // 可选列
                new DataGridColumnConfig { FieldName = "CoachNo", Header = "车厢号", IsVisible = false, DisplayOrder = 10, Width = "70", MinWidth = 70, CanSort = true, IsReadOnly = true },
                new DataGridColumnConfig { FieldName = "SeatNo", Header = "座位号", IsVisible = false, DisplayOrder = 11, Width = "70", MinWidth = 70, CanSort = true, IsReadOnly = true },
                new DataGridColumnConfig { FieldName = "TicketNumber", Header = "票号", IsVisible = false, DisplayOrder = 12, Width = "120", MinWidth = 120, CanSort = true, IsReadOnly = true },
                new DataGridColumnConfig { FieldName = "DepartStationPinyin", Header = "出发站拼音", IsVisible = false, DisplayOrder = 13, Width = "100", MinWidth = 100, CanSort = true, IsReadOnly = true },
                new DataGridColumnConfig { FieldName = "ArriveStationPinyin", Header = "到达站拼音", IsVisible = false, DisplayOrder = 14, Width = "100", MinWidth = 100, CanSort = true, IsReadOnly = true },
                new DataGridColumnConfig { FieldName = "CheckInLocation", Header = "检票地点", IsVisible = false, DisplayOrder = 15, Width = "100", MinWidth = 100, CanSort = true, IsReadOnly = true },
                new DataGridColumnConfig { FieldName = "Hint", Header = "提示信息", IsVisible = false, DisplayOrder = 16, Width = "120", MinWidth = 120, CanSort = true, IsReadOnly = true },
                new DataGridColumnConfig { FieldName = "AdditionalInfo", Header = "票面附加信息", IsVisible = false, DisplayOrder = 17, Width = "150", MinWidth = 150, CanSort = false, IsReadOnly = true },
                new DataGridColumnConfig { FieldName = "TicketPurpose", Header = "购票用途", IsVisible = false, DisplayOrder = 18, Width = "100", MinWidth = 100, CanSort = true, IsReadOnly = true },
                new DataGridColumnConfig { FieldName = "TicketModificationType", Header = "改签类型", IsVisible = false, DisplayOrder = 19, Width = "100", MinWidth = 100, CanSort = true, IsReadOnly = true },
                new DataGridColumnConfig { FieldName = "TicketType", Header = "票种类型", IsVisible = false, DisplayOrder = 20, Width = "100", MinWidth = 100, CanSort = true, IsReadOnly = true },
                new DataGridColumnConfig { FieldName = "PaymentChannel", Header = "支付渠道", IsVisible = false, DisplayOrder = 21, Width = "100", MinWidth = 100, CanSort = true, IsReadOnly = true },
            };
        }
    }
}
