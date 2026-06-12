namespace GuiPiao.Model;

/// <summary>
///     811×509 仿真票面各元素在 Canvas 上的位置与字号（红/蓝两套独立布局，与文档一致）。
/// </summary>
public sealed class TicketFaceLayout
{
    public double TicketSerialLeft { get; init; }
    public double TicketSerialTop { get; init; }
    public double TicketSerialFont { get; init; } = 22;

    public double CheckInLeft { get; init; }
    public double CheckInTop { get; init; }
    public double CheckInFont { get; init; } = 14;

    /// <summary>检票口内容（编号等）的 Canvas 锚点，与「检票：」标签独立。</summary>
    public double CheckInValueLeft { get; init; }
    public double CheckInValueTop { get; init; }
    public double CheckInValueFont { get; init; } = 14;

    public double DepartStationLeft { get; init; }
    public double DepartStationTop { get; init; }
    public double StationNameFont { get; init; } = 28;

    /// <summary>出发站名称主体字号（与 <see cref="ArriveStationNameFont" /> 独立，避免仅移动一边坐标时另一侧因共用 <see cref="StationNameFont" /> 绑定而重排）。</summary>
    public double DepartStationNameFont { get; init; } = 28;

    /// <summary>到达站名称主体字号。</summary>
    public double ArriveStationNameFont { get; init; } = 28;

    /// <summary>出发站名主体字间距参数（约千分之一 em 量级）；2～5 个汉字时在票面用细空白字符近似实现。</summary>
    public int DepartStationCharacterSpacing { get; init; }

    /// <summary>到达站名主体字间距；语义同 <see cref="DepartStationCharacterSpacing" />。</summary>
    public int ArriveStationCharacterSpacing { get; init; }

    /// <summary>出发站名后「站」字的 Canvas 坐标与字号（与站名主体独立）。</summary>
    public double DepartStationZhanLeft { get; init; }
    public double DepartStationZhanTop { get; init; }
    public double DepartStationZhanFont { get; init; } = 28;

    /// <summary>「站」字相对站名主体右缘的水平间距（px）；站名换字数/字间距时仍紧跟主体。</summary>
    public double DepartStationZhanGapLeft { get; init; }

    /// <summary>「站」字相对站名行的上偏移（px）。</summary>
    public double DepartStationZhanOffsetTop { get; init; }

    public double DepartPinyinLeft { get; init; }
    public double DepartPinyinTop { get; init; }
    public double PinyinFont { get; init; } = 12;

    public double TrainNoLeft { get; init; }
    public double TrainNoTop { get; init; }
    public double TrainNoFont { get; init; } = 26;

    public double ArrowLeft { get; init; }
    public double ArrowTop { get; init; }
    /// <summary>兼容旧布局：曾用作「→」字号；矢量箭头时载入 JSON 可用来推算 <see cref="ArrowLength" /> / <see cref="ArrowStrokeThickness" />。</summary>
    public double ArrowFont { get; init; } = 22;

    /// <summary>箭头总水平长度（px，与车次宽度相近的报销凭证样式）。</summary>
    public double ArrowLength { get; init; } = 54;

    /// <summary>箭杆线宽（px）。</summary>
    public double ArrowStrokeThickness { get; init; } = 1.15;

    /// <summary>三角尖沿 X 方向长度；0 表示由渲染器按 <see cref="ArrowLength" /> 自动估算。</summary>
    public double ArrowHeadLength { get; init; }

    /// <summary>三角底边宽度；0 表示自动。</summary>
    public double ArrowHeadWidth { get; init; }

    public double ArriveStationLeft { get; init; }
    public double ArriveStationTop { get; init; }

    /// <summary>到达站名后「站」字。</summary>
    public double ArriveStationZhanLeft { get; init; }
    public double ArriveStationZhanTop { get; init; }
    public double ArriveStationZhanFont { get; init; } = 28;

    /// <summary>到达站「站」字相对站名主体右缘的水平间距（px）。</summary>
    public double ArriveStationZhanGapLeft { get; init; }

    /// <summary>到达站「站」字相对站名行的上偏移（px）。</summary>
    public double ArriveStationZhanOffsetTop { get; init; }

    public double ArrivePinyinLeft { get; init; }
    public double ArrivePinyinTop { get; init; }

    public double DateRowLeft { get; init; }
    public double DateRowTop { get; init; }
    public double DateRowFont { get; init; } = 16;

