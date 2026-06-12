using System;
using System.Globalization;
using System.Text.Json;

namespace GuiPiao.Model;

public static class TicketFaceLayoutPatch
{
    public static bool TryApplyKey(ObservableTicketFaceLayout target, string rawKey, object? value)
    {
        var key = NormalizeKey(rawKey);
        if (string.IsNullOrEmpty(key)) return false;

        if (key.EndsWith("fontfamily", StringComparison.Ordinal))
        {
            var s = CoerceString(value);
            return TrySetStringProperty(target, key, s);
        }

        if (!TryToDouble(value, out var d)) return false;
        return TrySetDoubleProperty(target, key, d);
    }

    private static string NormalizeKey(string raw) =>
        raw.Trim().Replace("_", string.Empty).ToLowerInvariant();

    private static bool TrySetStringProperty(ObservableTicketFaceLayout o, string key, string? s)
    {
        switch (key)
        {
            case "ticketserialfontfamily": o.TicketSerialFontFamily = s; return true;
            case "checkinfontfamily": o.CheckInFontFamily = s; return true;
            case "checkinvaluefontfamily": o.CheckInValueFontFamily = s; return true;
            case "stationnamefontfamily": o.StationNameFontFamily = s; return true;
            case "departstationnamefontfamily": o.DepartStationNameFontFamily = s; return true;
            case "arrivestationnamefontfamily": o.ArriveStationNameFontFamily = s; return true;
            case "departstationzhanfontfamily": o.DepartStationZhanFontFamily = s; return true;
            case "arrivestationzhanfontfamily": o.ArriveStationZhanFontFamily = s; return true;
            case "pinyinfontfamily": o.PinyinFontFamily = s; return true;
            case "trainnofontfamily": o.TrainNoFontFamily = s; return true;
            case "arrowfontfamily": o.ArrowFontFamily = s; return true;
            case "daterowfontfamily": o.DateRowFontFamily = s; return true;
            case "moneyrowfontfamily": o.MoneyRowFontFamily = s; return true;
            case "additionalinfofontfamily": o.AdditionalInfoFontFamily = s; return true;
            case "coachseatfontfamily": o.CoachSeatFontFamily = s; return true;
            case "seattypefontfamily": o.SeatTypeFontFamily = s; return true;
            case "purposefontfamily": o.PurposeFontFamily = s; return true;
            case "ticketmodificationtypefontfamily": o.TicketModificationTypeFontFamily = s; return true;
            case "idnamefontfamily": o.IdNameFontFamily = s; return true;
            case "hintfontfamily": o.HintFontFamily = s; return true;
            case "footerfontfamily": o.FooterFontFamily = s; return true;
            case "badgefontfamily": o.BadgeFontFamily = s; return true;
            default: return false;
        }
    }

