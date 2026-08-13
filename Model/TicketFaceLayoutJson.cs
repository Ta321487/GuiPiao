using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GuiPiao.Model;

public sealed class TicketFaceLayoutFileDto
{
    public int Version { get; set; } = 1;
    public string? DefaultFontFamily { get; set; }

    /// <summary>票面工作台上次选中的「编辑元素」枚举名（如 TicketSerial）；用于重开窗口后恢复滑块与布局块一致。</summary>
    public string? WorkbenchSelectedElement { get; set; }

    public TicketFaceLayoutSideDto? Blue { get; set; }
    public TicketFaceLayoutSideDto? Red { get; set; }
}

public sealed class TicketFaceLayoutSideDto
{
    public double TicketSerialLeft { get; set; }
    public double TicketSerialTop { get; set; }
    public double TicketSerialFont { get; set; }
    public string? TicketSerialFontFamily { get; set; }
    public double CheckInLeft { get; set; }
    public double CheckInTop { get; set; }
    public double CheckInFont { get; set; }
    public string? CheckInFontFamily { get; set; }
    public double CheckInValueLeft { get; set; }
    public double CheckInValueTop { get; set; }
    public double CheckInValueFont { get; set; }
    public string? CheckInValueFontFamily { get; set; }
    public double DepartStationLeft { get; set; }
    public double DepartStationTop { get; set; }
    public double StationNameFont { get; set; }
    public double DepartStationNameFont { get; set; }
    public double ArriveStationNameFont { get; set; }
    public int DepartStationCharacterSpacing { get; set; }
    public int ArriveStationCharacterSpacing { get; set; }
    public int DepartStationSpacing1 { get; set; }
    public int DepartStationSpacing2 { get; set; }
    public int DepartStationSpacing3 { get; set; }
    public int DepartStationSpacing4 { get; set; }
    public int DepartStationSpacing5 { get; set; }
    public int ArriveStationSpacing1 { get; set; }
    public int ArriveStationSpacing2 { get; set; }
    public int ArriveStationSpacing3 { get; set; }
    public int ArriveStationSpacing4 { get; set; }
    public int ArriveStationSpacing5 { get; set; }
    public double DepartStationLeftOffset1 { get; set; }
    public double DepartStationLeftOffset2 { get; set; }
    public double DepartStationLeftOffset3 { get; set; }
    public double DepartStationLeftOffset4 { get; set; }
    public double DepartStationLeftOffset5 { get; set; }
    public double ArriveStationLeftOffset1 { get; set; }
    public double ArriveStationLeftOffset2 { get; set; }
    public double ArriveStationLeftOffset3 { get; set; }
    public double ArriveStationLeftOffset4 { get; set; }
    public double ArriveStationLeftOffset5 { get; set; }
    public string? StationNameFontFamily { get; set; }
    public string? DepartStationNameFontFamily { get; set; }
    public string? ArriveStationNameFontFamily { get; set; }
    public double DepartStationZhanLeft { get; set; }
    public double DepartStationZhanTop { get; set; }
    public double DepartStationZhanFont { get; set; }
    public string? DepartStationZhanFontFamily { get; set; }
    public double DepartStationZhanGapLeft { get; set; }
    public double DepartStationZhanOffsetTop { get; set; }
    public double DepartPinyinLeft { get; set; }
    public double DepartPinyinTop { get; set; }
    public double PinyinFont { get; set; }
    public string? PinyinFontFamily { get; set; }
    public double TrainNoLeft { get; set; }
    public double TrainNoTop { get; set; }
    public double TrainNoFont { get; set; }
    public string? TrainNoFontFamily { get; set; }
    public double ArrowLeft { get; set; }
    public double ArrowTop { get; set; }
    public double ArrowFont { get; set; }
    public string? ArrowFontFamily { get; set; }
    public double ArrowLength { get; set; }
    public double ArrowStrokeThickness { get; set; }
    public double ArrowHeadLength { get; set; }
    public double ArrowHeadWidth { get; set; }
    public double ArriveStationLeft { get; set; }
    public double ArriveStationTop { get; set; }
    public double ArriveStationZhanLeft { get; set; }
    public double ArriveStationZhanTop { get; set; }
    public double ArriveStationZhanFont { get; set; }
    public string? ArriveStationZhanFontFamily { get; set; }
    public double ArriveStationZhanGapLeft { get; set; }
    public double ArriveStationZhanOffsetTop { get; set; }
    public double ArrivePinyinLeft { get; set; }
    public double ArrivePinyinTop { get; set; }
    public double DateRowLeft { get; set; }
    public double DateRowTop { get; set; }
    public double DateRowFont { get; set; }
    public string? DateRowFontFamily { get; set; }
    public double DateYearDigitsLeft { get; set; }
    public double DateYearDigitsTop { get; set; }
    public double DateYearDigitsFont { get; set; }
    public double DateNianCharLeft { get; set; }
    public double DateNianCharTop { get; set; }
    public double DateNianCharFont { get; set; }
    public double DateMonthDigitsLeft { get; set; }
    public double DateMonthDigitsTop { get; set; }
    public double DateMonthDigitsFont { get; set; }
    public double DateYueCharLeft { get; set; }
    public double DateYueCharTop { get; set; }
    public double DateYueCharFont { get; set; }
    public double DateDayDigitsLeft { get; set; }
    public double DateDayDigitsTop { get; set; }
    public double DateDayDigitsFont { get; set; }
    public double DateRiCharLeft { get; set; }
    public double DateRiCharTop { get; set; }
    public double DateRiCharFont { get; set; }
    public double DateTimeHmLeft { get; set; }
    public double DateTimeHmTop { get; set; }
    public double DateTimeHmFont { get; set; }
    public double DateKaiCharLeft { get; set; }
    public double DateKaiCharTop { get; set; }
    public double DateKaiCharFont { get; set; }
    public double MoneyRowLeft { get; set; }
    public double MoneyRowTop { get; set; }
    public double MoneyRowFont { get; set; }
    public string? MoneyRowFontFamily { get; set; }
    public double MoneySymbolLeft { get; set; }
    public double MoneySymbolTop { get; set; }
    public double MoneySymbolFont { get; set; }
    public double MoneyAmountLeft { get; set; }
    public double MoneyAmountTop { get; set; }
    public double MoneyAmountFont { get; set; }
    public double MoneyUnitLeft { get; set; }
    public double MoneyUnitTop { get; set; }
    public double MoneyUnitFont { get; set; }
    public double CoachSeatRight { get; set; }
    public double CoachSeatTop { get; set; }
    public double CoachSeatFont { get; set; }
    public string? CoachSeatFontFamily { get; set; }
    public double CoachJiaLeft { get; set; }
    public double CoachJiaTop { get; set; }
    public double CoachJiaFont { get; set; }
    public double CoachNumberLeft { get; set; }
    public double CoachNumberTop { get; set; }
    public double CoachNumberFont { get; set; }
    public double CoachCheLeft { get; set; }
    public double CoachCheTop { get; set; }
    public double CoachCheFont { get; set; }
    public double SeatNumberLeft { get; set; }
    public double SeatNumberTop { get; set; }
    public double SeatNumberFont { get; set; }
    public double SeatHaoLeft { get; set; }
    public double SeatHaoTop { get; set; }
    public double SeatHaoFont { get; set; }
    public double SeatTypeRight { get; set; }
    public double SeatTypeTop { get; set; }
    public double SeatTypeFont { get; set; }
    public string? SeatTypeFontFamily { get; set; }
    public double PurposeLeft { get; set; }
    public double PurposeTop { get; set; }
    public double PurposeFont { get; set; }
    public string? PurposeFontFamily { get; set; }
    public double AdditionalInfoLeft { get; set; }
    public double AdditionalInfoTop { get; set; }
    public double AdditionalInfoFont { get; set; }
    public string? AdditionalInfoFontFamily { get; set; }
    public double TicketModificationTypeLeft { get; set; }
    public double TicketModificationTypeTop { get; set; }
    public double TicketModificationTypeFont { get; set; }
    public string? TicketModificationTypeFontFamily { get; set; }
    public double IdNameLeft { get; set; }
    public double IdNameTop { get; set; }
    public double IdNameFont { get; set; }
    public string? IdNameFontFamily { get; set; }
    public double IdNumberLeft { get; set; }
    public double IdNumberTop { get; set; }
    public double IdNumberFont { get; set; }
    public string? IdNumberFontFamily { get; set; }
    public double IdMaskLeft { get; set; }
    public double IdMaskTop { get; set; }
    public double IdMaskFont { get; set; }
    public string? IdMaskFontFamily { get; set; }
    public double IdPassengerNameLeft { get; set; }
    public double IdPassengerNameTop { get; set; }
    public double IdPassengerNameFont { get; set; }
    public string? IdPassengerNameFontFamily { get; set; }
    public double HintBoxLeft { get; set; }
    public double HintBoxTop { get; set; }
    public double HintBoxWidth { get; set; }
    public double HintFont { get; set; }
    public string? HintFontFamily { get; set; }
    public double FooterLeft { get; set; }
    public double FooterTop { get; set; }
    public double FooterFont { get; set; }
    public string? FooterFontFamily { get; set; }
    public double QrLeft { get; set; }
    public double QrTop { get; set; }
    public double QrSize { get; set; }
    public double BadgeRowLeft { get; set; }
    public double BadgeRowTop { get; set; }
    public double BadgeFont { get; set; }
    public string? BadgeFontFamily { get; set; }
    public double BadgeLetterXueLeft { get; set; }
    public double BadgeLetterXueTop { get; set; }
    public double BadgeLetterXueFont { get; set; }
    public double BadgeLetterHaiLeft { get; set; }
    public double BadgeLetterHaiTop { get; set; }
    public double BadgeLetterHaiFont { get; set; }
    public double BadgeLetterWangLeft { get; set; }
    public double BadgeLetterWangTop { get; set; }
    public double BadgeLetterWangFont { get; set; }
    public double BadgeLetterDiscountLeft { get; set; }
    public double BadgeLetterDiscountTop { get; set; }
    public double BadgeLetterDiscountFont { get; set; }
    public double BadgePaymentRowLeft { get; set; }
    public double BadgePaymentRowTop { get; set; }
    public double BadgePaymentRowFont { get; set; }

