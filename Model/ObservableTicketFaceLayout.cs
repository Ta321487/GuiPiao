using System;
using CommunityToolkit.Mvvm.ComponentModel;
using GuiPiao.Services;

namespace GuiPiao.Model;

/// <summary>
///     811×509 票面布局的可编辑副本（绑定 + JSON 导入导出）。
/// </summary>
public partial class ObservableTicketFaceLayout : ObservableObject
{
    public static ObservableTicketFaceLayout FromTemplate(TicketFaceLayout t)
    {
        var o = new ObservableTicketFaceLayout();
        o.ApplySnapshot(t);
        return o;
    }

    /// <summary>JSON 批量导入后通知 WPF 刷新全部布局绑定（否则部分 Canvas 块可能仍显示旧坐标）。</summary>
    public void NotifyAllPropertiesChanged() => OnPropertyChanged(string.Empty);

    public void ApplySnapshot(TicketFaceLayout t)
    {
        TicketSerialLeft = t.TicketSerialLeft;
        TicketSerialTop = t.TicketSerialTop;
        TicketSerialFont = t.TicketSerialFont;
        CheckInLeft = t.CheckInLeft;
        CheckInTop = t.CheckInTop;
        CheckInFont = t.CheckInFont;
        CheckInValueLeft = t.CheckInValueLeft;
        CheckInValueTop = t.CheckInValueTop;
        CheckInValueFont = t.CheckInValueFont;
        DepartStationLeft = t.DepartStationLeft;
        DepartStationTop = t.DepartStationTop;
        StationNameFont = t.StationNameFont;
        DepartStationNameFont = t.DepartStationNameFont;
        ArriveStationNameFont = t.ArriveStationNameFont;
        DepartStationCharacterSpacing = t.DepartStationCharacterSpacing;
        ArriveStationCharacterSpacing = t.ArriveStationCharacterSpacing;
        DepartStationZhanLeft = t.DepartStationZhanLeft;
        DepartStationZhanTop = t.DepartStationZhanTop;
        DepartStationZhanFont = t.DepartStationZhanFont;
        DepartStationZhanGapLeft = t.DepartStationZhanGapLeft;
        DepartStationZhanOffsetTop = t.DepartStationZhanOffsetTop;
        DepartPinyinLeft = t.DepartPinyinLeft;
        DepartPinyinTop = t.DepartPinyinTop;
        PinyinFont = t.PinyinFont;
        TrainNoLeft = t.TrainNoLeft;
        TrainNoTop = t.TrainNoTop;
        TrainNoFont = t.TrainNoFont;
        ArrowLeft = t.ArrowLeft;
        ArrowTop = t.ArrowTop;
        ArrowFont = t.ArrowFont;
        ArrowLength = t.ArrowLength;
        ArrowStrokeThickness = t.ArrowStrokeThickness;
        ArrowHeadLength = t.ArrowHeadLength;
        ArrowHeadWidth = t.ArrowHeadWidth;
        ArriveStationLeft = t.ArriveStationLeft;
        ArriveStationTop = t.ArriveStationTop;
        ArriveStationZhanLeft = t.ArriveStationZhanLeft;
        ArriveStationZhanTop = t.ArriveStationZhanTop;
        ArriveStationZhanFont = t.ArriveStationZhanFont;
        ArriveStationZhanGapLeft = t.ArriveStationZhanGapLeft;
        ArriveStationZhanOffsetTop = t.ArriveStationZhanOffsetTop;
        ArrivePinyinLeft = t.ArrivePinyinLeft;
        ArrivePinyinTop = t.ArrivePinyinTop;
        DateRowLeft = t.DateRowLeft;
        DateRowTop = t.DateRowTop;
        DateRowFont = t.DateRowFont;
        DateYearDigitsLeft = t.DateYearDigitsLeft;
        DateYearDigitsTop = t.DateYearDigitsTop;
        DateYearDigitsFont = t.DateYearDigitsFont;
        DateNianCharLeft = t.DateNianCharLeft;
        DateNianCharTop = t.DateNianCharTop;
        DateNianCharFont = t.DateNianCharFont;
        DateMonthDigitsLeft = t.DateMonthDigitsLeft;
        DateMonthDigitsTop = t.DateMonthDigitsTop;
        DateMonthDigitsFont = t.DateMonthDigitsFont;
        DateYueCharLeft = t.DateYueCharLeft;
        DateYueCharTop = t.DateYueCharTop;
        DateYueCharFont = t.DateYueCharFont;
        DateDayDigitsLeft = t.DateDayDigitsLeft;
        DateDayDigitsTop = t.DateDayDigitsTop;
        DateDayDigitsFont = t.DateDayDigitsFont;
        DateRiCharLeft = t.DateRiCharLeft;
        DateRiCharTop = t.DateRiCharTop;
        DateRiCharFont = t.DateRiCharFont;
        DateTimeHmLeft = t.DateTimeHmLeft;
        DateTimeHmTop = t.DateTimeHmTop;
        DateTimeHmFont = t.DateTimeHmFont;
        DateKaiCharLeft = t.DateKaiCharLeft;
        DateKaiCharTop = t.DateKaiCharTop;
        DateKaiCharFont = t.DateKaiCharFont;
        MoneyRowLeft = t.MoneyRowLeft;
        MoneyRowTop = t.MoneyRowTop;
        MoneyRowFont = t.MoneyRowFont;
        MoneySymbolLeft = t.MoneySymbolLeft;
        MoneySymbolTop = t.MoneySymbolTop;
        MoneySymbolFont = t.MoneySymbolFont;
        MoneyAmountLeft = t.MoneyAmountLeft;
        MoneyAmountTop = t.MoneyAmountTop;
        MoneyAmountFont = t.MoneyAmountFont;
        MoneyUnitLeft = t.MoneyUnitLeft;
        MoneyUnitTop = t.MoneyUnitTop;
        MoneyUnitFont = t.MoneyUnitFont;
        CoachSeatRight = t.CoachSeatRight;
        CoachSeatTop = t.CoachSeatTop;
        CoachSeatFont = t.CoachSeatFont;
        CoachJiaLeft = t.CoachJiaLeft;
        CoachJiaTop = t.CoachJiaTop;
        CoachJiaFont = t.CoachJiaFont;
        CoachNumberLeft = t.CoachNumberLeft;
        CoachNumberTop = t.CoachNumberTop;
        CoachNumberFont = t.CoachNumberFont;
        CoachCheLeft = t.CoachCheLeft;
        CoachCheTop = t.CoachCheTop;
        CoachCheFont = t.CoachCheFont;
        SeatNumberLeft = t.SeatNumberLeft;
        SeatNumberTop = t.SeatNumberTop;
        SeatNumberFont = t.SeatNumberFont;
        SeatHaoLeft = t.SeatHaoLeft;
        SeatHaoTop = t.SeatHaoTop;
        SeatHaoFont = t.SeatHaoFont;
        SeatTypeRight = t.SeatTypeRight;
        SeatTypeTop = t.SeatTypeTop;
        SeatTypeFont = t.SeatTypeFont;
        TicketModificationTypeLeft = t.TicketModificationTypeLeft;
        TicketModificationTypeTop = t.TicketModificationTypeTop;
        TicketModificationTypeFont = t.TicketModificationTypeFont;
        PurposeLeft = t.PurposeLeft;
        PurposeTop = t.PurposeTop;
        PurposeFont = t.PurposeFont;
        AdditionalInfoLeft = t.AdditionalInfoLeft;
        AdditionalInfoTop = t.AdditionalInfoTop;
        AdditionalInfoFont = t.AdditionalInfoFont;
        IdNameLeft = t.IdNameLeft;
        IdNameTop = t.IdNameTop;
        IdNameFont = t.IdNameFont;
        HintBoxLeft = t.HintBoxLeft;
        HintBoxTop = t.HintBoxTop;
        HintBoxWidth = t.HintBoxWidth;
        HintFont = t.HintFont;
        FooterLeft = t.FooterLeft;
        FooterTop = t.FooterTop;
        FooterFont = t.FooterFont;
        QrLeft = t.QrLeft;
        QrTop = t.QrTop;
        QrSize = t.QrSize;
        BadgeRowLeft = t.BadgeRowLeft;
        BadgeRowTop = t.BadgeRowTop;
        BadgeFont = t.BadgeFont;
        BadgeLetterXueLeft = t.BadgeLetterXueLeft;
        BadgeLetterXueTop = t.BadgeLetterXueTop;
        BadgeLetterXueFont = t.BadgeLetterXueFont;
        BadgeLetterHaiLeft = t.BadgeLetterHaiLeft;
        BadgeLetterHaiTop = t.BadgeLetterHaiTop;
        BadgeLetterHaiFont = t.BadgeLetterHaiFont;
        BadgeLetterWangLeft = t.BadgeLetterWangLeft;
        BadgeLetterWangTop = t.BadgeLetterWangTop;
        BadgeLetterWangFont = t.BadgeLetterWangFont;
        BadgeLetterDiscountLeft = t.BadgeLetterDiscountLeft;
        BadgeLetterDiscountTop = t.BadgeLetterDiscountTop;
        BadgeLetterDiscountFont = t.BadgeLetterDiscountFont;
        BadgePaymentRowLeft = t.BadgePaymentRowLeft;
        BadgePaymentRowTop = t.BadgePaymentRowTop;
        BadgePaymentRowFont = t.BadgePaymentRowFont;
        EnsureStationZhanFromLegacyIfUnset();
        EnsureCheckInValueFromLegacyIfUnset();
        EnsureTicketModificationTypeFromPurposeIfUnset();
        EnsureMoneySegmentsFromLegacyIfUnset();
        EnsureCoachSeatSegmentsFromLegacyIfUnset();
        EnsureAdditionalInfoFromLegacyIfUnset();
    }

