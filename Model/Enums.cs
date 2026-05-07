namespace GuiPiao.Model
{
    /// <summary>
    /// 票种类型枚举（与TrainTicketFormData中的位标志对应）
    /// </summary>
    public enum TicketType
    {
        None = 0,           // 无
        Student = 1,        // 学生票 (位0)
        Discount = 2,       // 优惠票 (位1)
        Online = 4,         // 网络售票 (位2)
        Child = 8           // 儿童票 (位3)
    }

    /// <summary>
    /// 支付渠道枚举（与TrainTicketFormData中的位标志对应）
    /// </summary>
    public enum PaymentChannel
    {
        None = 0,           // 无
        Alipay = 1,         // 支付宝售票 (位0)
        WeChat = 2,         // 微信售票 (位1)
        ABC = 4,            // 农业银行 (位2)
        CCB = 8,            // 建设银行 (位3)
        ICBC = 16,          // 工商银行 (位4)
        BCOM = 32,          // 交通银行 (位5)
        CMB = 64,           // 招商银行 (位6)
        PSBC = 128,         // 邮储银行 (位7)
        BOC = 256           // 中国银行 (位8)
    }

    public enum StationLevel
    {
        // 车站等级枚举
        Special = 1,        // 特等站
        First = 2,          // 一等站
        Second = 4,         // 二等站
        Third = 8,          // 三等站
        Fourth = 16,        // 四等站
        Fifth = 32          // 五等站
    }

    public enum TrainRideStatus
    {
        // 行程状态枚举
        NotTraveled = 0,    // 未出行 - 车票已录入但尚未乘车（未来车票）
        Completed = 1,      // 已完成 - 行程已完成（历史车票）
        Rescheduled = 2,    // 已改签 - 车票已改签为其他班次
        Refunded = 3        // 已退票 - 车票已退
    }
}
