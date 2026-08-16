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
    public string MoneyDisplay => $"¥{Money:0.00}";
    public string AdditionalInfoDisplay => OrDash(AdditionalInfo);
    public string TicketPurposeDisplay => OrDash(TicketPurpose);
    public string TicketModificationTypeDisplay => OrDash(TicketModificationType);
    public string HintDisplay => OrDash(Hint);
    public string DepartStationCodeDisplay => OrDash(DepartStationCode);
    public string ArriveStationCodeDisplay => OrDash(ArriveStationCode);

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