    /// <summary>旧 JSON 仅 moneyRow 锚点时，为 ￥/数字/元 分段补默认坐标。</summary>
    public void EnsureMoneySegmentsFromLegacyIfUnset()
    {
        const double eps = 0.01;
        var unset = Math.Abs(MoneySymbolLeft) < eps && Math.Abs(MoneyAmountLeft) < eps && Math.Abs(MoneyUnitLeft) < eps;
        var baseFont = MoneyRowFont > eps ? MoneyRowFont : 18;
        if (unset && (Math.Abs(MoneyRowLeft) > eps || Math.Abs(MoneyRowTop) > eps))
        {
            var l = MoneyRowLeft;
            var t = MoneyRowTop;
            MoneySymbolLeft = l;
            MoneySymbolTop = t;
            MoneySymbolFont = baseFont;
            MoneyAmountLeft = l + 16;
            MoneyAmountTop = t;
            MoneyAmountFont = baseFont;
            MoneyUnitLeft = l + 70;
            MoneyUnitTop = t;
            MoneyUnitFont = baseFont;
        }
        else
        {
            if (Math.Abs(MoneySymbolFont) < eps) MoneySymbolFont = baseFont;
            if (Math.Abs(MoneyAmountFont) < eps) MoneyAmountFont = baseFont;
            if (Math.Abs(MoneyUnitFont) < eps) MoneyUnitFont = baseFont;
        }
    }