    private static bool TrySetDoubleProperty(ObservableTicketFaceLayout o, string key, double d)
    {
        switch (key)
        {
            case "ticketserialleft": o.TicketSerialLeft = d; return true;
            case "ticketserialtop": o.TicketSerialTop = d; return true;
            case "ticketserialfont": o.TicketSerialFont = d; return true;
            case "checkinleft": o.CheckInLeft = d; return true;
            case "checkintop": o.CheckInTop = d; return true;
            case "checkinfont": o.CheckInFont = d; return true;
            case "checkinvalueleft": o.CheckInValueLeft = d; return true;
            case "checkinvaluetop": o.CheckInValueTop = d; return true;
            case "checkinvaluefont": o.CheckInValueFont = d; return true;
            case "departstationleft": o.DepartStationLeft = d; return true;
            case "departstationtop": o.DepartStationTop = d; return true;
            case "stationnamefont":
                o.StationNameFont = d;
                return true;
            case "departstationnamefont": o.DepartStationNameFont = d; return true;
            case "arrivestationnamefont": o.ArriveStationNameFont = d; return true;
            case "departstationcharacterspacing": o.DepartStationCharacterSpacing = (int)Math.Round(d); return true;
            case "arrivestationcharacterspacing": o.ArriveStationCharacterSpacing = (int)Math.Round(d); return true;
            case "departstationspacing1": o.DepartStationSpacing1 = (int)Math.Round(d); return true;
            case "departstationspacing2": o.DepartStationSpacing2 = (int)Math.Round(d); return true;
            case "departstationspacing3": o.DepartStationSpacing3 = (int)Math.Round(d); return true;
            case "departstationspacing4": o.DepartStationSpacing4 = (int)Math.Round(d); return true;
            case "departstationspacing5": o.DepartStationSpacing5 = (int)Math.Round(d); return true;
            case "arrivestationspacing1": o.ArriveStationSpacing1 = (int)Math.Round(d); return true;
            case "arrivestationspacing2": o.ArriveStationSpacing2 = (int)Math.Round(d); return true;
            case "arrivestationspacing3": o.ArriveStationSpacing3 = (int)Math.Round(d); return true;
            case "arrivestationspacing4": o.ArriveStationSpacing4 = (int)Math.Round(d); return true;
            case "arrivestationspacing5": o.ArriveStationSpacing5 = (int)Math.Round(d); return true;
            case "departstationleftoffset1": o.DepartStationLeftOffset1 = d; return true;
            case "departstationleftoffset2": o.DepartStationLeftOffset2 = d; return true;
            case "departstationleftoffset3": o.DepartStationLeftOffset3 = d; return true;
            case "departstationleftoffset4": o.DepartStationLeftOffset4 = d; return true;
            case "departstationleftoffset5": o.DepartStationLeftOffset5 = d; return true;
            case "arrivestationleftoffset1": o.ArriveStationLeftOffset1 = d; return true;
            case "arrivestationleftoffset2": o.ArriveStationLeftOffset2 = d; return true;
            case "arrivestationleftoffset3": o.ArriveStationLeftOffset3 = d; return true;
            case "arrivestationleftoffset4": o.ArriveStationLeftOffset4 = d; return true;
            case "arrivestationleftoffset5": o.ArriveStationLeftOffset5 = d; return true;
            case "departstationzhanleft": o.DepartStationZhanLeft = d; return true;
            case "departstationzhantop": o.DepartStationZhanTop = d; return true;
            case "departstationzhanfont": o.DepartStationZhanFont = d; return true;
            case "departstationzhangapleft": o.DepartStationZhanGapLeft = d; return true;
            case "departstationzhanoffsettop": o.DepartStationZhanOffsetTop = d; return true;
            case "arrivestationzhanleft": o.ArriveStationZhanLeft = d; return true;
            case "arrivestationzhantop": o.ArriveStationZhanTop = d; return true;
            case "arrivestationzhanfont": o.ArriveStationZhanFont = d; return true;
            case "arrivestationzhangapleft": o.ArriveStationZhanGapLeft = d; return true;
            case "arrivestationzhanoffsettop": o.ArriveStationZhanOffsetTop = d; return true;
            case "departpinyinleft": o.DepartPinyinLeft = d; return true;
            case "departpinyintop": o.DepartPinyinTop = d; return true;
            case "pinyinfont": o.PinyinFont = d; return true;
            case "trainnoleft": o.TrainNoLeft = d; return true;
            case "trainnotop": o.TrainNoTop = d; return true;
            case "trainnofont": o.TrainNoFont = d; return true;
            case "arrowleft": o.ArrowLeft = d; return true;
            case "arrowtop": o.ArrowTop = d; return true;
            case "arrowfont": o.ArrowFont = d; return true;
            case "arrowlength": o.ArrowLength = d; return true;
            case "arrowstrokethickness": o.ArrowStrokeThickness = d; return true;
            case "arrowheadlength": o.ArrowHeadLength = d; return true;
            case "arrowheadwidth": o.ArrowHeadWidth = d; return true;
            case "arrivestationleft": o.ArriveStationLeft = d; return true;
            case "arrivestationtop": o.ArriveStationTop = d; return true;
            case "arrivepinyinleft": o.ArrivePinyinLeft = d; return true;
            case "arrivepinyintop": o.ArrivePinyinTop = d; return true;
            case "daterowleft":
            {
                var delta = d - o.DateRowLeft;
                o.DateRowLeft = d;
                ShiftDateSegmentsHorizontal(o, delta);
                return true;
            }
            case "daterowtop":
            {
                var delta = d - o.DateRowTop;
                o.DateRowTop = d;
                ShiftDateSegmentsVertical(o, delta);
                return true;
            }
            case "daterowfont":
                o.DateRowFont = d;
                o.DateYearDigitsFont = d;
                o.DateNianCharFont = d;
                o.DateMonthDigitsFont = d;
                o.DateYueCharFont = d;
                o.DateDayDigitsFont = d;
                o.DateRiCharFont = d;
                o.DateTimeHmFont = d;
                o.DateKaiCharFont = d;
                return true;
            case "moneyrowleft": o.MoneyRowLeft = d; return true;
            case "moneyrowtop": o.MoneyRowTop = d; return true;
            case "moneyrowfont": o.MoneyRowFont = d; return true;
            case "moneysymbolleft": o.MoneySymbolLeft = d; return true;
            case "moneysymboltop": o.MoneySymbolTop = d; return true;
            case "moneysymbolfont": o.MoneySymbolFont = d; return true;
            case "moneyamountleft": o.MoneyAmountLeft = d; return true;
            case "moneyamounttop": o.MoneyAmountTop = d; return true;
            case "moneyamountfont": o.MoneyAmountFont = d; return true;
            case "moneyunitleft": o.MoneyUnitLeft = d; return true;
            case "moneyunittop": o.MoneyUnitTop = d; return true;
            case "moneyunitfont": o.MoneyUnitFont = d; return true;
            case "coachseatright": o.CoachSeatRight = d; return true;
            case "coachseattop": o.CoachSeatTop = d; return true;
            case "coachseatfont": o.CoachSeatFont = d; return true;
            case "coachnumberleft": o.CoachNumberLeft = d; return true;
            case "coachnumbertop": o.CoachNumberTop = d; return true;
            case "coachnumberfont": o.CoachNumberFont = d; return true;
            case "coachcheleft": o.CoachCheLeft = d; return true;
            case "coachchetop": o.CoachCheTop = d; return true;
            case "coachchefont": o.CoachCheFont = d; return true;
            case "seatnumberleft": o.SeatNumberLeft = d; return true;
            case "seatnumbertop": o.SeatNumberTop = d; return true;
            case "seatnumberfont": o.SeatNumberFont = d; return true;
            case "seathaoleft": o.SeatHaoLeft = d; return true;
            case "seathaotop": o.SeatHaoTop = d; return true;
            case "seathaofont": o.SeatHaoFont = d; return true;
            case "seattyperight": o.SeatTypeRight = d; return true;
            case "seattypetop": o.SeatTypeTop = d; return true;
            case "seattypefont": o.SeatTypeFont = d; return true;
            case "purposeleft": o.PurposeLeft = d; return true;
            case "purposetop": o.PurposeTop = d; return true;
            case "purposefont": o.PurposeFont = d; return true;
            case "additionalinfoleft": o.AdditionalInfoLeft = d; return true;
            case "additionalinfotop": o.AdditionalInfoTop = d; return true;
            case "additionalinfofont": o.AdditionalInfoFont = d; return true;
            case "ticketmodificationtypeleft": o.TicketModificationTypeLeft = d; return true;
            case "ticketmodificationtypetop": o.TicketModificationTypeTop = d; return true;
            case "ticketmodificationtypefont": o.TicketModificationTypeFont = d; return true;
            case "idnameleft": o.IdNameLeft = d; return true;
            case "idnametop": o.IdNameTop = d; return true;
            case "idnamefont": o.IdNameFont = d; return true;
            case "hintboxleft": o.HintBoxLeft = d; return true;
            case "hintboxtop": o.HintBoxTop = d; return true;
            case "hintboxwidth": o.HintBoxWidth = d; return true;
            case "hintfont": o.HintFont = d; return true;
            case "footerleft": o.FooterLeft = d; return true;
            case "footertop": o.FooterTop = d; return true;
            case "footerfont": o.FooterFont = d; return true;
            case "qrleft": o.QrLeft = d; return true;
            case "qrtop": o.QrTop = d; return true;
            case "qrsize": o.QrSize = d; return true;
            case "badgerowleft":
            {
                var delta = d - o.BadgeRowLeft;
                o.BadgeRowLeft = d;
                ShiftBadgeSegmentsHorizontal(o, delta);
                return true;
            }
            case "badgerowtop":
            {
                var delta = d - o.BadgeRowTop;
                o.BadgeRowTop = d;
                ShiftBadgeSegmentsVertical(o, delta);
                return true;
            }
            case "badgefont":
                o.BadgeFont = d;
                o.BadgeLetterXueFont = d;
                o.BadgeLetterHaiFont = d;
                o.BadgeLetterWangFont = d;
                o.BadgeLetterDiscountFont = d;
                o.BadgePaymentRowFont = d;
                return true;
            case "dateyeardigitsleft": o.DateYearDigitsLeft = d; return true;
            case "dateyeardigitstop": o.DateYearDigitsTop = d; return true;
            case "dateyeardigitsfont": o.DateYearDigitsFont = d; return true;
            case "dateniancharleft": o.DateNianCharLeft = d; return true;
            case "datenianchartop": o.DateNianCharTop = d; return true;
            case "dateniancharfont": o.DateNianCharFont = d; return true;
            case "datemonthdigitsleft": o.DateMonthDigitsLeft = d; return true;
            case "datemonthdigitstop": o.DateMonthDigitsTop = d; return true;
            case "datemonthdigitsfont": o.DateMonthDigitsFont = d; return true;
            case "dateyuecharleft": o.DateYueCharLeft = d; return true;
            case "dateyuechartop": o.DateYueCharTop = d; return true;
            case "dateyuecharfont": o.DateYueCharFont = d; return true;
            case "datedaydigitsleft": o.DateDayDigitsLeft = d; return true;
            case "datedaydigitstop": o.DateDayDigitsTop = d; return true;
            case "datedaydigitsfont": o.DateDayDigitsFont = d; return true;
            case "datericharleft": o.DateRiCharLeft = d; return true;
            case "daterichartop": o.DateRiCharTop = d; return true;
            case "datericharfont": o.DateRiCharFont = d; return true;
            case "datetimehmleft": o.DateTimeHmLeft = d; return true;
            case "datetimehmtop": o.DateTimeHmTop = d; return true;
            case "datetimehmfont": o.DateTimeHmFont = d; return true;
            case "datekaicharleft": o.DateKaiCharLeft = d; return true;
            case "datekaichartop": o.DateKaiCharTop = d; return true;
            case "datekaicharfont": o.DateKaiCharFont = d; return true;
            case "badgeletterxueleft": o.BadgeLetterXueLeft = d; return true;
            case "badgeletterxuetop": o.BadgeLetterXueTop = d; return true;
            case "badgeletterxuefont": o.BadgeLetterXueFont = d; return true;
            case "badgeletterhaileft": o.BadgeLetterHaiLeft = d; return true;
            case "badgeletterhaitop": o.BadgeLetterHaiTop = d; return true;
            case "badgeletterhaifont": o.BadgeLetterHaiFont = d; return true;
            case "badgeletterwangleft": o.BadgeLetterWangLeft = d; return true;
            case "badgeletterwangtop": o.BadgeLetterWangTop = d; return true;
            case "badgeletterwangfont": o.BadgeLetterWangFont = d; return true;
            case "badgeletterdiscountleft": o.BadgeLetterDiscountLeft = d; return true;
            case "badgeletterdiscounttop": o.BadgeLetterDiscountTop = d; return true;
            case "badgeletterdiscountfont": o.BadgeLetterDiscountFont = d; return true;
            case "badgepaymentrowleft": o.BadgePaymentRowLeft = d; return true;
            case "badgepaymentrowtop": o.BadgePaymentRowTop = d; return true;
            case "badgepaymentrowfont": o.BadgePaymentRowFont = d; return true;
            default: return false;
        }
    }

