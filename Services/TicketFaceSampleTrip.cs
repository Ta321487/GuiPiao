using GuiPiao.Model;

namespace GuiPiao.Services;

/// <summary>
///     车票预览 / 导出测试共用的示例行程（与界面设置「票面版式」示例一致）。
/// </summary>
public static class TicketFaceSampleTrip
{
    public static TripItem Create() => new()
    {
        TrainNo = "K1020",
        DepartStation = "深圳东",
        ArriveStation = "九江",
        DepartDate = "2024-01-25",
        DepartTime = "18:55",
        ArriveTime = "21:10",
        SeatType = "二等座",
        Money = "163.5",
        CoachNo = "11车",
        SeatNo = "104",
        CheckInLocation = "候车室5",
        TicketPurpose = "仅供报销使用",
        Hint = "报销凭证 遗失不补|退票改签时须交回车站",
        TicketNumber = "L098229",
        Status = 0
    };
}