    /// <summary>旧 JSON 仅 coachSeat 锚点时，为车厢号/车/座位号/号 分段补默认坐标。</summary>
    public void EnsureCoachSeatSegmentsFromLegacyIfUnset()
    {
        const double eps = 0.01;
        var unset = Math.Abs(CoachNumberLeft) < eps && Math.Abs(CoachCheLeft) < eps &&
                    Math.Abs(SeatNumberLeft) < eps && Math.Abs(SeatHaoLeft) < eps;
        var baseFont = CoachSeatFont > eps ? CoachSeatFont : 16;
        var top = CoachSeatTop;
        if (unset && (Math.Abs(CoachSeatRight) > eps || Math.Abs(CoachSeatTop) > eps))
        {
            var anchor = CoachSeatRight;
            CoachJiaLeft = anchor - 112;
            CoachJiaTop = top;
            CoachJiaFont = baseFont;
            CoachNumberLeft = anchor - 92;
            CoachNumberTop = top;
            CoachNumberFont = baseFont;
            CoachCheLeft = anchor - 68;
            CoachCheTop = top;
            CoachCheFont = baseFont;
            SeatNumberLeft = anchor - 48;
            SeatNumberTop = top;
            SeatNumberFont = baseFont;
            SeatHaoLeft = anchor - 8;
            SeatHaoTop = top;
            SeatHaoFont = baseFont;
        }
        else
        {
            if (Math.Abs(CoachJiaFont) < eps) CoachJiaFont = baseFont;
            if (Math.Abs(CoachNumberFont) < eps) CoachNumberFont = baseFont;
            if (Math.Abs(CoachCheFont) < eps) CoachCheFont = baseFont;
            if (Math.Abs(SeatNumberFont) < eps) SeatNumberFont = baseFont;
            if (Math.Abs(SeatHaoFont) < eps) SeatHaoFont = baseFont;
            // 旧版式无「加」字锚点时，默认放在车厢号左侧
            if (Math.Abs(CoachJiaLeft) < eps && Math.Abs(CoachNumberLeft) > eps)
            {
                CoachJiaLeft = CoachNumberLeft - 20;
                CoachJiaTop = CoachNumberTop;
                if (Math.Abs(CoachJiaFont) < eps)
                    CoachJiaFont = CoachNumberFont > eps ? CoachNumberFont : baseFont;
            }
        }
    }

    /// <summary>旧 JSON 无附加信息锚点时，在「车票用途」下方写入合理默认。</summary>
    public void EnsureAdditionalInfoFromLegacyIfUnset()
    {
        const double eps = 0.01;
        if (Math.Abs(AdditionalInfoLeft) < eps && Math.Abs(AdditionalInfoTop) < eps && Math.Abs(AdditionalInfoFont) < eps)
        {
            AdditionalInfoLeft = PurposeLeft;
            AdditionalInfoTop = PurposeTop + 22;
            AdditionalInfoFont = 11;
        }
        else if (Math.Abs(AdditionalInfoFont) < eps)
            AdditionalInfoFont = 11;
    }

    /// <summary>旧 JSON 仅一条检票口锚点时，为「检票口内容」块补默认位置（约在「检票：」右侧）。</summary>
    public void EnsureCheckInValueFromLegacyIfUnset()
    {
        const double eps = 0.01;
        const double defaultContentOffset = 44;
        if (Math.Abs(CheckInValueLeft) < eps && Math.Abs(CheckInValueTop) < eps && Math.Abs(CheckInValueFont) < eps)
        {
            CheckInValueLeft = CheckInLeft + defaultContentOffset;
            CheckInValueTop = CheckInTop;
            CheckInValueFont = CheckInFont > eps ? CheckInFont : 14;
        }
        else if (Math.Abs(CheckInValueFont) < eps)
            CheckInValueFont = CheckInFont > eps ? CheckInFont : 14;
    }

    /// <summary>旧 JSON 无改签类型锚点时，在「车票用途」上方写入合理默认。</summary>
    public void EnsureTicketModificationTypeFromPurposeIfUnset()
    {
        const double eps = 0.01;
        var unset = Math.Abs(TicketModificationTypeLeft) < eps && Math.Abs(TicketModificationTypeTop) < eps &&
                    Math.Abs(TicketModificationTypeFont) < eps;
        if (unset)
        {
            TicketModificationTypeLeft = PurposeLeft;
            TicketModificationTypeTop = PurposeTop - 22;
            TicketModificationTypeFont = 12;
        }
        else if (Math.Abs(TicketModificationTypeFont) < eps)
            TicketModificationTypeFont = 12;
    }