    private static void ShiftDateSegmentsHorizontal(ObservableTicketFaceLayout o, double delta)
    {
        if (Math.Abs(delta) < 0.0001) return;
        o.DateYearDigitsLeft += delta;
        o.DateNianCharLeft += delta;
        o.DateMonthDigitsLeft += delta;
        o.DateYueCharLeft += delta;
        o.DateDayDigitsLeft += delta;
        o.DateRiCharLeft += delta;
        o.DateTimeHmLeft += delta;
        o.DateKaiCharLeft += delta;
    }

    private static void ShiftDateSegmentsVertical(ObservableTicketFaceLayout o, double delta)
    {
        if (Math.Abs(delta) < 0.0001) return;
        o.DateYearDigitsTop += delta;
        o.DateNianCharTop += delta;
        o.DateMonthDigitsTop += delta;
        o.DateYueCharTop += delta;
        o.DateDayDigitsTop += delta;
        o.DateRiCharTop += delta;
        o.DateTimeHmTop += delta;
        o.DateKaiCharTop += delta;
    }

    private static void ShiftBadgeSegmentsHorizontal(ObservableTicketFaceLayout o, double delta)
    {
        if (Math.Abs(delta) < 0.0001) return;
        o.BadgeLetterXueLeft += delta;
        o.BadgeLetterHaiLeft += delta;
        o.BadgeLetterWangLeft += delta;
        o.BadgeLetterDiscountLeft += delta;
        o.BadgePaymentRowLeft += delta;
    }