    public double DateYearDigitsLeft { get; init; }
    public double DateYearDigitsTop { get; init; }
    public double DateYearDigitsFont { get; init; } = 16;

    public double DateNianCharLeft { get; init; }
    public double DateNianCharTop { get; init; }
    public double DateNianCharFont { get; init; } = 16;

    public double DateMonthDigitsLeft { get; init; }
    public double DateMonthDigitsTop { get; init; }
    public double DateMonthDigitsFont { get; init; } = 16;

    public double DateYueCharLeft { get; init; }
    public double DateYueCharTop { get; init; }
    public double DateYueCharFont { get; init; } = 16;

    public double DateDayDigitsLeft { get; init; }
    public double DateDayDigitsTop { get; init; }
    public double DateDayDigitsFont { get; init; } = 16;

    public double DateRiCharLeft { get; init; }
    public double DateRiCharTop { get; init; }
    public double DateRiCharFont { get; init; } = 16;

    public double DateTimeHmLeft { get; init; }
    public double DateTimeHmTop { get; init; }
    public double DateTimeHmFont { get; init; } = 16;

    public double DateKaiCharLeft { get; init; }
    public double DateKaiCharTop { get; init; }
    public double DateKaiCharFont { get; init; } = 16;

    public double MoneyRowLeft { get; init; }
    public double MoneyRowTop { get; init; }
    public double MoneyRowFont { get; init; } = 18;

    public double MoneySymbolLeft { get; init; }
    public double MoneySymbolTop { get; init; }
    public double MoneySymbolFont { get; init; } = 18;
    public double MoneyAmountLeft { get; init; }
    public double MoneyAmountTop { get; init; }
    public double MoneyAmountFont { get; init; } = 18;
    public double MoneyUnitLeft { get; init; }
    public double MoneyUnitTop { get; init; }
    public double MoneyUnitFont { get; init; } = 18;

    public double CoachSeatRight { get; init; }
    public double CoachSeatTop { get; init; }
    public double CoachSeatFont { get; init; } = 16;

    public double CoachNumberLeft { get; init; }
    public double CoachNumberTop { get; init; }
    public double CoachNumberFont { get; init; } = 16;
    public double CoachCheLeft { get; init; }
    public double CoachCheTop { get; init; }
    public double CoachCheFont { get; init; } = 16;
    public double SeatNumberLeft { get; init; }
    public double SeatNumberTop { get; init; }
    public double SeatNumberFont { get; init; } = 16;
    public double SeatHaoLeft { get; init; }
    public double SeatHaoTop { get; init; }
    public double SeatHaoFont { get; init; } = 16;

    public double SeatTypeRight { get; init; }
    public double SeatTypeTop { get; init; }
    public double SeatTypeFont { get; init; } = 15;

    public double TicketModificationTypeLeft { get; init; }
    public double TicketModificationTypeTop { get; init; }
    public double TicketModificationTypeFont { get; init; } = 12;

    public double PurposeLeft { get; init; }
    public double PurposeTop { get; init; }
    public double PurposeFont { get; init; } = 13;

    public double AdditionalInfoLeft { get; init; }
    public double AdditionalInfoTop { get; init; }
    public double AdditionalInfoFont { get; init; } = 11;

    public double IdNameLeft { get; init; }
    public double IdNameTop { get; init; }
    public double IdNameFont { get; init; } = 12;

    public double HintBoxLeft { get; init; }
    public double HintBoxTop { get; init; }
    public double HintBoxWidth { get; init; } = 420;
    public double HintFont { get; init; } = 11;

    public double FooterLeft { get; init; }
    public double FooterTop { get; init; }
    public double FooterFont { get; init; } = 10;

    public double QrLeft { get; init; }
    public double QrTop { get; init; }
    public double QrSize { get; init; } = 120;

    public double BadgeRowLeft { get; init; }
    public double BadgeRowTop { get; init; }
    public double BadgeFont { get; init; } = 12;

    public double BadgeLetterXueLeft { get; init; }
    public double BadgeLetterXueTop { get; init; }
    public double BadgeLetterXueFont { get; init; } = 12;

    public double BadgeLetterHaiLeft { get; init; }
    public double BadgeLetterHaiTop { get; init; }
    public double BadgeLetterHaiFont { get; init; } = 12;

    public double BadgeLetterWangLeft { get; init; }
    public double BadgeLetterWangTop { get; init; }
    public double BadgeLetterWangFont { get; init; } = 12;