    /// <summary>旧版仅有 DateRow/BadgeRow 锚点时，为分段填充默认坐标（JSON 缺字段时为 0）。</summary>
    public void EnsureDateAndBadgeSegmentsFromLegacyIfUnset()
    {
        const double eps = 0.01;
        var dateUnset = Math.Abs(DateYearDigitsLeft) < eps && Math.Abs(DateNianCharLeft) < eps &&
                         Math.Abs(DateMonthDigitsLeft) < eps && Math.Abs(DateTimeHmLeft) < eps;
        if (dateUnset && (Math.Abs(DateRowLeft) > eps || Math.Abs(DateRowTop) > eps))
        {
            var L = DateRowLeft;
            var top = DateRowTop;
            var f = DateRowFont;
            DateYearDigitsLeft = L;
            DateYearDigitsTop = top;
            DateYearDigitsFont = f;
            DateNianCharLeft = L + 40;
            DateNianCharTop = top;
            DateNianCharFont = f;
            DateMonthDigitsLeft = L + 56;
            DateMonthDigitsTop = top;
            DateMonthDigitsFont = f;
            DateYueCharLeft = L + 68;
            DateYueCharTop = top;
            DateYueCharFont = f;
            DateDayDigitsLeft = L + 84;
            DateDayDigitsTop = top;
            DateDayDigitsFont = f;
            DateRiCharLeft = L + 104;
            DateRiCharTop = top;
            DateRiCharFont = f;
            DateTimeHmLeft = L + 122;
            DateTimeHmTop = top;
            DateTimeHmFont = f;
            DateKaiCharLeft = L + 180;
            DateKaiCharTop = top;
            DateKaiCharFont = f;
        }

        var badgeUnset = Math.Abs(BadgeLetterXueLeft) < eps && Math.Abs(BadgePaymentRowLeft) < eps;
        if (badgeUnset && (Math.Abs(BadgeRowLeft) > eps || Math.Abs(BadgeRowTop) > eps))
        {
            var bl = BadgeRowLeft;
            var bt = BadgeRowTop;
            var bf = BadgeFont;
            BadgeLetterXueLeft = bl;
            BadgeLetterXueTop = bt;
            BadgeLetterXueFont = bf;
            BadgeLetterHaiLeft = bl + 12;
            BadgeLetterHaiTop = bt;
            BadgeLetterHaiFont = bf;
            BadgeLetterWangLeft = bl + 24;
            BadgeLetterWangTop = bt;
            BadgeLetterWangFont = bf;
            BadgeLetterDiscountLeft = bl + 36;
            BadgeLetterDiscountTop = bt;
            BadgeLetterDiscountFont = bf;
            BadgePaymentRowLeft = bl + 48;
            BadgePaymentRowTop = bt;
            BadgePaymentRowFont = bf;
        }
    }

    /// <summary>旧 JSON 无「站」字独立字段时，用站名锚点与字号推断合理默认。</summary>
    public void EnsureStationZhanFromLegacyIfUnset()
    {
        const double eps = 0.01;
        var zhanUnset = Math.Abs(DepartStationZhanLeft) < eps && Math.Abs(DepartStationZhanTop) < eps &&
                        Math.Abs(DepartStationZhanFont) < eps;
        var departNameBase = DepartStationNameFont > eps ? DepartStationNameFont : StationNameFont;
        if (zhanUnset)
        {
            DepartStationZhanLeft = DepartStationLeft + 100;
            DepartStationZhanTop = DepartStationTop;
            DepartStationZhanFont = departNameBase > eps ? departNameBase : 28;
        }
        else if (Math.Abs(DepartStationZhanFont) < eps)
            DepartStationZhanFont = departNameBase > eps ? departNameBase : 28;

        var arrUnset = Math.Abs(ArriveStationZhanLeft) < eps && Math.Abs(ArriveStationZhanTop) < eps &&
                       Math.Abs(ArriveStationZhanFont) < eps;
        var arriveNameBase = ArriveStationNameFont > eps ? ArriveStationNameFont : StationNameFont;
        if (arrUnset)
        {
            ArriveStationZhanLeft = ArriveStationLeft + 140;
            ArriveStationZhanTop = ArriveStationTop;
            ArriveStationZhanFont = arriveNameBase > eps ? arriveNameBase : 28;
        }
        else if (Math.Abs(ArriveStationZhanFont) < eps)
            ArriveStationZhanFont = arriveNameBase > eps ? arriveNameBase : 28;
    }

    /// <summary>旧 JSON 的 <see cref="StationNameFontFamily" /> 会挡住全局后备；票面渲染已改为「元素专用 → 全局」。</summary>
    public void EnsureStationNameFontFamilyDoesNotOverrideGlobal()
    {
        if (!string.IsNullOrWhiteSpace(StationNameFontFamily))
            StationNameFontFamily = null;
    }

    /// <summary>旧 JSON 只有单一字间距时，复制到 1～5 字各档；左边距微调默认为 0。</summary>
    public void EnsurePerHanCountStationLayoutFromLegacyIfUnset()
    {
        if (DepartStationSpacing1 == 0 && DepartStationSpacing2 == 0 && DepartStationSpacing3 == 0 &&
            DepartStationSpacing4 == 0 && DepartStationSpacing5 == 0 && DepartStationCharacterSpacing != 0)
        {
            DepartStationSpacing1 = DepartStationCharacterSpacing;
            DepartStationSpacing2 = DepartStationCharacterSpacing;
            DepartStationSpacing3 = DepartStationCharacterSpacing;
            DepartStationSpacing4 = DepartStationCharacterSpacing;
            DepartStationSpacing5 = DepartStationCharacterSpacing;
        }

        if (ArriveStationSpacing1 == 0 && ArriveStationSpacing2 == 0 && ArriveStationSpacing3 == 0 &&
            ArriveStationSpacing4 == 0 && ArriveStationSpacing5 == 0 && ArriveStationCharacterSpacing != 0)
        {
            ArriveStationSpacing1 = ArriveStationCharacterSpacing;
            ArriveStationSpacing2 = ArriveStationCharacterSpacing;
            ArriveStationSpacing3 = ArriveStationCharacterSpacing;
            ArriveStationSpacing4 = ArriveStationCharacterSpacing;
            ArriveStationSpacing5 = ArriveStationCharacterSpacing;
        }
    }

