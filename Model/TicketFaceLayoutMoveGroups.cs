using System.Collections.Generic;
using System.Linq;

namespace GuiPiao.Model;

/// <summary>
///     票面布局工作台「成组移动」：组内相对间距不变，整体平移。
/// </summary>
public static class TicketFaceLayoutMoveGroups
{
    public sealed record Group(string Name, IReadOnlyList<TicketFaceLayoutElementKind> Members);

    private static readonly Group[] Groups =
    {
        new("检票口", new[]
        {
            TicketFaceLayoutElementKind.CheckInLabel,
            TicketFaceLayoutElementKind.CheckInValue
        }),
        new("出发站", new[]
        {
            TicketFaceLayoutElementKind.DepartStation,
            TicketFaceLayoutElementKind.DepartStationZhan,
            TicketFaceLayoutElementKind.DepartPinyin
        }),
        new("到达站", new[]
        {
            TicketFaceLayoutElementKind.ArriveStation,
            TicketFaceLayoutElementKind.ArriveStationZhan,
            TicketFaceLayoutElementKind.ArrivePinyin
        }),
        new("车次·箭头", new[]
        {
            TicketFaceLayoutElementKind.TrainNo,
            TicketFaceLayoutElementKind.Arrow
        }),
        new("日期行", new[]
        {
            TicketFaceLayoutElementKind.DateYearDigits,
            TicketFaceLayoutElementKind.DateNianChar,
            TicketFaceLayoutElementKind.DateMonthDigits,
            TicketFaceLayoutElementKind.DateYueChar,
            TicketFaceLayoutElementKind.DateDayDigits,
            TicketFaceLayoutElementKind.DateRiChar,
            TicketFaceLayoutElementKind.DateTimeHm,
            TicketFaceLayoutElementKind.DateKaiChar
        }),
        new("金额行", new[]
        {
            TicketFaceLayoutElementKind.MoneySymbol,
            TicketFaceLayoutElementKind.MoneyAmount,
            TicketFaceLayoutElementKind.MoneyUnit,
            TicketFaceLayoutElementKind.MoneyRow
        }),
        new("车厢座位", new[]
        {
            TicketFaceLayoutElementKind.CoachJia,
            TicketFaceLayoutElementKind.CoachNumber,
            TicketFaceLayoutElementKind.CoachChe,
            TicketFaceLayoutElementKind.SeatNumber,
            TicketFaceLayoutElementKind.SeatHao,
            TicketFaceLayoutElementKind.CoachSeat
        }),
        new("简字区", new[]
        {
            TicketFaceLayoutElementKind.BadgeLetterXue,
            TicketFaceLayoutElementKind.BadgeLetterHai,
            TicketFaceLayoutElementKind.BadgeLetterWang,
            TicketFaceLayoutElementKind.BadgeLetterDiscount,
            TicketFaceLayoutElementKind.BadgePaymentRow
        })
    };

    public static Group? Find(TicketFaceLayoutElementKind kind) =>
        Groups.FirstOrDefault(g => g.Members.Contains(kind));

    public static bool IsStationGroup(Group group) =>
        group.Name is "出发站" or "到达站";

    /// <summary>
    ///     成组平移时实际写入坐标的成员。
    ///     「站」字为相对站名的 gap，跟站名走即可，不单独加 Δ。
    /// </summary>
    public static IReadOnlyList<TicketFaceLayoutElementKind> GetAbsoluteMoveMembers(
        Group group,
        bool includePinyin)
    {
        var list = new List<TicketFaceLayoutElementKind>(group.Members.Count);
        foreach (var m in group.Members)
        {
            if (m is TicketFaceLayoutElementKind.DepartStationZhan
                or TicketFaceLayoutElementKind.ArriveStationZhan)
                continue;

            if (!includePinyin &&
                m is TicketFaceLayoutElementKind.DepartPinyin or TicketFaceLayoutElementKind.ArrivePinyin)
                continue;

            // 兼容种仅作别名，避免重复平移
            if (m is TicketFaceLayoutElementKind.MoneyRow or TicketFaceLayoutElementKind.CoachSeat)
                continue;

            list.Add(m);
        }

        return list;
    }
}