    public double BadgeLetterDiscountLeft { get; init; }
    public double BadgeLetterDiscountTop { get; init; }
    public double BadgeLetterDiscountFont { get; init; } = 12;

    public double BadgePaymentRowLeft { get; init; }
    public double BadgePaymentRowTop { get; init; }
    public double BadgePaymentRowFont { get; init; } = 12;

    /// <summary>蓝票底图上的文字/控件布局（坐标可按真实底图再调）</summary>
    public static TicketFaceLayout BlueDefault() => new()
    {
        TicketSerialLeft = 52, TicketSerialTop = 36,
        CheckInLeft = 520, CheckInTop = 42,
        CheckInValueLeft = 564, CheckInValueTop = 42, CheckInValueFont = 14,
        DepartStationLeft = 48, DepartStationTop = 110,
        DepartStationZhanGapLeft = 2, DepartStationZhanOffsetTop = 0,
        DepartStationZhanFont = 28,
        DepartPinyinLeft = 48, DepartPinyinTop = 152,
        TrainNoLeft = 360, TrainNoTop = 118,
        ArrowLeft = 378, ArrowTop = 158, ArrowFont = 20,
        ArrowLength = 54, ArrowStrokeThickness = 1.15, ArrowHeadLength = 0, ArrowHeadWidth = 0,
        ArriveStationLeft = 520, ArriveStationTop = 110,
        ArriveStationZhanGapLeft = 2, ArriveStationZhanOffsetTop = 0,
        ArriveStationZhanFont = 28,
        ArrivePinyinLeft = 520, ArrivePinyinTop = 152,
        DateRowLeft = 48, DateRowTop = 210,
        DateYearDigitsLeft = 48, DateYearDigitsTop = 210, DateYearDigitsFont = 16,
        DateNianCharLeft = 88, DateNianCharTop = 210, DateNianCharFont = 16,
        DateMonthDigitsLeft = 104, DateMonthDigitsTop = 210, DateMonthDigitsFont = 16,
        DateYueCharLeft = 116, DateYueCharTop = 210, DateYueCharFont = 16,
        DateDayDigitsLeft = 132, DateDayDigitsTop = 210, DateDayDigitsFont = 16,
        DateRiCharLeft = 152, DateRiCharTop = 210, DateRiCharFont = 16,
        DateTimeHmLeft = 170, DateTimeHmTop = 210, DateTimeHmFont = 16,
        DateKaiCharLeft = 228, DateKaiCharTop = 210, DateKaiCharFont = 16,
        MoneyRowLeft = 48, MoneyRowTop = 248, MoneyRowFont = 18,
        MoneySymbolLeft = 48, MoneySymbolTop = 248, MoneySymbolFont = 18,
        MoneyAmountLeft = 64, MoneyAmountTop = 248, MoneyAmountFont = 18,
        MoneyUnitLeft = 118, MoneyUnitTop = 248, MoneyUnitFont = 18,
        CoachSeatRight = 760, CoachSeatTop = 210, CoachSeatFont = 16,
        CoachNumberLeft = 668, CoachNumberTop = 210, CoachNumberFont = 16,
        CoachCheLeft = 692, CoachCheTop = 210, CoachCheFont = 16,
        SeatNumberLeft = 712, SeatNumberTop = 210, SeatNumberFont = 16,
        SeatHaoLeft = 752, SeatHaoTop = 210, SeatHaoFont = 16,
        SeatTypeRight = 760, SeatTypeTop = 246,
        TicketModificationTypeLeft = 48, TicketModificationTypeTop = 278, TicketModificationTypeFont = 12,
        PurposeLeft = 48, PurposeTop = 300,
        AdditionalInfoLeft = 48, AdditionalInfoTop = 324, AdditionalInfoFont = 11,
        IdNameLeft = 48, IdNameTop = 332,
        HintBoxLeft = 48, HintBoxTop = 368, HintBoxWidth = 480,
        FooterLeft = 48, FooterTop = 462,
        QrLeft = 620, QrTop = 300,
        BadgeRowLeft = 200, BadgeRowTop = 248,
        BadgeLetterXueLeft = 200, BadgeLetterXueTop = 248, BadgeLetterXueFont = 12,
        BadgeLetterHaiLeft = 214, BadgeLetterHaiTop = 248, BadgeLetterHaiFont = 12,
        BadgeLetterWangLeft = 228, BadgeLetterWangTop = 248, BadgeLetterWangFont = 12,
        BadgeLetterDiscountLeft = 242, BadgeLetterDiscountTop = 248, BadgeLetterDiscountFont = 12,
        BadgePaymentRowLeft = 258, BadgePaymentRowTop = 248, BadgePaymentRowFont = 12
    };