    public static TicketFaceLayoutSideDto FromObservable(ObservableTicketFaceLayout o) => new()
    {
        TicketSerialLeft = o.TicketSerialLeft, TicketSerialTop = o.TicketSerialTop, TicketSerialFont = o.TicketSerialFont,
        TicketSerialFontFamily = o.TicketSerialFontFamily,
        CheckInLeft = o.CheckInLeft, CheckInTop = o.CheckInTop, CheckInFont = o.CheckInFont, CheckInFontFamily = o.CheckInFontFamily,
        CheckInValueLeft = o.CheckInValueLeft, CheckInValueTop = o.CheckInValueTop, CheckInValueFont = o.CheckInValueFont,
        CheckInValueFontFamily = o.CheckInValueFontFamily,
        DepartStationLeft = o.DepartStationLeft, DepartStationTop = o.DepartStationTop,
        StationNameFont = Math.Max(o.DepartStationNameFont, o.ArriveStationNameFont),
        DepartStationNameFont = o.DepartStationNameFont, ArriveStationNameFont = o.ArriveStationNameFont,
        DepartStationCharacterSpacing = o.DepartStationCharacterSpacing,
        ArriveStationCharacterSpacing = o.ArriveStationCharacterSpacing,
        DepartStationSpacing1 = o.DepartStationSpacing1,
        DepartStationSpacing2 = o.DepartStationSpacing2,
        DepartStationSpacing3 = o.DepartStationSpacing3,
        DepartStationSpacing4 = o.DepartStationSpacing4,
        DepartStationSpacing5 = o.DepartStationSpacing5,
        ArriveStationSpacing1 = o.ArriveStationSpacing1,
        ArriveStationSpacing2 = o.ArriveStationSpacing2,
        ArriveStationSpacing3 = o.ArriveStationSpacing3,
        ArriveStationSpacing4 = o.ArriveStationSpacing4,
        ArriveStationSpacing5 = o.ArriveStationSpacing5,
        DepartStationLeftOffset1 = o.DepartStationLeftOffset1,
        DepartStationLeftOffset2 = o.DepartStationLeftOffset2,
        DepartStationLeftOffset3 = o.DepartStationLeftOffset3,
        DepartStationLeftOffset4 = o.DepartStationLeftOffset4,
        DepartStationLeftOffset5 = o.DepartStationLeftOffset5,
        ArriveStationLeftOffset1 = o.ArriveStationLeftOffset1,
        ArriveStationLeftOffset2 = o.ArriveStationLeftOffset2,
        ArriveStationLeftOffset3 = o.ArriveStationLeftOffset3,
        ArriveStationLeftOffset4 = o.ArriveStationLeftOffset4,
        ArriveStationLeftOffset5 = o.ArriveStationLeftOffset5,
        StationNameFontFamily = o.StationNameFontFamily,
        DepartStationNameFontFamily = o.DepartStationNameFontFamily,
        ArriveStationNameFontFamily = o.ArriveStationNameFontFamily,
        DepartStationZhanLeft = o.DepartStationZhanLeft, DepartStationZhanTop = o.DepartStationZhanTop,
        DepartStationZhanFont = o.DepartStationZhanFont, DepartStationZhanFontFamily = o.DepartStationZhanFontFamily,
        DepartStationZhanGapLeft = o.DepartStationZhanGapLeft, DepartStationZhanOffsetTop = o.DepartStationZhanOffsetTop,
        DepartPinyinLeft = o.DepartPinyinLeft, DepartPinyinTop = o.DepartPinyinTop, PinyinFont = o.PinyinFont,
        PinyinFontFamily = o.PinyinFontFamily,
        TrainNoLeft = o.TrainNoLeft, TrainNoTop = o.TrainNoTop, TrainNoFont = o.TrainNoFont, TrainNoFontFamily = o.TrainNoFontFamily,
        ArrowLeft = o.ArrowLeft, ArrowTop = o.ArrowTop, ArrowFont = o.ArrowFont, ArrowFontFamily = o.ArrowFontFamily,
        ArrowLength = o.ArrowLength, ArrowStrokeThickness = o.ArrowStrokeThickness, ArrowHeadLength = o.ArrowHeadLength,
        ArrowHeadWidth = o.ArrowHeadWidth,
        ArriveStationLeft = o.ArriveStationLeft, ArriveStationTop = o.ArriveStationTop,
        ArriveStationZhanLeft = o.ArriveStationZhanLeft, ArriveStationZhanTop = o.ArriveStationZhanTop,
        ArriveStationZhanFont = o.ArriveStationZhanFont, ArriveStationZhanFontFamily = o.ArriveStationZhanFontFamily,
        ArriveStationZhanGapLeft = o.ArriveStationZhanGapLeft, ArriveStationZhanOffsetTop = o.ArriveStationZhanOffsetTop,
        ArrivePinyinLeft = o.ArrivePinyinLeft, ArrivePinyinTop = o.ArrivePinyinTop,
        DateRowLeft = o.DateRowLeft, DateRowTop = o.DateRowTop, DateRowFont = o.DateRowFont, DateRowFontFamily = o.DateRowFontFamily,
        DateYearDigitsLeft = o.DateYearDigitsLeft, DateYearDigitsTop = o.DateYearDigitsTop, DateYearDigitsFont = o.DateYearDigitsFont,
        DateNianCharLeft = o.DateNianCharLeft, DateNianCharTop = o.DateNianCharTop, DateNianCharFont = o.DateNianCharFont,
        DateMonthDigitsLeft = o.DateMonthDigitsLeft, DateMonthDigitsTop = o.DateMonthDigitsTop, DateMonthDigitsFont = o.DateMonthDigitsFont,
        DateYueCharLeft = o.DateYueCharLeft, DateYueCharTop = o.DateYueCharTop, DateYueCharFont = o.DateYueCharFont,
        DateDayDigitsLeft = o.DateDayDigitsLeft, DateDayDigitsTop = o.DateDayDigitsTop, DateDayDigitsFont = o.DateDayDigitsFont,
        DateRiCharLeft = o.DateRiCharLeft, DateRiCharTop = o.DateRiCharTop, DateRiCharFont = o.DateRiCharFont,
        DateTimeHmLeft = o.DateTimeHmLeft, DateTimeHmTop = o.DateTimeHmTop, DateTimeHmFont = o.DateTimeHmFont,
        DateKaiCharLeft = o.DateKaiCharLeft, DateKaiCharTop = o.DateKaiCharTop, DateKaiCharFont = o.DateKaiCharFont,
        MoneyRowLeft = o.MoneyRowLeft, MoneyRowTop = o.MoneyRowTop, MoneyRowFont = o.MoneyRowFont,
        MoneyRowFontFamily = o.MoneyRowFontFamily,
        MoneySymbolLeft = o.MoneySymbolLeft, MoneySymbolTop = o.MoneySymbolTop, MoneySymbolFont = o.MoneySymbolFont,
        MoneyAmountLeft = o.MoneyAmountLeft, MoneyAmountTop = o.MoneyAmountTop, MoneyAmountFont = o.MoneyAmountFont,
        MoneyUnitLeft = o.MoneyUnitLeft, MoneyUnitTop = o.MoneyUnitTop, MoneyUnitFont = o.MoneyUnitFont,
        CoachSeatRight = o.CoachSeatRight, CoachSeatTop = o.CoachSeatTop, CoachSeatFont = o.CoachSeatFont,
        CoachSeatFontFamily = o.CoachSeatFontFamily,
        CoachJiaLeft = o.CoachJiaLeft, CoachJiaTop = o.CoachJiaTop, CoachJiaFont = o.CoachJiaFont,
        CoachNumberLeft = o.CoachNumberLeft, CoachNumberTop = o.CoachNumberTop, CoachNumberFont = o.CoachNumberFont,
        CoachCheLeft = o.CoachCheLeft, CoachCheTop = o.CoachCheTop, CoachCheFont = o.CoachCheFont,
        SeatNumberLeft = o.SeatNumberLeft, SeatNumberTop = o.SeatNumberTop, SeatNumberFont = o.SeatNumberFont,
        SeatHaoLeft = o.SeatHaoLeft, SeatHaoTop = o.SeatHaoTop, SeatHaoFont = o.SeatHaoFont,
        SeatTypeRight = o.SeatTypeRight, SeatTypeTop = o.SeatTypeTop, SeatTypeFont = o.SeatTypeFont,
        SeatTypeFontFamily = o.SeatTypeFontFamily,
        PurposeLeft = o.PurposeLeft, PurposeTop = o.PurposeTop, PurposeFont = o.PurposeFont, PurposeFontFamily = o.PurposeFontFamily,
        AdditionalInfoLeft = o.AdditionalInfoLeft, AdditionalInfoTop = o.AdditionalInfoTop,
        AdditionalInfoFont = o.AdditionalInfoFont, AdditionalInfoFontFamily = o.AdditionalInfoFontFamily,
        TicketModificationTypeLeft = o.TicketModificationTypeLeft, TicketModificationTypeTop = o.TicketModificationTypeTop,
        TicketModificationTypeFont = o.TicketModificationTypeFont, TicketModificationTypeFontFamily = o.TicketModificationTypeFontFamily,
        IdNameLeft = o.IdNameLeft, IdNameTop = o.IdNameTop, IdNameFont = o.IdNameFont, IdNameFontFamily = o.IdNameFontFamily,
        IdNumberLeft = o.IdNumberLeft, IdNumberTop = o.IdNumberTop, IdNumberFont = o.IdNumberFont, IdNumberFontFamily = o.IdNumberFontFamily,
        IdMaskLeft = o.IdMaskLeft, IdMaskTop = o.IdMaskTop, IdMaskFont = o.IdMaskFont, IdMaskFontFamily = o.IdMaskFontFamily,
        IdPassengerNameLeft = o.IdPassengerNameLeft, IdPassengerNameTop = o.IdPassengerNameTop, IdPassengerNameFont = o.IdPassengerNameFont, IdPassengerNameFontFamily = o.IdPassengerNameFontFamily,
        HintBoxLeft = o.HintBoxLeft, HintBoxTop = o.HintBoxTop, HintBoxWidth = o.HintBoxWidth, HintFont = o.HintFont,
        HintFontFamily = o.HintFontFamily,
        FooterLeft = o.FooterLeft, FooterTop = o.FooterTop, FooterFont = o.FooterFont, FooterFontFamily = o.FooterFontFamily,
        QrLeft = o.QrLeft, QrTop = o.QrTop, QrSize = o.QrSize,
        BadgeRowLeft = o.BadgeRowLeft, BadgeRowTop = o.BadgeRowTop, BadgeFont = o.BadgeFont, BadgeFontFamily = o.BadgeFontFamily,
        BadgeLetterXueLeft = o.BadgeLetterXueLeft, BadgeLetterXueTop = o.BadgeLetterXueTop, BadgeLetterXueFont = o.BadgeLetterXueFont,
        BadgeLetterHaiLeft = o.BadgeLetterHaiLeft, BadgeLetterHaiTop = o.BadgeLetterHaiTop, BadgeLetterHaiFont = o.BadgeLetterHaiFont,
        BadgeLetterWangLeft = o.BadgeLetterWangLeft, BadgeLetterWangTop = o.BadgeLetterWangTop, BadgeLetterWangFont = o.BadgeLetterWangFont,
        BadgeLetterDiscountLeft = o.BadgeLetterDiscountLeft, BadgeLetterDiscountTop = o.BadgeLetterDiscountTop, BadgeLetterDiscountFont = o.BadgeLetterDiscountFont,
        BadgePaymentRowLeft = o.BadgePaymentRowLeft, BadgePaymentRowTop = o.BadgePaymentRowTop, BadgePaymentRowFont = o.BadgePaymentRowFont
    };

