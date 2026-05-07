using System;
using System.Collections.ObjectModel;

namespace GuiPiao.ViewModel.TrainTicketForm;

/// <summary>
///     火车票表单数据 DTO
/// </summary>
public class TrainTicketFormData
{
    // 车次号相关
    public string SelectedTrainNoPrefix { get; set; } = "G";
    public string TrainNoNumber { get; set; } = string.Empty;
    public string TrainNo => SelectedTrainNoPrefix == "纯数字" ? TrainNoNumber : $"{SelectedTrainNoPrefix}{TrainNoNumber}";

    // 车站相关
    public string DepartStationInput { get; set; } = string.Empty;
    public string ArriveStationInput { get; set; } = string.Empty;
    public string DepartStation => string.IsNullOrEmpty(DepartStationInput) ? string.Empty : $"{DepartStationInput}站";
    public string ArriveStation => string.IsNullOrEmpty(ArriveStationInput) ? string.Empty : $"{ArriveStationInput}站";

    // 日期时间相关
    public DateTime? DepartDateTime { get; set; } = DateTime.Now;
    public DateTime? DepartTimeValue { get; set; } = DateTime.Now;
    public string DepartDate => DepartDateTime?.ToString("yyyy-MM-dd") ?? string.Empty;
    public string DepartTime => DepartTimeValue?.ToString("HH:mm") ?? string.Empty;

    // 车厢号
    public string CoachNoInput { get; set; } = string.Empty;
    public string CoachNo => string.IsNullOrEmpty(CoachNoInput) ? string.Empty : $"{CoachNoInput}车";

    // 座位号相关
    public string SeatNoNumber { get; set; } = string.Empty;
    public string SelectedSeatLetter { get; set; } = string.Empty;
    public bool IsNoSeat { get; set; }
    public string SeatNo => IsNoSeat ? "无座" : $"{SeatNoNumber}{SelectedSeatLetter}".Trim();

    // 席别
    public string SeatType { get; set; } = "二等座";

    // 金额
    public string MoneyText { get; set; } = string.Empty;
    public decimal Money => decimal.TryParse(MoneyText, out var m) ? m : 0;

    // 附加信息
    public string AdditionalInfo { get; set; } = string.Empty;

    // 车票用途
    public string TicketPurpose { get; set; } = string.Empty;

    // 改签类型
    public string TicketModificationType { get; set; } = string.Empty;

    // 状态
    public string SelectedStatus { get; set; } = "已完成";

    public int StatusValue => SelectedStatus switch
    {
        "未出行" => 0,
        "已完成" => 1,
        "已改签" => 2,
        "已退票" => 3,
        _ => 1
    };

    // 票种类型
    public bool IsStudentTicket { get; set; }
    public bool IsDiscountTicket { get; set; }
    public bool IsOnlineTicket { get; set; }
    public bool IsChildTicket { get; set; }

    public int TicketTypeFlags
    {
        get
        {
            var flags = 0;
            if (IsStudentTicket) flags |= 1;
            if (IsDiscountTicket) flags |= 2;
            if (IsOnlineTicket) flags |= 4;
            if (IsChildTicket) flags |= 8;
            return flags;
        }
        set
        {
            IsStudentTicket = (value & 1) != 0;
            IsDiscountTicket = (value & 2) != 0;
            IsOnlineTicket = (value & 4) != 0;
            IsChildTicket = (value & 8) != 0;
        }
    }

    // 支付渠道
    public bool IsAlipay { get; set; }
    public bool IsWeChat { get; set; }
    public bool IsABC { get; set; }
    public bool IsCCB { get; set; }
    public bool IsICBC { get; set; }
    public bool IsBCOM { get; set; }
    public bool IsCMB { get; set; }
    public bool IsPSBC { get; set; }
    public bool IsBOC { get; set; }

    public int PaymentChannelFlags
    {
        get
        {
            var flags = 0;
            if (IsAlipay) flags |= 1;
            if (IsWeChat) flags |= 2;
            if (IsABC) flags |= 4;
            if (IsCCB) flags |= 8;
            if (IsICBC) flags |= 16;
            if (IsBCOM) flags |= 32;
            if (IsCMB) flags |= 64;
            if (IsPSBC) flags |= 128;
            if (IsBOC) flags |= 256;
            return flags;
        }
        set
        {
            IsAlipay = (value & 1) != 0;
            IsWeChat = (value & 2) != 0;
            IsABC = (value & 4) != 0;
            IsCCB = (value & 8) != 0;
            IsICBC = (value & 16) != 0;
            IsBCOM = (value & 32) != 0;
            IsCMB = (value & 64) != 0;
            IsPSBC = (value & 128) != 0;
            IsBOC = (value & 256) != 0;
        }
    }

