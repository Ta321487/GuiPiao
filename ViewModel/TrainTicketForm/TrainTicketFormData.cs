using System;
using System.Collections.ObjectModel;

namespace GuiPiao.ViewModel.TrainTicketForm
{
    /// <summary>
    /// 火车票表单数据 DTO
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
        public bool IsNoSeat { get; set; } = false;
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
        public bool IsStudentTicket { get; set; } = false;
        public bool IsDiscountTicket { get; set; } = false;
        public bool IsOnlineTicket { get; set; } = false;
        public bool IsChildTicket { get; set; } = false;
        public int TicketTypeFlags
        {
            get
            {
                int flags = 0;
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
        public bool IsAlipay { get; set; } = false;
        public bool IsWeChat { get; set; } = false;
        public bool IsABC { get; set; } = false;
        public bool IsCCB { get; set; } = false;
        public bool IsICBC { get; set; } = false;
        public bool IsBCOM { get; set; } = false;
        public bool IsCMB { get; set; } = false;
        public bool IsPSBC { get; set; } = false;
        public bool IsBOC { get; set; } = false;
        public int PaymentChannelFlags
        {
            get
            {
                int flags = 0;
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
        public ObservableCollection<int> SelectedTagIds { get; set; } = new ObservableCollection<int>();

        /// <summary>
        /// 克隆表单数据
        /// </summary>
        public TrainTicketFormData Clone()
        {
            return new TrainTicketFormData
            {
                SelectedTrainNoPrefix = this.SelectedTrainNoPrefix,
                TrainNoNumber = this.TrainNoNumber,
                DepartStationInput = this.DepartStationInput,
                ArriveStationInput = this.ArriveStationInput,
                DepartDateTime = this.DepartDateTime,
                DepartTimeValue = this.DepartTimeValue,
                CoachNoInput = this.CoachNoInput,
                SeatNoNumber = this.SeatNoNumber,
                SelectedSeatLetter = this.SelectedSeatLetter,
                IsNoSeat = this.IsNoSeat,
                SeatType = this.SeatType,
                MoneyText = this.MoneyText,
                AdditionalInfo = this.AdditionalInfo,
                TicketPurpose = this.TicketPurpose,
                TicketModificationType = this.TicketModificationType,
                SelectedStatus = this.SelectedStatus,
                IsStudentTicket = this.IsStudentTicket,
                IsDiscountTicket = this.IsDiscountTicket,
                IsOnlineTicket = this.IsOnlineTicket,
                IsChildTicket = this.IsChildTicket,
                IsAlipay = this.IsAlipay,
                IsWeChat = this.IsWeChat,
                IsABC = this.IsABC,
                IsCCB = this.IsCCB,
                IsICBC = this.IsICBC,
                IsBCOM = this.IsBCOM,
                IsCMB = this.IsCMB,
                IsPSBC = this.IsPSBC,
                IsBOC = this.IsBOC,
                Hint = this.Hint,
                SelectedHint = this.SelectedHint,
                TicketNumber = this.TicketNumber,
                CheckInLocation = this.CheckInLocation,
                DepartStationCode = this.DepartStationCode,
                ArriveStationCode = this.ArriveStationCode,
                DepartStationPinyin = this.DepartStationPinyin,
                ArriveStationPinyin = this.ArriveStationPinyin,
                SelectedTagIds = new ObservableCollection<int>(this.SelectedTagIds)
            };
        }

        /// <summary>
        /// 复制数据到目标对象
        /// </summary>
        public void CopyTo(TrainTicketFormData target)
        {
            target.SelectedTrainNoPrefix = this.SelectedTrainNoPrefix;
            target.TrainNoNumber = this.TrainNoNumber;
            target.DepartStationInput = this.DepartStationInput;
            target.ArriveStationInput = this.ArriveStationInput;
            target.DepartDateTime = this.DepartDateTime;
            target.DepartTimeValue = this.DepartTimeValue;
            target.CoachNoInput = this.CoachNoInput;
            target.SeatNoNumber = this.SeatNoNumber;
            target.SelectedSeatLetter = this.SelectedSeatLetter;
            target.IsNoSeat = this.IsNoSeat;
            target.SeatType = this.SeatType;
            target.MoneyText = this.MoneyText;
            target.AdditionalInfo = this.AdditionalInfo;
            target.TicketPurpose = this.TicketPurpose;
            target.TicketModificationType = this.TicketModificationType;
            target.SelectedStatus = this.SelectedStatus;
            target.IsStudentTicket = this.IsStudentTicket;
            target.IsDiscountTicket = this.IsDiscountTicket;
            target.IsOnlineTicket = this.IsOnlineTicket;
            target.IsChildTicket = this.IsChildTicket;
            target.IsAlipay = this.IsAlipay;
            target.IsWeChat = this.IsWeChat;
            target.IsABC = this.IsABC;
            target.IsCCB = this.IsCCB;
            target.IsICBC = this.IsICBC;
            target.IsBCOM = this.IsBCOM;
            target.IsCMB = this.IsCMB;
            target.IsPSBC = this.IsPSBC;
            target.IsBOC = this.IsBOC;
            target.Hint = this.Hint;
            target.SelectedHint = this.SelectedHint;
            target.TicketNumber = this.TicketNumber;
            target.CheckInLocation = this.CheckInLocation;
            target.DepartStationCode = this.DepartStationCode;
            target.ArriveStationCode = this.ArriveStationCode;
            target.DepartStationPinyin = this.DepartStationPinyin;
            target.ArriveStationPinyin = this.ArriveStationPinyin;
            target.SelectedTagIds = new ObservableCollection<int>(this.SelectedTagIds);
        }
    }
}
