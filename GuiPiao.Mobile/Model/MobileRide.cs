using GuiPiao.Mobile.Services;

namespace GuiPiao.Mobile.Model;

/// <summary>手机副本行程（字段与同步 ride payload 对齐）。</summary>
public class MobileRide
{
    public long Id { get; set; }
    public string SyncId { get; set; } = string.Empty;
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
    public int ArriveDayOffset { get; set; }
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
    public int Status { get; set; }
    public string UpdatedAt { get; set; } = string.Empty;
    public string? DeletedAt { get; set; }

    public string RouteText => $"{OrDash(DepartStation)} → {OrDash(ArriveStation)}";
    public string WhenText
    {
        get
        {
            if (string.IsNullOrWhiteSpace(DepartDate) && string.IsNullOrWhiteSpace(DepartTime))
                return "—";
            return string.IsNullOrWhiteSpace(DepartTime)
                ? DepartDate.Trim()
                : $"{DepartDate.Trim()} {DepartTime.Trim()}";
        }
    }

    /// <summary>与 PC 一致：CoachNo 已含「车」，不再追加。</summary>
    public string CoachSeatText
    {
        get
        {
            var c = (CoachNo ?? "").Trim();
            var s = (SeatNo ?? "").Trim();
            if (c.Length == 0 && s.Length == 0) return "—";
            if (c.Length == 0) return s;
            if (s.Length == 0) return c;
            return $"{c} {s}";
        }
    }

    public string TagsText { get; set; } = string.Empty;
    public bool HasTags => !string.IsNullOrWhiteSpace(TagsText);
    public string StatusText => Status switch
    {
        0 => "未出行",
        1 => "已完成",
        2 => "已改签",
        3 => "已退票",
        _ => $"状态{Status}"
    };

    public string TicketNumberDisplay => OrDash(TicketNumber);
    public string CheckInLocationDisplay => OrDash(CheckInLocation);
    public string DepartStationDisplay => OrDash(DepartStation);
    public string ArriveStationDisplay => OrDash(ArriveStation);
    public string DepartStationPinyinDisplay => OrDash(DepartStationPinyin);
    public string ArriveStationPinyinDisplay => OrDash(ArriveStationPinyin);
    public string DepartDateDisplay => OrDash(DepartDate);
    public string TrainNoDisplay => OrDash(TrainNo);
    public string DepartTimeDisplay => OrDash(DepartTime);
    public string ArriveTimeDisplay => OrDash(ArriveTime);
    public string CoachNoDisplay => OrDash(CoachNo);
    public string SeatNoDisplay => OrDash(SeatNo);
    public string SeatTypeDisplay => OrDash(SeatType);
    public string MoneyDisplay => MoneyFormat.Display(Money);
    public string AdditionalInfoDisplay => OrDash(AdditionalInfo);
    public string TicketPurposeDisplay => OrDash(TicketPurpose);
    public string TicketModificationTypeDisplay => OrDash(TicketModificationType);
    public string HintDisplay => OrDash(Hint);
    public string DepartStationCodeDisplay => OrDash(DepartStationCode);
    public string ArriveStationCodeDisplay => OrDash(ArriveStationCode);

    // —— 报销凭证票面（对齐 PC TicketPreviewDraft 语义，简易只读）——

    public string FaceSerial => (TicketNumber ?? "").Trim();
    public bool HasFaceSerial => FaceSerial.Length > 0;

    public string FaceCheckInLine =>
        string.IsNullOrWhiteSpace(CheckInLocation) ? string.Empty : $"检票：{CheckInLocation.Trim()}";
    public bool HasFaceCheckIn => FaceCheckInLine.Length > 0;

    public string FaceDepartStation => OrDash(DepartStation);
    public string FaceArriveStation => OrDash(ArriveStation);
    public string FaceDepartPinyin => (DepartStationPinyin ?? "").Trim().ToLowerInvariant();
    public string FaceArrivePinyin => (ArriveStationPinyin ?? "").Trim().ToLowerInvariant();
    public bool HasFaceDepartPinyin => FaceDepartPinyin.Length > 0;
    public bool HasFaceArrivePinyin => FaceArrivePinyin.Length > 0;

    public string FaceTrainNo => OrDash(TrainNo);

    /// <summary>与 PC 票面一致：yyyy年M月d日 HH:mm开</summary>
    public string FaceDepartDateOpenLine
    {
        get
        {
            var datePart = string.Empty;
            if (DateTime.TryParse(DepartDate, out var d))
                datePart = $"{d.Year}年{d.Month}月{d.Day}日";
            else if (!string.IsNullOrWhiteSpace(DepartDate))
                datePart = DepartDate.Trim();

            var time = (DepartTime ?? "").Trim();
            if (datePart.Length == 0 && time.Length == 0) return "—";
            if (time.Length == 0) return datePart;
            if (datePart.Length == 0) return $"{time}开";
            return $"{datePart} {time}开";
        }
    }

    /// <summary>票面车厢座位：座位号旁补「号」（与 PC 票面 SeatHao 一致）。</summary>
    public string FaceCoachSeat
    {
        get
        {
            var c = (CoachNo ?? "").Trim();
            var s = (SeatNo ?? "").Trim();
            if (c.Length == 0 && s.Length == 0) return "—";

            var seatPart = FormatFaceSeatNo(s);
            if (c.Length == 0) return seatPart;
            if (seatPart.Length == 0) return c;
            return $"{c} {seatPart}";
        }
    }

    /// <summary>有座位号时票面显示「…号」；无座/卧铺保持原文。</summary>
    private static string FormatFaceSeatNo(string seatNo)
    {
        if (seatNo.Length == 0) return string.Empty;
        if (seatNo.Contains("无座", StringComparison.Ordinal)) return seatNo;
        if (seatNo.Contains('上') || seatNo.Contains('中') || seatNo.Contains('下'))
            return seatNo;
        return seatNo.EndsWith("号", StringComparison.Ordinal) ? seatNo : seatNo + "号";
    }

    public string FaceSeatType => (SeatType ?? "").Trim();
    public bool HasFaceSeatType => FaceSeatType.Length > 0;

    /// <summary>票面金额一位小数 + ￥…元</summary>
    public string FaceMoneyLine =>
        $"{MoneyFormat.SymbolText}{Money.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)}元";

    public string FaceModification => (TicketModificationType ?? "").Trim();
    public bool HasFaceModification => FaceModification.Length > 0;
    public string FacePurpose => (TicketPurpose ?? "").Trim();
    public bool HasFacePurpose => FacePurpose.Length > 0;
    public string FaceAdditional => (AdditionalInfo ?? "").Trim();
    public bool HasFaceAdditional => FaceAdditional.Length > 0;

    public string FaceHintMultiline =>
        string.IsNullOrWhiteSpace(Hint) ? string.Empty : Hint.Replace('|', '\n').Trim();
    public bool HasFaceHint => FaceHintMultiline.Length > 0;

    public string FaceFooterReceiptLine =>
        string.IsNullOrWhiteSpace(TicketNumber) ? "报销凭证" : $"{TicketNumber.Trim()} 报销凭证";

    public static string OrDash(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "—" : value.Trim();
}

public class MobileTag
{
    public long Id { get; set; }
    public string SyncId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public string TextColor { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsDefault { get; set; }
    public string UpdatedAt { get; set; } = string.Empty;
    public string? DeletedAt { get; set; }
}
