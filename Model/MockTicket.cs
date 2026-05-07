namespace GuiPiao.Model
{
    /// <summary>
    /// Mock 车票数据模型 - 字段与实际数据库表 train_ride_info 保持一致
    /// </summary>
    public class MockTicket
    {
        public string Id { get; set; } = string.Empty;
        public string TrainNo { get; set; } = string.Empty;           // 车次号
        public string DepartStation { get; set; } = string.Empty;     // 出发站
        public string ArriveStation { get; set; } = string.Empty;     // 到达站
        public string DepartDate { get; set; } = string.Empty;        // 出发日期 (yyyy-MM-dd)
        public string DepartTime { get; set; } = string.Empty;        // 出发时间 (HH:mm)
        public string ArriveTime { get; set; } = string.Empty;        // 到达时间
        public string Status { get; set; } = string.Empty;            // 状态：已完成/未出行/已改签/已退票
        public string SeatType { get; set; } = string.Empty;          // 席别：二等座/一等座/商务座
        public double Price { get; set; }                             // 价格
        public double DepartLat { get; set; }                         // 出发站纬度
        public double DepartLng { get; set; }                         // 出发站经度
        public double ArriveLat { get; set; }                         // 到达站纬度
        public double ArriveLng { get; set; }                         // 到达站经度
    }
}