    private static void ShiftBadgeSegmentsVertical(ObservableTicketFaceLayout o, double delta)
    {
        if (Math.Abs(delta) < 0.0001) return;
        o.BadgeLetterXueTop += delta;
        o.BadgeLetterHaiTop += delta;
        o.BadgeLetterWangTop += delta;
        o.BadgeLetterDiscountTop += delta;
        o.BadgePaymentRowTop += delta;
    }
    private static string? CoerceString(object? v) => v switch
    {
        null => null,
        string s => string.IsNullOrWhiteSpace(s) ? null : s.Trim(),
        JsonElement je when je.ValueKind == JsonValueKind.String => je.GetString(),
        _ => v.ToString()
    };

    private static bool TryToDouble(object? v, out double d)
    {
        d = default;
        switch (v)
        {
            case null:
                return false;
            case double dd:
                d = dd;
                return true;
            case float f:
                d = f;
                return true;
            case int i:
                d = i;
                return true;
            case long l:
                d = l;
                return true;
            case JsonElement je:
                if (je.ValueKind == JsonValueKind.Number && je.TryGetDouble(out d)) return true;
                if (je.ValueKind == JsonValueKind.String &&
                    double.TryParse(je.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out d))
                    return true;
                return false;
            case string s:
                return double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out d);
            default:
                return double.TryParse(Convert.ToString(v, CultureInfo.InvariantCulture), NumberStyles.Float,
                    CultureInfo.InvariantCulture, out d);
        }
    }
}