    // 提示信息
    public string Hint { get; set; } = string.Empty;
    public string SelectedHint { get; set; } = string.Empty;

    // 其他信息
    public string TicketNumber { get; set; } = string.Empty;
    public string CheckInLocation { get; set; } = string.Empty;
    public string DepartStationCode { get; set; } = string.Empty;
    public string ArriveStationCode { get; set; } = string.Empty;
    public string DepartStationPinyin { get; set; } = string.Empty;
    public string ArriveStationPinyin { get; set; } = string.Empty;
    public ObservableCollection<int> SelectedTagIds { get; set; } = new();

    /// <summary>
    ///     克隆表单数据
    /// </summary>
    public TrainTicketFormData Clone()
    {
        return new TrainTicketFormData
        {
            SelectedTrainNoPrefix = SelectedTrainNoPrefix,
            TrainNoNumber = TrainNoNumber,
            DepartStationInput = DepartStationInput,
            ArriveStationInput = ArriveStationInput,
            DepartDateTime = DepartDateTime,
            DepartTimeValue = DepartTimeValue,
            CoachNoInput = CoachNoInput,
            SeatNoNumber = SeatNoNumber,
            SelectedSeatLetter = SelectedSeatLetter,
            IsNoSeat = IsNoSeat,
            SeatType = SeatType,
            MoneyText = MoneyText,
            AdditionalInfo = AdditionalInfo,
            TicketPurpose = TicketPurpose,
            TicketModificationType = TicketModificationType,
            SelectedStatus = SelectedStatus,
            IsStudentTicket = IsStudentTicket,
            IsDiscountTicket = IsDiscountTicket,
            IsOnlineTicket = IsOnlineTicket,
            IsChildTicket = IsChildTicket,
            IsAlipay = IsAlipay,
            IsWeChat = IsWeChat,
            IsABC = IsABC,
            IsCCB = IsCCB,
            IsICBC = IsICBC,
            IsBCOM = IsBCOM,
            IsCMB = IsCMB,
            IsPSBC = IsPSBC,
            IsBOC = IsBOC,
            Hint = Hint,
            SelectedHint = SelectedHint,
            TicketNumber = TicketNumber,
            CheckInLocation = CheckInLocation,
            DepartStationCode = DepartStationCode,
            ArriveStationCode = ArriveStationCode,
            DepartStationPinyin = DepartStationPinyin,
            ArriveStationPinyin = ArriveStationPinyin,
            SelectedTagIds = new ObservableCollection<int>(SelectedTagIds)
        };
    }

    /// <summary>
    ///     复制数据到目标对象
    /// </summary>
    public void CopyTo(TrainTicketFormData target)
    {
        target.SelectedTrainNoPrefix = SelectedTrainNoPrefix;
        target.TrainNoNumber = TrainNoNumber;
        target.DepartStationInput = DepartStationInput;
        target.ArriveStationInput = ArriveStationInput;
        target.DepartDateTime = DepartDateTime;
        target.DepartTimeValue = DepartTimeValue;
        target.CoachNoInput = CoachNoInput;
        target.SeatNoNumber = SeatNoNumber;
        target.SelectedSeatLetter = SelectedSeatLetter;
        target.IsNoSeat = IsNoSeat;
        target.SeatType = SeatType;
        target.MoneyText = MoneyText;
        target.AdditionalInfo = AdditionalInfo;
        target.TicketPurpose = TicketPurpose;
        target.TicketModificationType = TicketModificationType;
        target.SelectedStatus = SelectedStatus;
        target.IsStudentTicket = IsStudentTicket;
        target.IsDiscountTicket = IsDiscountTicket;
        target.IsOnlineTicket = IsOnlineTicket;
        target.IsChildTicket = IsChildTicket;
        target.IsAlipay = IsAlipay;
        target.IsWeChat = IsWeChat;
        target.IsABC = IsABC;
        target.IsCCB = IsCCB;
        target.IsICBC = IsICBC;
        target.IsBCOM = IsBCOM;
        target.IsCMB = IsCMB;
        target.IsPSBC = IsPSBC;
        target.IsBOC = IsBOC;
        target.Hint = Hint;
        target.SelectedHint = SelectedHint;
        target.TicketNumber = TicketNumber;
        target.CheckInLocation = CheckInLocation;
        target.DepartStationCode = DepartStationCode;
        target.ArriveStationCode = ArriveStationCode;
        target.DepartStationPinyin = DepartStationPinyin;
        target.ArriveStationPinyin = ArriveStationPinyin;
        target.SelectedTagIds = new ObservableCollection<int>(SelectedTagIds);
    }
}