    /// <summary>旧 JSON 以 Canvas 绝对坐标存「站」字时，反推相对站名主体的间距/上偏移。</summary>
    public void EnsureStationZhanGapFromLegacyIfUnset()
    {
        const double eps = 0.01;
        double departGap;
        double departOffset;
        if (Math.Abs(DepartStationZhanGapLeft) < eps && Math.Abs(DepartStationZhanOffsetTop) < eps)
            ApplyLegacyStationZhanGapMigration(
                DepartStationLeft,
                DepartStationTop,
                DepartStationZhanLeft,
                DepartStationZhanTop,
                DepartStationCharacterSpacing,
                DepartStationNameFont > eps ? DepartStationNameFont : StationNameFont,
                out departGap,
                out departOffset);
        else
        {
            departGap = DepartStationZhanGapLeft;
            departOffset = DepartStationZhanOffsetTop;
        }

        if (Math.Abs(departGap - DepartStationZhanGapLeft) > eps)
            DepartStationZhanGapLeft = departGap;
        if (Math.Abs(departOffset - DepartStationZhanOffsetTop) > eps)
            DepartStationZhanOffsetTop = departOffset;

        double arriveGap;
        double arriveOffset;
        if (Math.Abs(ArriveStationZhanGapLeft) < eps && Math.Abs(ArriveStationZhanOffsetTop) < eps)
            ApplyLegacyStationZhanGapMigration(
                ArriveStationLeft,
                ArriveStationTop,
                ArriveStationZhanLeft,
                ArriveStationZhanTop,
                ArriveStationCharacterSpacing,
                ArriveStationNameFont > eps ? ArriveStationNameFont : StationNameFont,
                out arriveGap,
                out arriveOffset);
        else
        {
            arriveGap = ArriveStationZhanGapLeft;
            arriveOffset = ArriveStationZhanOffsetTop;
        }

        if (Math.Abs(arriveGap - ArriveStationZhanGapLeft) > eps)
            ArriveStationZhanGapLeft = arriveGap;
        if (Math.Abs(arriveOffset - ArriveStationZhanOffsetTop) > eps)
            ArriveStationZhanOffsetTop = arriveOffset;
    }

    private static void ApplyLegacyStationZhanGapMigration(
        double stationLeft,
        double stationTop,
        double zhanLeft,
        double zhanTop,
        int characterSpacing,
        double nameFont,
        out double gapLeft,
        out double offsetTop)
    {
        const double eps = 0.01;
        var font = nameFont > eps ? nameFont : 28;
        if (zhanLeft > stationLeft + 20)
        {
            var bodyWidth = TicketStationFaceMeasure.EstimateLegacyStationBodyWidth(characterSpacing, font);
            gapLeft = Math.Max(0, zhanLeft - stationLeft - bodyWidth);
            offsetTop = zhanTop - stationTop;
            return;
        }

        gapLeft = Math.Max(0, zhanLeft);
        offsetTop = zhanTop - stationTop;
    }