    public void ApplyTo(ObservableTicketFaceLayout o)
    {
        o.TicketSerialLeft = TicketSerialLeft;
        o.TicketSerialTop = TicketSerialTop;
        o.TicketSerialFont = TicketSerialFont;
        o.TicketSerialFontFamily = TicketSerialFontFamily;
        o.CheckInLeft = CheckInLeft;
        o.CheckInTop = CheckInTop;
        o.CheckInFont = CheckInFont;
        o.CheckInFontFamily = CheckInFontFamily;
        o.CheckInValueLeft = CheckInValueLeft;
        o.CheckInValueTop = CheckInValueTop;
        o.CheckInValueFont = CheckInValueFont;
        o.CheckInValueFontFamily = CheckInValueFontFamily;
        o.DepartStationLeft = DepartStationLeft;
        o.DepartStationTop = DepartStationTop;
        o.StationNameFont = StationNameFont;
        o.DepartStationNameFont = DepartStationNameFont;
        o.ArriveStationNameFont = ArriveStationNameFont;
        o.DepartStationCharacterSpacing = DepartStationCharacterSpacing;
        o.ArriveStationCharacterSpacing = ArriveStationCharacterSpacing;
        o.DepartStationSpacing1 = DepartStationSpacing1;
        o.DepartStationSpacing2 = DepartStationSpacing2;
        o.DepartStationSpacing3 = DepartStationSpacing3;
        o.DepartStationSpacing4 = DepartStationSpacing4;
        o.DepartStationSpacing5 = DepartStationSpacing5;
        o.ArriveStationSpacing1 = ArriveStationSpacing1;
        o.ArriveStationSpacing2 = ArriveStationSpacing2;
        o.ArriveStationSpacing3 = ArriveStationSpacing3;
        o.ArriveStationSpacing4 = ArriveStationSpacing4;
        o.ArriveStationSpacing5 = ArriveStationSpacing5;
        o.DepartStationLeftOffset1 = DepartStationLeftOffset1;
        o.DepartStationLeftOffset2 = DepartStationLeftOffset2;
        o.DepartStationLeftOffset3 = DepartStationLeftOffset3;
        o.DepartStationLeftOffset4 = DepartStationLeftOffset4;
        o.DepartStationLeftOffset5 = DepartStationLeftOffset5;
        o.ArriveStationLeftOffset1 = ArriveStationLeftOffset1;
        o.ArriveStationLeftOffset2 = ArriveStationLeftOffset2;
        o.ArriveStationLeftOffset3 = ArriveStationLeftOffset3;
        o.ArriveStationLeftOffset4 = ArriveStationLeftOffset4;
        o.ArriveStationLeftOffset5 = ArriveStationLeftOffset5;
        o.StationNameFontFamily = StationNameFontFamily;
        o.DepartStationNameFontFamily = DepartStationNameFontFamily;
        o.ArriveStationNameFontFamily = ArriveStationNameFontFamily;
        const double eps = 0.01;
        if (Math.Abs(o.DepartStationNameFont) < eps && o.StationNameFont > eps)
            o.DepartStationNameFont = o.StationNameFont;
        if (Math.Abs(o.ArriveStationNameFont) < eps && o.StationNameFont > eps)
            o.ArriveStationNameFont = o.StationNameFont;
        o.DepartStationZhanLeft = DepartStationZhanLeft;
        o.DepartStationZhanTop = DepartStationZhanTop;
        o.DepartStationZhanFont = DepartStationZhanFont;
        o.DepartStationZhanFontFamily = DepartStationZhanFontFamily;
        o.DepartStationZhanGapLeft = DepartStationZhanGapLeft;
        o.DepartStationZhanOffsetTop = DepartStationZhanOffsetTop;
        o.DepartPinyinLeft = DepartPinyinLeft;
        o.DepartPinyinTop = DepartPinyinTop;
        o.PinyinFont = PinyinFont;
        o.PinyinFontFamily = PinyinFontFamily;
        o.TrainNoLeft = TrainNoLeft;
        o.TrainNoTop = TrainNoTop;
        o.TrainNoFont = TrainNoFont;
        o.TrainNoFontFamily = TrainNoFontFamily;
        o.ArrowLeft = ArrowLeft;
        o.ArrowTop = ArrowTop;
        o.ArrowFont = ArrowFont;
        o.ArrowFontFamily = ArrowFontFamily;
        o.ArrowLength = ArrowLength;
        o.ArrowStrokeThickness = ArrowStrokeThickness;
        o.ArrowHeadLength = ArrowHeadLength;
        o.ArrowHeadWidth = ArrowHeadWidth;
        if (o.ArrowLength < eps && o.ArrowFont > eps)
            o.ArrowLength = Math.Clamp(o.ArrowFont * 2.35, 32.0, 96.0);
        if (o.ArrowStrokeThickness < 0.2 && o.ArrowFont > eps)
            o.ArrowStrokeThickness = Math.Clamp(o.ArrowFont / 17.5, 0.75, 2.8);
        if (o.ArrowLength < eps)
            o.ArrowLength = 54;
        if (o.ArrowStrokeThickness < 0.2)
            o.ArrowStrokeThickness = 1.15;
        o.ArriveStationLeft = ArriveStationLeft;
        o.ArriveStationTop = ArriveStationTop;
        o.ArriveStationZhanLeft = ArriveStationZhanLeft;
        o.ArriveStationZhanTop = ArriveStationZhanTop;
        o.ArriveStationZhanFont = ArriveStationZhanFont;
        o.ArriveStationZhanFontFamily = ArriveStationZhanFontFamily;
        o.ArriveStationZhanGapLeft = ArriveStationZhanGapLeft;
        o.ArriveStationZhanOffsetTop = ArriveStationZhanOffsetTop;
        o.ArrivePinyinLeft = ArrivePinyinLeft;
        o.ArrivePinyinTop = ArrivePinyinTop;
        o.DateRowLeft = DateRowLeft;
        o.DateRowTop = DateRowTop;
        o.DateRowFont = DateRowFont;
        o.DateRowFontFamily = DateRowFontFamily;
        o.DateYearDigitsLeft = DateYearDigitsLeft;
        o.DateYearDigitsTop = DateYearDigitsTop;
        o.DateYearDigitsFont = DateYearDigitsFont;
        o.DateNianCharLeft = DateNianCharLeft;
        o.DateNianCharTop = DateNianCharTop;
        o.DateNianCharFont = DateNianCharFont;
        o.DateMonthDigitsLeft = DateMonthDigitsLeft;
        o.DateMonthDigitsTop = DateMonthDigitsTop;
        o.DateMonthDigitsFont = DateMonthDigitsFont;
        o.DateYueCharLeft = DateYueCharLeft;
        o.DateYueCharTop = DateYueCharTop;
        o.DateYueCharFont = DateYueCharFont;
        o.DateDayDigitsLeft = DateDayDigitsLeft;
        o.DateDayDigitsTop = DateDayDigitsTop;
        o.DateDayDigitsFont = DateDayDigitsFont;
        o.DateRiCharLeft = DateRiCharLeft;
        o.DateRiCharTop = DateRiCharTop;
        o.DateRiCharFont = DateRiCharFont;
        o.DateTimeHmLeft = DateTimeHmLeft;
        o.DateTimeHmTop = DateTimeHmTop;
        o.DateTimeHmFont = DateTimeHmFont;
        o.DateKaiCharLeft = DateKaiCharLeft;
        o.DateKaiCharTop = DateKaiCharTop;
        o.DateKaiCharFont = DateKaiCharFont;
        o.MoneyRowLeft = MoneyRowLeft;
        o.MoneyRowTop = MoneyRowTop;
        o.MoneyRowFont = MoneyRowFont;
        o.MoneyRowFontFamily = MoneyRowFontFamily;
        o.MoneySymbolLeft = MoneySymbolLeft;
        o.MoneySymbolTop = MoneySymbolTop;
        o.MoneySymbolFont = MoneySymbolFont;
        o.MoneyAmountLeft = MoneyAmountLeft;
        o.MoneyAmountTop = MoneyAmountTop;
        o.MoneyAmountFont = MoneyAmountFont;
        o.MoneyUnitLeft = MoneyUnitLeft;
        o.MoneyUnitTop = MoneyUnitTop;
        o.MoneyUnitFont = MoneyUnitFont;
        o.CoachSeatRight = CoachSeatRight;
        o.CoachSeatTop = CoachSeatTop;
        o.CoachSeatFont = CoachSeatFont;
        o.CoachSeatFontFamily = CoachSeatFontFamily;
        o.CoachJiaLeft = CoachJiaLeft;
        o.CoachJiaTop = CoachJiaTop;
        o.CoachJiaFont = CoachJiaFont;
        o.CoachNumberLeft = CoachNumberLeft;
        o.CoachNumberTop = CoachNumberTop;
        o.CoachNumberFont = CoachNumberFont;
        o.CoachCheLeft = CoachCheLeft;
        o.CoachCheTop = CoachCheTop;
        o.CoachCheFont = CoachCheFont;
        o.SeatNumberLeft = SeatNumberLeft;
        o.SeatNumberTop = SeatNumberTop;
        o.SeatNumberFont = SeatNumberFont;
        o.SeatHaoLeft = SeatHaoLeft;
        o.SeatHaoTop = SeatHaoTop;
        o.SeatHaoFont = SeatHaoFont;
        o.SeatTypeRight = SeatTypeRight;
        o.SeatTypeTop = SeatTypeTop;
        o.SeatTypeFont = SeatTypeFont;
        o.SeatTypeFontFamily = SeatTypeFontFamily;
        o.PurposeLeft = PurposeLeft;
        o.PurposeTop = PurposeTop;
        o.PurposeFont = PurposeFont;
        o.PurposeFontFamily = PurposeFontFamily;
        o.AdditionalInfoLeft = AdditionalInfoLeft;
        o.AdditionalInfoTop = AdditionalInfoTop;
        o.AdditionalInfoFont = AdditionalInfoFont;
        o.AdditionalInfoFontFamily = AdditionalInfoFontFamily;
        o.TicketModificationTypeLeft = TicketModificationTypeLeft;
        o.TicketModificationTypeTop = TicketModificationTypeTop;
        o.TicketModificationTypeFont = TicketModificationTypeFont;
        o.TicketModificationTypeFontFamily = TicketModificationTypeFontFamily;
        o.IdNameLeft = IdNameLeft;
        o.IdNameTop = IdNameTop;
        o.IdNameFont = IdNameFont;
        o.IdNameFontFamily = IdNameFontFamily;
        o.IdNumberLeft = IdNumberLeft;
        o.IdNumberTop = IdNumberTop;
        o.IdNumberFont = IdNumberFont;
        o.IdNumberFontFamily = IdNumberFontFamily;
        o.IdMaskLeft = IdMaskLeft;
        o.IdMaskTop = IdMaskTop;
        o.IdMaskFont = IdMaskFont;
        o.IdMaskFontFamily = IdMaskFontFamily;
        o.IdPassengerNameLeft = IdPassengerNameLeft;
        o.IdPassengerNameTop = IdPassengerNameTop;
        o.IdPassengerNameFont = IdPassengerNameFont;
        o.IdPassengerNameFontFamily = IdPassengerNameFontFamily;
        o.HintBoxLeft = HintBoxLeft;
        o.HintBoxTop = HintBoxTop;
        o.HintBoxWidth = HintBoxWidth;
        o.HintFont = HintFont;
        o.HintFontFamily = HintFontFamily;
        o.FooterLeft = FooterLeft;
        o.FooterTop = FooterTop;
        o.FooterFont = FooterFont;
        o.FooterFontFamily = FooterFontFamily;
        o.QrLeft = QrLeft;
        o.QrTop = QrTop;
        o.QrSize = QrSize;
        o.BadgeRowLeft = BadgeRowLeft;
        o.BadgeRowTop = BadgeRowTop;
        o.BadgeFont = BadgeFont;
        o.BadgeFontFamily = BadgeFontFamily;
        o.BadgeLetterXueLeft = BadgeLetterXueLeft;
        o.BadgeLetterXueTop = BadgeLetterXueTop;
        o.BadgeLetterXueFont = BadgeLetterXueFont;
        o.BadgeLetterHaiLeft = BadgeLetterHaiLeft;
        o.BadgeLetterHaiTop = BadgeLetterHaiTop;
        o.BadgeLetterHaiFont = BadgeLetterHaiFont;
        o.BadgeLetterWangLeft = BadgeLetterWangLeft;
        o.BadgeLetterWangTop = BadgeLetterWangTop;
        o.BadgeLetterWangFont = BadgeLetterWangFont;
        o.BadgeLetterDiscountLeft = BadgeLetterDiscountLeft;
        o.BadgeLetterDiscountTop = BadgeLetterDiscountTop;
        o.BadgeLetterDiscountFont = BadgeLetterDiscountFont;
        o.BadgePaymentRowLeft = BadgePaymentRowLeft;
        o.BadgePaymentRowTop = BadgePaymentRowTop;
        o.BadgePaymentRowFont = BadgePaymentRowFont;
        o.EnsureDateAndBadgeSegmentsFromLegacyIfUnset();
        o.EnsureStationZhanFromLegacyIfUnset();
        o.EnsureStationZhanGapFromLegacyIfUnset();
        o.EnsureStationNameFontFamilyDoesNotOverrideGlobal();
        o.EnsurePerHanCountStationLayoutFromLegacyIfUnset();
        o.EnsureCheckInValueFromLegacyIfUnset();
        o.EnsureTicketModificationTypeFromPurposeIfUnset();
        o.EnsureMoneySegmentsFromLegacyIfUnset();
        o.EnsureCoachSeatSegmentsFromLegacyIfUnset();
        o.EnsureAdditionalInfoFromLegacyIfUnset();
        o.EnsureIdSegmentsFromLegacyIfUnset();
    }
}

