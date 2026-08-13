using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using GuiPiao.DataAccess;
using GuiPiao.Messages;
using GuiPiao.Model;
using GuiPiao.Services;
using GuiPiao.Utils;
using GuiPiao.View;

namespace GuiPiao.ViewModel.TrainTicketForm;

public abstract partial class TrainTicketFormViewModelBase
{
    #region 绑定属性（代理到FormData）

    // 车次号相关属性
    public ObservableCollection<string> TrainNoPrefixes => _optionsProvider.TrainNoPrefixes;

    [ObservableProperty] private string _selectedTrainNoPrefix;

    [ObservableProperty] private string _trainNoNumber;

    public string TrainNo => _formData.TrainNo;

    // 车站相关属性
    [ObservableProperty] private string _departStationInput;

    [ObservableProperty] private string _arriveStationInput;

    public string DepartStation => _formData.DepartStation;
    public string ArriveStation => _formData.ArriveStation;

    // 日期时间相关属性
    [ObservableProperty] private DateTime? _departDateTime;

    public string DepartDate => _formData.DepartDate;

    [ObservableProperty] private DateTime? _departTimeValue;

    [ObservableProperty] private DateTime? _arriveTimeValue;

    [ObservableProperty] private int _arriveDayOffset;

    public string DepartTime => _formData.DepartTime;

    // 车厢号相关属性
    [ObservableProperty] private string _coachNoInput;

    [ObservableProperty] private bool _isJiaChe;

    public string CoachNo => _formData.CoachNo;

    // 座位号相关属性
    [ObservableProperty] private string _seatNoNumber;

    [ObservableProperty] private ObservableCollection<string> _seatLetterOptions;

    [ObservableProperty] private string _selectedSeatLetter;

    [ObservableProperty] private bool _isNoSeat;

    [ObservableProperty] private bool _isSeatNoInputEnabled = true;

    [ObservableProperty] private bool _isSeatLetterEnabled = true;

    [ObservableProperty] private bool _isSeatLetterVisible = true;

    public string SeatNo => _formData.SeatNo;

    // 席别相关属性
    public ObservableCollection<string> SeatTypeOptions => _optionsProvider.SeatTypeOptions;

    [ObservableProperty] private string _seatType;

    // 金额相关属性
    [ObservableProperty] private string _moneyText;

    public decimal Money => _formData.Money;

    // 附加信息相关属性
    public ObservableCollection<string> AdditionalInfoOptions => _optionsProvider.AdditionalInfoOptions;

    [ObservableProperty] private string _additionalInfo;

    // 车票用途相关属性
    public ObservableCollection<string> TicketPurposeOptions => _optionsProvider.TicketPurposeOptions;

    [ObservableProperty] private string _ticketPurpose;

    // 改签类型相关属性
    public ObservableCollection<string> TicketModificationTypeOptions => _optionsProvider.TicketModificationTypeOptions;

    [ObservableProperty] private string _ticketModificationType;

    // 状态相关属性（仅新增窗口）
    [ObservableProperty] private bool _isStatusVisible;

    public ObservableCollection<string> StatusOptions => _optionsProvider.StatusOptions;

    public ObservableCollection<string> ArriveDayOffsetOptions { get; } =
        new(ArriveTimeFormat.DayOffsetLabels);

    [ObservableProperty] private string _selectedStatus;

    public int StatusValue => _formData.StatusValue;

    // 票种类型相关属性
    [ObservableProperty] private bool _isStudentTicket;

    [ObservableProperty] private bool _isDiscountTicket;

    [ObservableProperty] private bool _isOnlineTicket;

    [ObservableProperty] private bool _isChildTicket;

    public int TicketTypeFlags
    {
        get => _formData.TicketTypeFlags;
        set => _formData.TicketTypeFlags = value;
    }

    // 支付渠道相关属性
    [ObservableProperty] private bool _isAlipay;

    [ObservableProperty] private bool _isWeChat;

    [ObservableProperty] private bool _isABC;

    [ObservableProperty] private bool _isCCB;

    [ObservableProperty] private bool _isICBC;

    [ObservableProperty] private bool _isBCOM;

    [ObservableProperty] private bool _isCMB;

    [ObservableProperty] private bool _isPSBC;

    [ObservableProperty] private bool _isBOC;

    public int PaymentChannelFlags
    {
        get => _formData.PaymentChannelFlags;
        set => _formData.PaymentChannelFlags = value;
    }

    // 提示信息相关属性
    public ObservableCollection<string> HintOptions => _optionsProvider.HintOptions;

    [ObservableProperty] private string _selectedHint;

    [ObservableProperty] private string _hint;

    // 其他属性
    [ObservableProperty] private string _ticketNumber;

    [ObservableProperty] private string _checkInLocation;

    // OCR/导入：浅黄高亮（已填入或需核对）
    [ObservableProperty] private bool _highlightTrainNo;
    [ObservableProperty] private bool _highlightDepartStation;
    [ObservableProperty] private bool _highlightArriveStation;
    [ObservableProperty] private bool _highlightDepartDate;
    [ObservableProperty] private bool _highlightDepartTime;
    [ObservableProperty] private bool _highlightArriveTime;
    [ObservableProperty] private bool _highlightCoachNo;
    [ObservableProperty] private bool _highlightSeatNo;
    [ObservableProperty] private bool _highlightSeatType;
    [ObservableProperty] private bool _highlightMoney;
    [ObservableProperty] private bool _highlightCheckIn;
    [ObservableProperty] private bool _highlightTicketNumber;

    [ObservableProperty] private string _departStationCode;

    [ObservableProperty] private string _arriveStationCode;

    [ObservableProperty] private string _departStationPinyin;

    [ObservableProperty] private string _arriveStationPinyin;

    [ObservableProperty] private ObservableCollection<string> _stationNames;

    // 车站联想相关属性
    [ObservableProperty] private ObservableCollection<string> _departStationSuggestions = new();

    [ObservableProperty] private ObservableCollection<string> _arriveStationSuggestions = new();

    [ObservableProperty] private bool _isDepartStationDropdownOpen;

    [ObservableProperty] private bool _isArriveStationDropdownOpen;

    [ObservableProperty] private int _departStationSelectedIndex = -1;

    [ObservableProperty] private int _arriveStationSelectedIndex = -1;

    [ObservableProperty] private ObservableCollection<TicketTag> _availableTags;

    [ObservableProperty] private ObservableCollection<int> _selectedTagIds;

    [ObservableProperty] private string _windowTitle = "火车票";

    [ObservableProperty] private string _saveButtonText = "保存";

    [ObservableProperty] private bool _isEditMode;

    [ObservableProperty] private int? _editTicketId;

    [ObservableProperty] private bool _hasUnsavedChanges;

    [ObservableProperty] private bool _canUndo;

    [ObservableProperty] private bool _canRedo;

    [ObservableProperty] private ObservableCollection<OperationHistoryItem> _operationHistory = new();

    [ObservableProperty] private bool _isOperationHistoryExpanded;

    // 改签相关属性
    [ObservableProperty] private bool _isRescheduleMode;

    [ObservableProperty] private int _originalTicketId;

    [ObservableProperty] private string _originalTicketStatus = string.Empty;

    [ObservableProperty] private bool _isDepartStationReadOnly;

    [ObservableProperty] private bool _isArriveStationReadOnly;

    [ObservableProperty] private bool _isRescheduleTypeChangeDestination;

    #endregion
}