    public TicketFaceLayout ToImmutableSnapshot() => new()
    {
        TicketSerialLeft = TicketSerialLeft,
        TicketSerialTop = TicketSerialTop,
        TicketSerialFont = TicketSerialFont,
        CheckInLeft = CheckInLeft,
        CheckInTop = CheckInTop,
        CheckInFont = CheckInFont,
        CheckInValueLeft = CheckInValueLeft,
        CheckInValueTop = CheckInValueTop,
        CheckInValueFont = CheckInValueFont,
        DepartStationLeft = DepartStationLeft,
        DepartStationTop = DepartStationTop,
        StationNameFont = StationNameFont,
        DepartStationNameFont = DepartStationNameFont,
        ArriveStationNameFont = ArriveStationNameFont,
        DepartStationCharacterSpacing = DepartStationCharacterSpacing,
        ArriveStationCharacterSpacing = ArriveStationCharacterSpacing,
        DepartStationZhanLeft = DepartStationZhanLeft,
        DepartStationZhanTop = DepartStationZhanTop,
        DepartStationZhanFont = DepartStationZhanFont,
        DepartStationZhanGapLeft = DepartStationZhanGapLeft,
        DepartStationZhanOffsetTop = DepartStationZhanOffsetTop,
        DepartPinyinLeft = DepartPinyinLeft,
        DepartPinyinTop = DepartPinyinTop,
        PinyinFont = PinyinFont,
        TrainNoLeft = TrainNoLeft,
        TrainNoTop = TrainNoTop,
        TrainNoFont = TrainNoFont,
        ArrowLeft = ArrowLeft,
        ArrowTop = ArrowTop,
        ArrowFont = ArrowFont,
        ArrowLength = ArrowLength,
        ArrowStrokeThickness = ArrowStrokeThickness,
        ArrowHeadLength = ArrowHeadLength,
        ArrowHeadWidth = ArrowHeadWidth,
        ArriveStationLeft = ArriveStationLeft,
        ArriveStationTop = ArriveStationTop,
        ArriveStationZhanLeft = ArriveStationZhanLeft,
        ArriveStationZhanTop = ArriveStationZhanTop,
        ArriveStationZhanFont = ArriveStationZhanFont,
        ArriveStationZhanGapLeft = ArriveStationZhanGapLeft,
        ArriveStationZhanOffsetTop = ArriveStationZhanOffsetTop,
        ArrivePinyinLeft = ArrivePinyinLeft,
        ArrivePinyinTop = ArrivePinyinTop,
        DateRowLeft = DateRowLeft,
        DateRowTop = DateRowTop,
        DateRowFont = DateRowFont,
        DateYearDigitsLeft = DateYearDigitsLeft,
        DateYearDigitsTop = DateYearDigitsTop,
        DateYearDigitsFont = DateYearDigitsFont,
        DateNianCharLeft = DateNianCharLeft,
        DateNianCharTop = DateNianCharTop,
        DateNianCharFont = DateNianCharFont,
        DateMonthDigitsLeft = DateMonthDigitsLeft,
        DateMonthDigitsTop = DateMonthDigitsTop,
        DateMonthDigitsFont = DateMonthDigitsFont,
        DateYueCharLeft = DateYueCharLeft,
        DateYueCharTop = DateYueCharTop,
        DateYueCharFont = DateYueCharFont,
        DateDayDigitsLeft = DateDayDigitsLeft,
        DateDayDigitsTop = DateDayDigitsTop,
        DateDayDigitsFont = DateDayDigitsFont,
        DateRiCharLeft = DateRiCharLeft,
        DateRiCharTop = DateRiCharTop,
        DateRiCharFont = DateRiCharFont,
        DateTimeHmLeft = DateTimeHmLeft,
        DateTimeHmTop = DateTimeHmTop,
        DateTimeHmFont = DateTimeHmFont,
        DateKaiCharLeft = DateKaiCharLeft,
        DateKaiCharTop = DateKaiCharTop,
        DateKaiCharFont = DateKaiCharFont,
        MoneyRowLeft = MoneyRowLeft,
        MoneyRowTop = MoneyRowTop,
        MoneyRowFont = MoneyRowFont,
        MoneySymbolLeft = MoneySymbolLeft,
        MoneySymbolTop = MoneySymbolTop,
        MoneySymbolFont = MoneySymbolFont,
        MoneyAmountLeft = MoneyAmountLeft,
        MoneyAmountTop = MoneyAmountTop,
        MoneyAmountFont = MoneyAmountFont,
        MoneyUnitLeft = MoneyUnitLeft,
        MoneyUnitTop = MoneyUnitTop,
        MoneyUnitFont = MoneyUnitFont,
        CoachSeatRight = CoachSeatRight,
        CoachSeatTop = CoachSeatTop,
        CoachSeatFont = CoachSeatFont,
        CoachJiaLeft = CoachJiaLeft,
        CoachJiaTop = CoachJiaTop,
        CoachJiaFont = CoachJiaFont,
        CoachNumberLeft = CoachNumberLeft,
        CoachNumberTop = CoachNumberTop,
        CoachNumberFont = CoachNumberFont,
        CoachCheLeft = CoachCheLeft,
        CoachCheTop = CoachCheTop,
        CoachCheFont = CoachCheFont,
        SeatNumberLeft = SeatNumberLeft,
        SeatNumberTop = SeatNumberTop,
        SeatNumberFont = SeatNumberFont,
        SeatHaoLeft = SeatHaoLeft,
        SeatHaoTop = SeatHaoTop,
        SeatHaoFont = SeatHaoFont,
        SeatTypeRight = SeatTypeRight,
        SeatTypeTop = SeatTypeTop,
        SeatTypeFont = SeatTypeFont,
        TicketModificationTypeLeft = TicketModificationTypeLeft,
        TicketModificationTypeTop = TicketModificationTypeTop,
        TicketModificationTypeFont = TicketModificationTypeFont,
        PurposeLeft = PurposeLeft,
        PurposeTop = PurposeTop,
        PurposeFont = PurposeFont,
        AdditionalInfoLeft = AdditionalInfoLeft,
        AdditionalInfoTop = AdditionalInfoTop,
        AdditionalInfoFont = AdditionalInfoFont,
        IdNameLeft = IdNameLeft,
        IdNameTop = IdNameTop,
        IdNameFont = IdNameFont,
        HintBoxLeft = HintBoxLeft,
        HintBoxTop = HintBoxTop,
        HintBoxWidth = HintBoxWidth,
        HintFont = HintFont,
        FooterLeft = FooterLeft,
        FooterTop = FooterTop,
        FooterFont = FooterFont,
        QrLeft = QrLeft,
        QrTop = QrTop,
        QrSize = QrSize,
        BadgeRowLeft = BadgeRowLeft,
        BadgeRowTop = BadgeRowTop,
        BadgeFont = BadgeFont,
        BadgeLetterXueLeft = BadgeLetterXueLeft,
        BadgeLetterXueTop = BadgeLetterXueTop,
        BadgeLetterXueFont = BadgeLetterXueFont,
        BadgeLetterHaiLeft = BadgeLetterHaiLeft,
        BadgeLetterHaiTop = BadgeLetterHaiTop,
        BadgeLetterHaiFont = BadgeLetterHaiFont,
        BadgeLetterWangLeft = BadgeLetterWangLeft,
        BadgeLetterWangTop = BadgeLetterWangTop,
        BadgeLetterWangFont = BadgeLetterWangFont,
        BadgeLetterDiscountLeft = BadgeLetterDiscountLeft,
        BadgeLetterDiscountTop = BadgeLetterDiscountTop,
        BadgeLetterDiscountFont = BadgeLetterDiscountFont,
        BadgePaymentRowLeft = BadgePaymentRowLeft,
        BadgePaymentRowTop = BadgePaymentRowTop,
        BadgePaymentRowFont = BadgePaymentRowFont
    };

    [ObservableProperty] private double _ticketSerialLeft;
    [ObservableProperty] private double _ticketSerialTop;
    [ObservableProperty] private double _ticketSerialFont = 22;
    [ObservableProperty] private string? _ticketSerialFontFamily;

    [ObservableProperty] private double _checkInLeft;
    [ObservableProperty] private double _checkInTop;
    [ObservableProperty] private double _checkInFont = 14;
    [ObservableProperty] private string? _checkInFontFamily;