public static class TicketFaceLayoutJson
{
    public static string DefaultRelativePath => Path.Combine("Config", "ticket-face-layout.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public static string GetDefaultFilePath() =>
        GuiPiao.Utils.JsonConfigManager.Instance.GetConfigFilePath("ticket-face-layout.json");

    public static TicketFaceLayoutFileDto BuildFileDto(string? defaultFontFamily, ObservableTicketFaceLayout blue,
        ObservableTicketFaceLayout red, string? workbenchSelectedElement = null) =>
        new()
        {
            Version = 4,
            DefaultFontFamily = string.IsNullOrWhiteSpace(defaultFontFamily) ? null : defaultFontFamily.Trim(),
            WorkbenchSelectedElement = string.IsNullOrWhiteSpace(workbenchSelectedElement)
                ? null
                : workbenchSelectedElement.Trim(),
            Blue = TicketFaceLayoutSideDto.FromObservable(blue),
            Red = TicketFaceLayoutSideDto.FromObservable(red)
        };

    public static string Serialize(TicketFaceLayoutFileDto dto) => JsonSerializer.Serialize(dto, JsonOptions);

    public static TicketFaceLayoutFileDto? Deserialize(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<TicketFaceLayoutFileDto>(json, JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    public static bool TryLoadFromFile(string path, out TicketFaceLayoutFileDto? dto)
    {
        dto = null;
        if (!File.Exists(path)) return false;
        try
        {
            dto = Deserialize(File.ReadAllText(path));
            return dto != null;
        }
        catch
        {
            return false;
        }
    }

    public static void SaveToFile(string path, TicketFaceLayoutFileDto dto)
    {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
        File.WriteAllText(path, Serialize(dto));
    }
}