    /// <summary>红票布局（与蓝票略有偏移）</summary>
    public static TicketFaceLayout RedDefault() => new()
    {
        TicketSerialLeft = 56, TicketSerialTop = 40,
        CheckInLeft = 524, CheckInTop = 46,
        CheckInValueLeft = 568, CheckInValueTop = 46, CheckInValueFont = 14,
        DepartStationLeft = 52, DepartStationTop = 114,
        DepartStationZhanGapLeft = 2, DepartStationZhanOffsetTop = 0,
        DepartStationZhanFont = 28,
        DepartPinyinLeft = 52, DepartPinyinTop = 156,
        TrainNoLeft = 364, TrainNoTop = 122,
        ArrowLeft = 382, ArrowTop = 162, ArrowFont = 20,
        ArrowLength = 54, ArrowStrokeThickness = 1.15, ArrowHeadLength = 0, ArrowHeadWidth = 0,
        ArriveStationLeft = 524, ArriveStationTop = 114,
        ArriveStationZhanGapLeft = 2, ArriveStationZhanOffsetTop = 0,
        ArriveStationZhanFont = 28,
        ArrivePinyinLeft = 524, ArrivePinyinTop = 156,
        DateRowLeft = 52, DateRowTop = 214,
        DateYearDigitsLeft = 52, DateYearDigitsTop = 214, DateYearDigitsFont = 16,
        DateNianCharLeft = 92, DateNianCharTop = 214, DateNianCharFont = 16,
        DateMonthDigitsLeft = 108, DateMonthDigitsTop = 214, DateMonthDigitsFont = 16,
        DateYueCharLeft = 120, DateYueCharTop = 214, DateYueCharFont = 16,
        DateDayDigitsLeft = 136, DateDayDigitsTop = 214, DateDayDigitsFont = 16,
        DateRiCharLeft = 156, DateRiCharTop = 214, DateRiCharFont = 16,
        DateTimeHmLeft = 174, DateTimeHmTop = 214, DateTimeHmFont = 16,
        DateKaiCharLeft = 232, DateKaiCharTop = 214, DateKaiCharFont = 16,
        MoneyRowLeft = 52, MoneyRowTop = 252, MoneyRowFont = 18,
        MoneySymbolLeft = 52, MoneySymbolTop = 252, MoneySymbolFont = 18,
        MoneyAmountLeft = 68, MoneyAmountTop = 252, MoneyAmountFont = 18,
        MoneyUnitLeft = 122, MoneyUnitTop = 252, MoneyUnitFont = 18,
        CoachSeatRight = 764, CoachSeatTop = 214, CoachSeatFont = 16,
        CoachNumberLeft = 672, CoachNumberTop = 214, CoachNumberFont = 16,
        CoachCheLeft = 696, CoachCheTop = 214, CoachCheFont = 16,
        SeatNumberLeft = 716, SeatNumberTop = 214, SeatNumberFont = 16,
        SeatHaoLeft = 756, SeatHaoTop = 214, SeatHaoFont = 16,
        SeatTypeRight = 764, SeatTypeTop = 250,
        TicketModificationTypeLeft = 52, TicketModificationTypeTop = 282, TicketModificationTypeFont = 12,
        PurposeLeft = 52, PurposeTop = 304,
        AdditionalInfoLeft = 52, AdditionalInfoTop = 328, AdditionalInfoFont = 11,
        IdNameLeft = 52, IdNameTop = 336,
        HintBoxLeft = 52, HintBoxTop = 372, HintBoxWidth = 480,
        FooterLeft = 52, FooterTop = 466,
        QrLeft = 624, QrTop = 304,
        BadgeRowLeft = 204, BadgeRowTop = 252,
        BadgeLetterXueLeft = 204, BadgeLetterXueTop = 252, BadgeLetterXueFont = 12,
        BadgeLetterHaiLeft = 218, BadgeLetterHaiTop = 252, BadgeLetterHaiFont = 12,
        BadgeLetterWangLeft = 232, BadgeLetterWangTop = 252, BadgeLetterWangFont = 12,
        BadgeLetterDiscountLeft = 246, BadgeLetterDiscountTop = 252, BadgeLetterDiscountFont = 12,
        BadgePaymentRowLeft = 262, BadgePaymentRowTop = 252, BadgePaymentRowFont = 12
    };
}