    [ObservableProperty] private double _checkInValueLeft;
    [ObservableProperty] private double _checkInValueTop;
    [ObservableProperty] private double _checkInValueFont = 14;
    [ObservableProperty] private string? _checkInValueFontFamily;

    [ObservableProperty] private double _departStationLeft;
    [ObservableProperty] private double _departStationTop;
    [ObservableProperty] private double _stationNameFont = 28;
    [ObservableProperty] private double _departStationNameFont = 28;
    [ObservableProperty] private double _arriveStationNameFont = 28;
    [ObservableProperty] private int _departStationCharacterSpacing;
    [ObservableProperty] private int _arriveStationCharacterSpacing;
    [ObservableProperty] private int _departStationSpacing1;
    [ObservableProperty] private int _departStationSpacing2;
    [ObservableProperty] private int _departStationSpacing3;
    [ObservableProperty] private int _departStationSpacing4;
    [ObservableProperty] private int _departStationSpacing5;
    [ObservableProperty] private int _arriveStationSpacing1;
    [ObservableProperty] private int _arriveStationSpacing2;
    [ObservableProperty] private int _arriveStationSpacing3;
    [ObservableProperty] private int _arriveStationSpacing4;
    [ObservableProperty] private int _arriveStationSpacing5;
    [ObservableProperty] private double _departStationLeftOffset1;
    [ObservableProperty] private double _departStationLeftOffset2;
    [ObservableProperty] private double _departStationLeftOffset3;
    [ObservableProperty] private double _departStationLeftOffset4;
    [ObservableProperty] private double _departStationLeftOffset5;
    [ObservableProperty] private double _arriveStationLeftOffset1;
    [ObservableProperty] private double _arriveStationLeftOffset2;
    [ObservableProperty] private double _arriveStationLeftOffset3;
    [ObservableProperty] private double _arriveStationLeftOffset4;
    [ObservableProperty] private double _arriveStationLeftOffset5;
    [ObservableProperty] private string? _stationNameFontFamily;
    /// <summary>出发站名主体专用字体（空则回退 <see cref="StationNameFontFamily" /> 共享站名字体）。</summary>
    [ObservableProperty] private string? _departStationNameFontFamily;
    /// <summary>到达站名主体专用字体（空则回退 <see cref="StationNameFontFamily" /> 共享站名字体）。</summary>
    [ObservableProperty] private string? _arriveStationNameFontFamily;

    [ObservableProperty] private double _departStationZhanLeft;
    [ObservableProperty] private double _departStationZhanTop;
    [ObservableProperty] private double _departStationZhanFont = 28;
    [ObservableProperty] private string? _departStationZhanFontFamily;
    [ObservableProperty] private double _departStationZhanGapLeft;
    [ObservableProperty] private double _departStationZhanOffsetTop;

    [ObservableProperty] private double _departPinyinLeft;
    [ObservableProperty] private double _departPinyinTop;
    [ObservableProperty] private double _pinyinFont = 12;
    [ObservableProperty] private string? _pinyinFontFamily;

    [ObservableProperty] private double _trainNoLeft;
    [ObservableProperty] private double _trainNoTop;
    [ObservableProperty] private double _trainNoFont = 26;
    [ObservableProperty] private string? _trainNoFontFamily;

    [ObservableProperty] private double _arrowLeft;
    [ObservableProperty] private double _arrowTop;
    [ObservableProperty] private double _arrowFont = 22;
    [ObservableProperty] private double _arrowLength = 54;
    [ObservableProperty] private double _arrowStrokeThickness = 1.15;
    [ObservableProperty] private double _arrowHeadLength;
    [ObservableProperty] private double _arrowHeadWidth;
    [ObservableProperty] private string? _arrowFontFamily;

    [ObservableProperty] private double _arriveStationLeft;
    [ObservableProperty] private double _arriveStationTop;

    [ObservableProperty] private double _arriveStationZhanLeft;
    [ObservableProperty] private double _arriveStationZhanTop;
    [ObservableProperty] private double _arriveStationZhanFont = 28;
    [ObservableProperty] private string? _arriveStationZhanFontFamily;
    [ObservableProperty] private double _arriveStationZhanGapLeft;
    [ObservableProperty] private double _arriveStationZhanOffsetTop;

    [ObservableProperty] private double _arrivePinyinLeft;
    [ObservableProperty] private double _arrivePinyinTop;

    [ObservableProperty] private double _dateRowLeft;
    [ObservableProperty] private double _dateRowTop;
    [ObservableProperty] private double _dateRowFont = 16;
    [ObservableProperty] private string? _dateRowFontFamily;

    [ObservableProperty] private double _dateYearDigitsLeft;
    [ObservableProperty] private double _dateYearDigitsTop;
    [ObservableProperty] private double _dateYearDigitsFont = 16;
    [ObservableProperty] private double _dateNianCharLeft;
    [ObservableProperty] private double _dateNianCharTop;
    [ObservableProperty] private double _dateNianCharFont = 16;
    [ObservableProperty] private double _dateMonthDigitsLeft;
    [ObservableProperty] private double _dateMonthDigitsTop;
    [ObservableProperty] private double _dateMonthDigitsFont = 16;
    [ObservableProperty] private double _dateYueCharLeft;
    [ObservableProperty] private double _dateYueCharTop;
    [ObservableProperty] private double _dateYueCharFont = 16;
    [ObservableProperty] private double _dateDayDigitsLeft;
    [ObservableProperty] private double _dateDayDigitsTop;
    [ObservableProperty] private double _dateDayDigitsFont = 16;
    [ObservableProperty] private double _dateRiCharLeft;
    [ObservableProperty] private double _dateRiCharTop;
    [ObservableProperty] private double _dateRiCharFont = 16;
    [ObservableProperty] private double _dateTimeHmLeft;
    [ObservableProperty] private double _dateTimeHmTop;
    [ObservableProperty] private double _dateTimeHmFont = 16;
    [ObservableProperty] private double _dateKaiCharLeft;
    [ObservableProperty] private double _dateKaiCharTop;
    [ObservableProperty] private double _dateKaiCharFont = 16;

