using System.Collections.Generic;
using GuiPiao.Utils;

namespace GuiPiao.Model;

public class TrainRideInfo
{
    public int Id { get; set; }
    public string TicketNumber { get; set; } = string.Empty;
    public string CheckInLocation { get; set; } = string.Empty;
    public string DepartStation { get; set; } = string.Empty;
    public string TrainNo { get; set; } = string.Empty;
    public string ArriveStation { get; set; } = string.Empty;
    public string DepartStationPinyin { get; set; } = string.Empty;
    public string ArriveStationPinyin { get; set; } = string.Empty;
    public string DepartDate { get; set; } = string.Empty;
    public string DepartTime { get; set; } = string.Empty;
    public string ArriveTime { get; set; } = string.Empty;

    /// <summary>
    ///     到达相对出发日期的跨天数：0 当日，1 次日，2 第三天。
    /// </summary>
    public int ArriveDayOffset { get; set; }

    /// <summary>到达时间展示（含跨天）。</summary>
    public string ArriveTimeDisplay => ArriveTimeFormat.Format(ArriveTime, ArriveDayOffset);

    public string CoachNo { get; set; } = string.Empty;
    public string SeatNo { get; set; } = string.Empty;
    public decimal Money { get; set; }
    public string SeatType { get; set; } = string.Empty;
    public string AdditionalInfo { get; set; } = string.Empty;
    public string TicketPurpose { get; set; } = string.Empty;
    public string TicketModificationType { get; set; } = string.Empty;
    public int TicketTypeFlags { get; set; }
    public int PaymentChannelFlags { get; set; }
    public string Hint { get; set; } = string.Empty;
    public string DepartStationCode { get; set; } = string.Empty;
    public string ArriveStationCode { get; set; } = string.Empty;

    /// <summary>
    ///     行程状态（0-未出行, 1-已完成, 2-已改签, 3-已退票）
    /// </summary>
    public int Status { get; set; } = (int)TrainRideStatus.NotTraveled;

    /// <summary>
    ///     行程关联的标签列表（非数据库字段，用于UI展示）
    /// </summary>
    public List<TicketTag> Tags { get; set; } = new();
}
