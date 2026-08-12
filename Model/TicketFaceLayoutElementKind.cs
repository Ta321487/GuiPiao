namespace GuiPiao.Model;

/// <summary>
///     811×509 票面上可独立调整位置/字号的逻辑块。
/// </summary>
public enum TicketFaceLayoutElementKind
{
    TicketSerial,

    /// <summary>检票口前缀「检票：」。</summary>
    CheckInLabel,

    /// <summary>检票口编号/地点（行程数据）。</summary>
    CheckInValue,

    DepartStation,

    /// <summary>出发站名后的「站」字（与站名块独立坐标/字号）。</summary>
    DepartStationZhan,

    DepartPinyin,
    TrainNo,
    Arrow,
    ArriveStation,

    /// <summary>到达站名后的「站」字。</summary>
    ArriveStationZhan,

    ArrivePinyin,
    DateRow,

    /// <summary>金额行：人民币符号「￥」。</summary>
    MoneySymbol,

    /// <summary>金额行：数字部分。</summary>
    MoneyAmount,

    /// <summary>金额行：单位「元」。</summary>
    MoneyUnit,

    /// <summary>兼容旧工作台/JSON；等同 <see cref="MoneySymbol"/>。</summary>
    MoneyRow,

    /// <summary>车厢号前的「加」字（加挂车厢）。</summary>
    CoachJia,

    /// <summary>车厢号数字（不含「车」字）。</summary>
    CoachNumber,

    /// <summary>车厢与座位之间的「车」字。</summary>
    CoachChe,

    /// <summary>座位号数字（不含「号」字）。</summary>
    SeatNumber,

    /// <summary>座位号后的「号」字。</summary>
    SeatHao,

    /// <summary>兼容旧工作台/JSON；等同 <see cref="CoachNumber"/>。</summary>
    CoachSeat,

    SeatType,

    /// <summary>票面附加信息（TripItem.AdditionalInfo）。</summary>
    AdditionalInfo,

    /// <summary>改签类型（业务字段 TripItem.TicketModificationType）。</summary>
    TicketModificationType,

    Purpose,

    /// <summary>兼容旧工作台/JSON；证件行整体锚点，等同 <see cref="IdMask"/> 为主。</summary>
    IdName,

    /// <summary>兼容旧工作台/JSON；完整号不上票面，等同 <see cref="IdMask"/>。</summary>
    IdNumber,

    /// <summary>票面身份证掩码（真票仅显示此项）。</summary>
    IdMask,

    /// <summary>票面旅客姓名。</summary>
    IdPassengerName,

    HintBox,
    Footer,
    Qr,
    BadgeRow,

    /// <summary>日期行：年份数字（如 2026）。</summary>
    DateYearDigits,

    /// <summary>日期行：「年」字。</summary>
    DateNianChar,

    /// <summary>日期行：月份数字。</summary>
    DateMonthDigits,

    /// <summary>日期行：「月」字。</summary>
    DateYueChar,

    /// <summary>日期行：日期数字。</summary>
    DateDayDigits,

    /// <summary>日期行：「日」字。</summary>
    DateRiChar,

    /// <summary>日期行：发车时间 HH:mm。</summary>
    DateTimeHm,

    /// <summary>日期行：「开」字。</summary>
    DateKaiChar,

    /// <summary>票种简字：学。</summary>
    BadgeLetterXue,

    /// <summary>票种简字：孩。</summary>
    BadgeLetterHai,

    /// <summary>票种简字：网。</summary>
    BadgeLetterWang,

    /// <summary>票种简字：折或惠。</summary>
    BadgeLetterDiscount,

    /// <summary>支付渠道简字行（支/微/银等）。</summary>
    BadgePaymentRow
}