    [ObservableProperty] private double _moneyRowLeft;
    [ObservableProperty] private double _moneyRowTop;
    [ObservableProperty] private double _moneyRowFont = 18;
    [ObservableProperty] private string? _moneyRowFontFamily;

    [ObservableProperty] private double _moneySymbolLeft;
    [ObservableProperty] private double _moneySymbolTop;
    [ObservableProperty] private double _moneySymbolFont = 18;
    [ObservableProperty] private double _moneyAmountLeft;
    [ObservableProperty] private double _moneyAmountTop;
    [ObservableProperty] private double _moneyAmountFont = 18;
    [ObservableProperty] private double _moneyUnitLeft;
    [ObservableProperty] private double _moneyUnitTop;
    [ObservableProperty] private double _moneyUnitFont = 18;

    [ObservableProperty] private double _coachSeatRight;
    [ObservableProperty] private double _coachSeatTop;
    [ObservableProperty] private double _coachSeatFont = 16;
    [ObservableProperty] private string? _coachSeatFontFamily;

    [ObservableProperty] private double _coachJiaLeft;
    [ObservableProperty] private double _coachJiaTop;
    [ObservableProperty] private double _coachJiaFont = 16;
    [ObservableProperty] private double _coachNumberLeft;
    [ObservableProperty] private double _coachNumberTop;
    [ObservableProperty] private double _coachNumberFont = 16;
    [ObservableProperty] private double _coachCheLeft;
    [ObservableProperty] private double _coachCheTop;
    [ObservableProperty] private double _coachCheFont = 16;
    [ObservableProperty] private double _seatNumberLeft;
    [ObservableProperty] private double _seatNumberTop;
    [ObservableProperty] private double _seatNumberFont = 16;
    [ObservableProperty] private double _seatHaoLeft;
    [ObservableProperty] private double _seatHaoTop;
    [ObservableProperty] private double _seatHaoFont = 16;

    [ObservableProperty] private double _seatTypeRight;
    [ObservableProperty] private double _seatTypeTop;
    [ObservableProperty] private double _seatTypeFont = 15;
    [ObservableProperty] private string? _seatTypeFontFamily;

    [ObservableProperty] private double _ticketModificationTypeLeft;
    [ObservableProperty] private double _ticketModificationTypeTop;
    [ObservableProperty] private double _ticketModificationTypeFont = 12;
    [ObservableProperty] private string? _ticketModificationTypeFontFamily;

    [ObservableProperty] private double _purposeLeft;
    [ObservableProperty] private double _purposeTop;
    [ObservableProperty] private double _purposeFont = 13;
    [ObservableProperty] private string? _purposeFontFamily;

    [ObservableProperty] private double _additionalInfoLeft;
    [ObservableProperty] private double _additionalInfoTop;
    [ObservableProperty] private double _additionalInfoFont = 11;
    [ObservableProperty] private string? _additionalInfoFontFamily;

    [ObservableProperty] private double _idNameLeft;
    [ObservableProperty] private double _idNameTop;
    [ObservableProperty] private double _idNameFont = 12;
    [ObservableProperty] private string? _idNameFontFamily;

    [ObservableProperty] private double _hintBoxLeft;
    [ObservableProperty] private double _hintBoxTop;
    [ObservableProperty] private double _hintBoxWidth = 480;
    [ObservableProperty] private double _hintFont = 11;
    [ObservableProperty] private string? _hintFontFamily;

    [ObservableProperty] private double _footerLeft;
    [ObservableProperty] private double _footerTop;
    [ObservableProperty] private double _footerFont = 10;
    [ObservableProperty] private string? _footerFontFamily;

    [ObservableProperty] private double _qrLeft;
    [ObservableProperty] private double _qrTop;
    [ObservableProperty] private double _qrSize = 120;

    [ObservableProperty] private double _badgeRowLeft;
    [ObservableProperty] private double _badgeRowTop;
    [ObservableProperty] private double _badgeFont = 12;
    [ObservableProperty] private string? _badgeFontFamily;

    [ObservableProperty] private double _badgeLetterXueLeft;
    [ObservableProperty] private double _badgeLetterXueTop;
    [ObservableProperty] private double _badgeLetterXueFont = 12;
    [ObservableProperty] private double _badgeLetterHaiLeft;
    [ObservableProperty] private double _badgeLetterHaiTop;
    [ObservableProperty] private double _badgeLetterHaiFont = 12;
    [ObservableProperty] private double _badgeLetterWangLeft;
    [ObservableProperty] private double _badgeLetterWangTop;
    [ObservableProperty] private double _badgeLetterWangFont = 12;
    [ObservableProperty] private double _badgeLetterDiscountLeft;
    [ObservableProperty] private double _badgeLetterDiscountTop;
    [ObservableProperty] private double _badgeLetterDiscountFont = 12;
    [ObservableProperty] private double _badgePaymentRowLeft;
    [ObservableProperty] private double _badgePaymentRowTop;
    [ObservableProperty] private double _badgePaymentRowFont = 12;
}
