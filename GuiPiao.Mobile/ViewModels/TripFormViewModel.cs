using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using GuiPiao.Model;
using GuiPiao.Mobile.Data;
using GuiPiao.Mobile.Messaging;
using GuiPiao.Mobile.Model;
using GuiPiao.Mobile.Services;

namespace GuiPiao.Mobile.ViewModels;

/// <summary>手机行程表单：字段与选项对齐 PC TrainTicketForm。</summary>
public partial class TripFormViewModel : ObservableObject, IQueryAttributable
{
    public static IReadOnlyList<string> TrainNoPrefixes { get; } =
        ["G", "C", "D", "Z", "T", "K", "L", "S", "纯数字"];

    public static IReadOnlyList<string> SeatTypeOptions { get; } =
    [
        "新空调硬座", "软座", "新空调硬卧", "新空调软卧", "商务座", "特等座", "一等座", "二等座", "硬卧代硬座"
    ];

    public static IReadOnlyList<string> StatusOptions { get; } =
        ["未出行", "已完成", "已改签", "已退票"];

    public static IReadOnlyList<string> ArriveDayOffsetOptions { get; } =
        ["当日", "次日", "第三天"];

    public static IReadOnlyList<string> AdditionalInfoOptions { get; } =
        ["", "限乘当日当次车", "退票费"];

    public static IReadOnlyList<string> TicketPurposeOptions { get; } =
        ["", "仅供报销使用"];

    public static IReadOnlyList<string> TicketModificationTypeOptions { get; } =
        ["", "始发改签", "变更到站"];

    public static IReadOnlyList<string> HintOptions { get; } =
    [
        "",
        "报销凭证 遗失不补|退票改签时须交回车站",
        "买票请到12306发货请到95306|中国铁路祝您旅途愉快",
        "欢度国庆 祝福祖国|中国铁路祝您旅途愉快",
        "奋斗百年路启航新征程|热烈庆祝中国共产党成立100周年",
        "锦州银行欢迎您",
        "中国铁路沈阳局集团公司|团体订票电话024-12306",
        "自定义"
    ];

    private readonly RideRepository _rides;
    private readonly TagRepository _tags;
    private readonly StationCacheRepository _stations;
    private readonly RideWriteService _write;
    private readonly CapturePrefillStore _prefill;

    private string _mode = string.Empty;
    private string _fromSyncId = string.Empty;
    private bool _isEdit;
    private bool _wantPrefill;
    private bool _suppressSeatLetterRefresh;
    private bool _suppressStationLookup;
    private bool _suppressPaymentMutex;
    private bool _suppressTicketMutex;

    [ObservableProperty] private string _syncId = string.Empty;
    [ObservableProperty] private string _pageTitle = "新增行程";
    [ObservableProperty] private string _errorText = string.Empty;
    [ObservableProperty] private string _importBanner = string.Empty;
    [ObservableProperty] private bool _isBusy;

    [ObservableProperty] private string _selectedTrainNoPrefix = "G";
    [ObservableProperty] private string _trainNoNumber = string.Empty;
    [ObservableProperty] private string _departStation = string.Empty;
    [ObservableProperty] private string _arriveStation = string.Empty;
    [ObservableProperty] private DateTime _departDateValue = DateTime.Today;
    [ObservableProperty] private TimeSpan _departTimeValue = new(12, 0, 0);
    [ObservableProperty] private bool _hasArriveTime;
    [ObservableProperty] private TimeSpan _arriveTimeValue = new(14, 0, 0);
    [ObservableProperty] private int _arriveDayOffsetIndex;
    [ObservableProperty] private string _seatType = "二等座";
    [ObservableProperty] private bool _isJiaChe;
    [ObservableProperty] private string _coachNoInput = string.Empty;
    [ObservableProperty] private string _seatNoNumber = string.Empty;
    [ObservableProperty] private string _selectedSeatLetter = "A";
    [ObservableProperty] private bool _isNoSeat;
    [ObservableProperty] private ObservableCollection<string> _seatLetterOptions = new();
    [ObservableProperty] private string _moneyText = "0.00";
    [ObservableProperty] private string _selectedStatus = "未出行";

    [ObservableProperty] private string _ticketNumber = string.Empty;
    [ObservableProperty] private string _checkInLocation = string.Empty;
    [ObservableProperty] private string _additionalInfo = string.Empty;
    [ObservableProperty] private string _ticketPurpose = string.Empty;
    [ObservableProperty] private string _ticketModificationType = string.Empty;
    [ObservableProperty] private string _selectedHint = string.Empty;
    [ObservableProperty] private string _customHint = string.Empty;
    [ObservableProperty] private bool _isCustomHint;

    [ObservableProperty] private bool _isStudentTicket;
    [ObservableProperty] private bool _isDiscountTicket;
    [ObservableProperty] private bool _isOnlineTicket;
    [ObservableProperty] private bool _isChildTicket;

    [ObservableProperty] private bool _isAlipay;
    [ObservableProperty] private bool _isWeChat;
    [ObservableProperty] private bool _isABC;
    [ObservableProperty] private bool _isCCB;
    [ObservableProperty] private bool _isICBC;
    [ObservableProperty] private bool _isBCOM;
    [ObservableProperty] private bool _isCMB;
    [ObservableProperty] private bool _isPSBC;
    [ObservableProperty] private bool _isBOC;

    [ObservableProperty] private string _departStationCode = string.Empty;
    [ObservableProperty] private string _arriveStationCode = string.Empty;
    [ObservableProperty] private string _departStationPinyin = string.Empty;
    [ObservableProperty] private string _arriveStationPinyin = string.Empty;

    [ObservableProperty] private ObservableCollection<StationCacheItem> _departSuggestions = new();
    [ObservableProperty] private ObservableCollection<StationCacheItem> _arriveSuggestions = new();
    [ObservableProperty] private bool _showDepartSuggestions;
    [ObservableProperty] private bool _showArriveSuggestions;
    [ObservableProperty] private ObservableCollection<SelectableTagItem> _availableTags = new();

    [ObservableProperty] private bool _highlightTrainNo;
    [ObservableProperty] private bool _highlightDepartStation;
    [ObservableProperty] private bool _highlightArriveStation;
    [ObservableProperty] private bool _highlightDepartDate;
    [ObservableProperty] private bool _highlightDepartTime;
    [ObservableProperty] private bool _highlightSeatType;
    [ObservableProperty] private bool _highlightCoachNo;
    [ObservableProperty] private bool _highlightSeatNo;
    [ObservableProperty] private bool _highlightMoney;
    [ObservableProperty] private bool _highlightTicketNumber;
    [ObservableProperty] private bool _highlightCheckIn;

    public bool IsSeatInputEnabled => !IsNoSeat;
    public bool ShowSeatLetter => !IsNoSeat && SeatLetterOptions.Count > 0;

    public DateTime DepartDateMin { get; } = new(1990, 1, 1);
    public DateTime DepartDateMax => DateTime.Today.AddYears(2);

    public TripFormViewModel(
        RideRepository rides,
        TagRepository tags,
        StationCacheRepository stations,
        RideWriteService write,
        CapturePrefillStore prefill)
    {
        _rides = rides;
        _tags = tags;
        _stations = stations;
        _write = write;
        _prefill = prefill;
        RefreshSeatLetters();
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        SyncId = ReadQuery(query, "syncId");
        _mode = ReadQuery(query, "mode");
        _fromSyncId = ReadQuery(query, "fromSyncId");
        _wantPrefill = string.Equals(ReadQuery(query, "prefill"), "1", StringComparison.Ordinal);
        Load();
    }

    public void OnAppearing()
    {
        if (_wantPrefill)
            ApplyPrefillIfAny();
        ReloadTags();
    }

    partial void OnSeatTypeChanged(string value) => RefreshSeatLetters();
    partial void OnIsNoSeatChanged(bool value)
    {
        OnPropertyChanged(nameof(IsSeatInputEnabled));
        OnPropertyChanged(nameof(ShowSeatLetter));
    }

    partial void OnSelectedHintChanged(string value)
    {
        IsCustomHint = string.Equals(value, "自定义", StringComparison.Ordinal);
        if (!IsCustomHint)
            CustomHint = string.Empty;
    }

    partial void OnDepartStationChanged(string value)
    {
        if (_suppressStationLookup) return;
        UpdateStationSuggestions(isDepart: true, value);
        TryLookupStation(isDepart: true, value);
    }

    partial void OnArriveStationChanged(string value)
    {
        if (_suppressStationLookup) return;
        UpdateStationSuggestions(isDepart: false, value);
        TryLookupStation(isDepart: false, value);
    }

    partial void OnIsStudentTicketChanged(bool value)
    {
        if (_suppressTicketMutex || !value) return;
        _suppressTicketMutex = true;
        IsChildTicket = false;
        _suppressTicketMutex = false;
    }

    partial void OnIsChildTicketChanged(bool value)
    {
        if (_suppressTicketMutex || !value) return;
        _suppressTicketMutex = true;
        IsStudentTicket = false;
        _suppressTicketMutex = false;
    }

    partial void OnIsAlipayChanged(bool value)
    {
        if (_suppressPaymentMutex || !value) return;
        _suppressPaymentMutex = true;
        IsWeChat = false;
        _suppressPaymentMutex = false;
    }

    partial void OnIsWeChatChanged(bool value)
    {
        if (_suppressPaymentMutex || !value) return;
        _suppressPaymentMutex = true;
        IsAlipay = false;
        _suppressPaymentMutex = false;
    }

    partial void OnIsABCChanged(bool value) { if (value) ClearOtherBanks(nameof(IsABC)); }
    partial void OnIsCCBChanged(bool value) { if (value) ClearOtherBanks(nameof(IsCCB)); }
    partial void OnIsICBCChanged(bool value) { if (value) ClearOtherBanks(nameof(IsICBC)); }
    partial void OnIsBCOMChanged(bool value) { if (value) ClearOtherBanks(nameof(IsBCOM)); }
    partial void OnIsCMBChanged(bool value) { if (value) ClearOtherBanks(nameof(IsCMB)); }
    partial void OnIsPSBCChanged(bool value) { if (value) ClearOtherBanks(nameof(IsPSBC)); }
    partial void OnIsBOCChanged(bool value) { if (value) ClearOtherBanks(nameof(IsBOC)); }

    private void ClearOtherBanks(string keep)
    {
        if (_suppressPaymentMutex) return;
        _suppressPaymentMutex = true;
        if (keep != nameof(IsABC)) IsABC = false;
        if (keep != nameof(IsCCB)) IsCCB = false;
        if (keep != nameof(IsICBC)) IsICBC = false;
        if (keep != nameof(IsBCOM)) IsBCOM = false;
        if (keep != nameof(IsCMB)) IsCMB = false;
        if (keep != nameof(IsPSBC)) IsPSBC = false;
        if (keep != nameof(IsBOC)) IsBOC = false;
        _suppressPaymentMutex = false;
    }

    private void RefreshSeatLetters()
    {
        if (_suppressSeatLetterRefresh) return;
        var previous = SelectedSeatLetter;
        SeatLetterOptions = new ObservableCollection<string>(GetSeatLettersFor(SeatType));
        if (SeatLetterOptions.Contains(previous))
            SelectedSeatLetter = previous;
        else if (SeatLetterOptions.Count > 0)
            SelectedSeatLetter = SeatLetterOptions[0];
        else
            SelectedSeatLetter = string.Empty;
        OnPropertyChanged(nameof(ShowSeatLetter));
    }

    private void Load()
    {
        ClearHighlights();
        ImportBanner = string.Empty;
        _isEdit = !string.IsNullOrWhiteSpace(SyncId) &&
                  !string.Equals(_mode, "reschedule", StringComparison.OrdinalIgnoreCase);
        ReloadTags();

        if (_isEdit)
        {
            var ride = _rides.GetBySyncId(SyncId);
            if (ride == null) return;
            PageTitle = "编辑行程";
            FillFrom(ride);
            SelectTagsForRide(SyncId);
            return;
        }

        if (string.Equals(_mode, "reschedule", StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(_fromSyncId))
        {
            var from = _rides.GetBySyncId(_fromSyncId);
            PageTitle = "改签新票";
            SyncId = string.Empty;
            if (from != null)
            {
                FillFrom(from);
                SelectedStatus = "未出行";
                MoneyText = "0.00";
                TicketModificationType = "始发改签";
            }

            return;
        }

        PageTitle = "新增行程";
        ResetDefaults();
        if (_wantPrefill)
            ApplyPrefillIfAny();
    }

    private void ReloadTags()
    {
        var selected = AvailableTags.Where(t => t.IsSelected).Select(t => t.SyncId).ToHashSet(StringComparer.Ordinal);
        AvailableTags = new ObservableCollection<SelectableTagItem>(
            _tags.ListActive().Select(t => new SelectableTagItem
            {
                SyncId = t.SyncId,
                Name = t.Name,
                Color = string.IsNullOrWhiteSpace(t.Color) ? "#0078D4" : t.Color,
                TextColor = string.IsNullOrWhiteSpace(t.TextColor) ? "#FFFFFF" : t.TextColor,
                IsSelected = selected.Contains(t.SyncId)
            }));
    }

    private void SelectTagsForRide(string rideSyncId)
    {
        var ids = _tags.GetTagSyncIdsForRide(rideSyncId).ToHashSet(StringComparer.Ordinal);
        foreach (var tag in AvailableTags)
            tag.IsSelected = ids.Contains(tag.SyncId);
    }

    private void ResetDefaults()
    {
        _suppressStationLookup = true;
        _suppressPaymentMutex = true;
        _suppressTicketMutex = true;
        SelectedTrainNoPrefix = "G";
        TrainNoNumber = string.Empty;
        DepartStation = string.Empty;
        ArriveStation = string.Empty;
        DepartDateValue = DateTime.Today;
        DepartTimeValue = new TimeSpan(12, 0, 0);
        HasArriveTime = false;
        ArriveTimeValue = new TimeSpan(14, 0, 0);
        ArriveDayOffsetIndex = 0;
        SeatType = "二等座";
        IsJiaChe = false;
        CoachNoInput = string.Empty;
        SeatNoNumber = string.Empty;
        IsNoSeat = false;
        MoneyText = "0.00";
        SelectedStatus = "未出行";
        TicketNumber = string.Empty;
        CheckInLocation = string.Empty;
        AdditionalInfo = string.Empty;
        TicketPurpose = string.Empty;
        TicketModificationType = string.Empty;
        SelectedHint = string.Empty;
        CustomHint = string.Empty;
        IsStudentTicket = IsDiscountTicket = IsOnlineTicket = IsChildTicket = false;
        IsAlipay = IsWeChat = IsABC = IsCCB = IsICBC = IsBCOM = IsCMB = IsPSBC = IsBOC = false;
        DepartStationCode = ArriveStationCode = DepartStationPinyin = ArriveStationPinyin = string.Empty;
        ShowDepartSuggestions = ShowArriveSuggestions = false;
        foreach (var t in AvailableTags) t.IsSelected = false;
        _suppressStationLookup = false;
        _suppressPaymentMutex = false;
        _suppressTicketMutex = false;
        RefreshSeatLetters();
    }

    private void ApplyPrefillIfAny()
    {
        var draft = _prefill.Take();
        _wantPrefill = false;
        if (draft == null) return;

        PageTitle = "确认导入";
        ClearHighlights();
        if (!string.IsNullOrWhiteSpace(draft.TrainNo))
        {
            ParseTrainNo(draft.TrainNo);
            HighlightTrainNo = true;
        }

        if (!string.IsNullOrWhiteSpace(draft.DepartStation))
        {
            DepartStation = StationFormRules.ToNameBody(draft.DepartStation);
            HighlightDepartStation = true;
        }

        if (!string.IsNullOrWhiteSpace(draft.ArriveStation))
        {
            ArriveStation = StationFormRules.ToNameBody(draft.ArriveStation);
            HighlightArriveStation = true;
        }

        if (!string.IsNullOrWhiteSpace(draft.DepartDate) &&
            DateTime.TryParse(draft.DepartDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
        {
            DepartDateValue = d.Date;
            HighlightDepartDate = true;
        }

        if (!string.IsNullOrWhiteSpace(draft.DepartTime) && TryParseTime(draft.DepartTime, out var dt))
        {
            DepartTimeValue = dt;
            HighlightDepartTime = true;
        }

        if (!string.IsNullOrWhiteSpace(draft.ArriveTime) && TryParseTime(draft.ArriveTime, out var at))
        {
            HasArriveTime = true;
            ArriveTimeValue = at;
        }

        if (!string.IsNullOrWhiteSpace(draft.SeatType) && SeatTypeOptions.Contains(draft.SeatType))
        {
            SeatType = draft.SeatType!;
            HighlightSeatType = true;
        }

        IsJiaChe = draft.IsJiaChe;
        if (!string.IsNullOrWhiteSpace(draft.CoachNo))
        {
            CoachNoInput = draft.CoachNo!;
            HighlightCoachNo = true;
        }

        IsNoSeat = draft.IsNoSeat;
        if (!IsNoSeat && !string.IsNullOrWhiteSpace(draft.SeatNo))
        {
            ParseSeatNo(draft.SeatNo!);
            HighlightSeatNo = true;
        }
        else if (IsNoSeat) HighlightSeatNo = true;

        if (!string.IsNullOrWhiteSpace(draft.MoneyText))
        {
            MoneyText = draft.MoneyText!;
            HighlightMoney = true;
        }

        if (!string.IsNullOrWhiteSpace(draft.TicketNumber))
        {
            TicketNumber = draft.TicketNumber!;
            HighlightTicketNumber = true;
        }

        if (!string.IsNullOrWhiteSpace(draft.CheckInLocation))
        {
            CheckInLocation = draft.CheckInLocation!;
            HighlightCheckIn = true;
        }

        SelectedStatus = "未出行";
        var review = draft.FieldsNeedingReview.Count > 0
            ? "待核对：" + string.Join("、", draft.FieldsNeedingReview.Take(8))
            : "请确认后保存入库";
        ImportBanner = $"导入来源：{draft.SourceHint} · {review}";
    }

    private void FillFrom(MobileRide ride)
    {
        _suppressSeatLetterRefresh = true;
        _suppressStationLookup = true;
        _suppressPaymentMutex = true;
        _suppressTicketMutex = true;
        try
        {
            ParseTrainNo(ride.TrainNo);
            DepartStation = StationFormRules.ToNameBody(ride.DepartStation);
            ArriveStation = StationFormRules.ToNameBody(ride.ArriveStation);
            if (DateTime.TryParse(ride.DepartDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
                DepartDateValue = d.Date;
            DepartTimeValue = TryParseTime(ride.DepartTime, out var dt) ? dt : new TimeSpan(12, 0, 0);
            if (!string.IsNullOrWhiteSpace(ride.ArriveTime) && TryParseTime(ride.ArriveTime, out var at))
            {
                HasArriveTime = true;
                ArriveTimeValue = at;
            }
            else HasArriveTime = false;

            ArriveDayOffsetIndex = Math.Clamp(ride.ArriveDayOffset, 0, 2);
            SeatType = SeatTypeOptions.Contains(ride.SeatType) ? ride.SeatType : "二等座";
            ParseCoachNo(ride.CoachNo);
            if (string.Equals(ride.SeatNo, "无座", StringComparison.Ordinal))
            {
                IsNoSeat = true;
                SeatNoNumber = string.Empty;
            }
            else
            {
                IsNoSeat = false;
                ParseSeatNo(ride.SeatNo);
            }

            MoneyText = ride.Money.ToString("0.00");
            SelectedStatus = ride.Status switch
            {
                0 => "未出行",
                1 => "已完成",
                2 => "已改签",
                3 => "已退票",
                _ => "未出行"
            };
            TicketNumber = ride.TicketNumber;
            CheckInLocation = ride.CheckInLocation;
            AdditionalInfo = ride.AdditionalInfo ?? "";
            TicketPurpose = ride.TicketPurpose ?? "";
            TicketModificationType = ride.TicketModificationType ?? "";
            ApplyHintFromStored(ride.Hint);
            ApplyTicketFlags(ride.TicketTypeFlags);
            ApplyPaymentFlags(ride.PaymentChannelFlags);
            DepartStationCode = ride.DepartStationCode;
            ArriveStationCode = ride.ArriveStationCode;
            DepartStationPinyin = ride.DepartStationPinyin;
            ArriveStationPinyin = ride.ArriveStationPinyin;
        }
        finally
        {
            _suppressSeatLetterRefresh = false;
            _suppressStationLookup = false;
            _suppressPaymentMutex = false;
            _suppressTicketMutex = false;
            RefreshSeatLetters();
        }
    }

    private void ApplyHintFromStored(string? hint)
    {
        var h = hint ?? "";
        if (string.IsNullOrWhiteSpace(h))
        {
            SelectedHint = "";
            return;
        }

        if (HintOptions.Contains(h))
        {
            SelectedHint = h;
            return;
        }

        SelectedHint = "自定义";
        CustomHint = h;
    }

    private void ApplyTicketFlags(int flags)
    {
        IsStudentTicket = (flags & 1) != 0;
        IsDiscountTicket = (flags & 2) != 0;
        IsOnlineTicket = (flags & 4) != 0;
        IsChildTicket = (flags & 8) != 0;
    }

    private void ApplyPaymentFlags(int flags)
    {
        IsAlipay = (flags & 1) != 0;
        IsWeChat = (flags & 2) != 0;
        IsABC = (flags & 4) != 0;
        IsCCB = (flags & 8) != 0;
        IsICBC = (flags & 16) != 0;
        IsBCOM = (flags & 32) != 0;
        IsCMB = (flags & 64) != 0;
        IsPSBC = (flags & 128) != 0;
        IsBOC = (flags & 256) != 0;
    }

    private int BuildTicketFlags()
    {
        var f = 0;
        if (IsStudentTicket) f |= 1;
        if (IsDiscountTicket) f |= 2;
        if (IsOnlineTicket) f |= 4;
        if (IsChildTicket) f |= 8;
        return f;
    }

    private int BuildPaymentFlags()
    {
        var f = 0;
        if (IsAlipay) f |= 1;
        if (IsWeChat) f |= 2;
        if (IsABC) f |= 4;
        if (IsCCB) f |= 8;
        if (IsICBC) f |= 16;
        if (IsBCOM) f |= 32;
        if (IsCMB) f |= 64;
        if (IsPSBC) f |= 128;
        if (IsBOC) f |= 256;
        return f;
    }

    [RelayCommand]
    private void PickDepartStation(StationCacheItem? item)
    {
        if (item == null) return;
        _suppressStationLookup = true;
        DepartStation = item.DisplayName;
        DepartStationCode = item.StationCode;
        DepartStationPinyin = item.StationPinyin;
        ShowDepartSuggestions = false;
        _suppressStationLookup = false;
    }

    [RelayCommand]
    private void PickArriveStation(StationCacheItem? item)
    {
        if (item == null) return;
        _suppressStationLookup = true;
        ArriveStation = item.DisplayName;
        ArriveStationCode = item.StationCode;
        ArriveStationPinyin = item.StationPinyin;
        ShowArriveSuggestions = false;
        _suppressStationLookup = false;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (IsBusy) return;
        ErrorText = string.Empty;
        if (!TryValidate(out var money, out var err))
        {
            ErrorText = err;
            return;
        }

        IsBusy = true;
        try
        {
            MobileRide ride;
            if (_isEdit)
                ride = _rides.GetBySyncId(SyncId) ?? new MobileRide { SyncId = SyncId };
            else
            {
                ride = new MobileRide();
                if (string.Equals(_mode, "reschedule", StringComparison.OrdinalIgnoreCase) &&
                    string.IsNullOrWhiteSpace(TicketModificationType))
                    TicketModificationType = "始发改签";
            }

            ride.TrainNo = BuildTrainNo();
            ride.DepartStation = StationFormRules.ToStoredName(DepartStation);
            ride.ArriveStation = StationFormRules.ToStoredName(ArriveStation);
            ride.DepartDate = DepartDateValue.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            ride.DepartTime = FormatTime(DepartTimeValue);
            ride.ArriveTime = HasArriveTime ? FormatTime(ArriveTimeValue) : string.Empty;
            ride.ArriveDayOffset = HasArriveTime ? ArriveDayOffsetIndex : 0;
            ride.SeatType = SeatType;
            ride.CoachNo = FormatCoachNo();
            ride.SeatNo = FormatSeatNo();
            ride.Money = money;
            ride.Status = SelectedStatus switch
            {
                "未出行" => 0,
                "已完成" => 1,
                "已改签" => 2,
                "已退票" => 3,
                _ => 0
            };
            ride.TicketNumber = TicketNumber.Trim();
            ride.CheckInLocation = CheckInLocation.Trim();
            ride.AdditionalInfo = AdditionalInfo ?? "";
            ride.TicketPurpose = TicketPurpose ?? "";
            ride.TicketModificationType = TicketModificationType ?? "";
            ride.Hint = IsCustomHint ? CustomHint.Trim() : (SelectedHint ?? "");
            ride.TicketTypeFlags = BuildTicketFlags();
            ride.PaymentChannelFlags = BuildPaymentFlags();
            ride.DepartStationCode = DepartStationCode.Trim();
            ride.ArriveStationCode = ArriveStationCode.Trim();
            ride.DepartStationPinyin = DepartStationPinyin.Trim();
            ride.ArriveStationPinyin = ArriveStationPinyin.Trim();

            var tagIds = AvailableTags.Where(t => t.IsSelected).Select(t => t.SyncId).ToList();
            ride = _write.SaveUpsert(ride, tagIds);
            WeakReferenceMessenger.Default.Send(new TripsDataChangedMessage());

            await Shell.Current.GoToAsync("..");
            if (!_isEdit)
                await Shell.Current.GoToAsync($"tripdetail?syncId={Uri.EscapeDataString(ride.SyncId)}");
        }
        catch (Exception ex)
        {
            ErrorText = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task CancelAsync() => await Shell.Current.GoToAsync("..");

    private void UpdateStationSuggestions(bool isDepart, string value)
    {
        var list = _stations.Search(value);
        if (isDepart)
        {
            DepartSuggestions = new ObservableCollection<StationCacheItem>(list);
            ShowDepartSuggestions = list.Count > 0;
        }
        else
        {
            ArriveSuggestions = new ObservableCollection<StationCacheItem>(list);
            ShowArriveSuggestions = list.Count > 0;
        }
    }

    private void TryLookupStation(bool isDepart, string value)
    {
        var hit = _stations.FindExact(value);
        if (hit == null) return;
        if (isDepart)
        {
            if (string.IsNullOrWhiteSpace(DepartStationCode))
                DepartStationCode = hit.StationCode;
            if (string.IsNullOrWhiteSpace(DepartStationPinyin))
                DepartStationPinyin = hit.StationPinyin;
        }
        else
        {
            if (string.IsNullOrWhiteSpace(ArriveStationCode))
                ArriveStationCode = hit.StationCode;
            if (string.IsNullOrWhiteSpace(ArriveStationPinyin))
                ArriveStationPinyin = hit.StationPinyin;
        }
    }

    private bool TryValidate(out decimal money, out string error)
    {
        money = 0;
        if (string.IsNullOrWhiteSpace(TrainNoNumber) ||
            !Regex.IsMatch(TrainNoNumber.Trim(), @"^\d{1,4}$"))
        {
            error = "请填写有效车次数字（1–4 位）。";
            return false;
        }

        if (string.IsNullOrWhiteSpace(DepartStation) || string.IsNullOrWhiteSpace(ArriveStation))
        {
            error = "请填写出发站与到达站。";
            return false;
        }

        if (string.IsNullOrWhiteSpace(SeatType))
        {
            error = "请选择席别。";
            return false;
        }

        if (string.IsNullOrWhiteSpace(CoachNoInput) || !int.TryParse(CoachNoInput.Trim(), out var coach) ||
            coach <= 0)
        {
            error = "请填写有效车厢号。";
            return false;
        }

        if (!IsNoSeat)
        {
            if (string.IsNullOrWhiteSpace(SeatNoNumber) || !int.TryParse(SeatNoNumber.Trim(), out _))
            {
                error = "请填写座位号，或勾选「无座」。";
                return false;
            }

            if (ShowSeatLetter && string.IsNullOrWhiteSpace(SelectedSeatLetter))
            {
                error = "请选择座位字母/铺位。";
                return false;
            }
        }

        if ((!decimal.TryParse(MoneyText.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out money) &&
             !decimal.TryParse(MoneyText.Trim(), out money)) || money < 0)
        {
            error = "金额格式无效。";
            return false;
        }

        if (IsCustomHint && string.IsNullOrWhiteSpace(CustomHint))
        {
            error = "已选自定义提示，请填写提示内容。";
            return false;
        }

        error = string.Empty;
        return true;
    }

    private string BuildTrainNo()
    {
        var num = TrainNoNumber.Trim();
        return string.Equals(SelectedTrainNoPrefix, "纯数字", StringComparison.Ordinal)
            ? num
            : SelectedTrainNoPrefix + num;
    }

    private string FormatCoachNo()
    {
        var body = int.TryParse(CoachNoInput.Trim(), out var n) ? n.ToString("D2") : CoachNoInput.Trim();
        return IsJiaChe ? $"加{body}车" : $"{body}车";
    }

    private string FormatSeatNo()
    {
        if (IsNoSeat) return "无座";
        if (!int.TryParse(SeatNoNumber.Trim(), out var seatNo))
            return SeatNoNumber.Trim() + (SelectedSeatLetter ?? "");
        var pad = SeatType is "二等座" or "一等座" or "商务座" or "特等座" or "硬卧代硬座" ? 2 : 3;
        return seatNo.ToString("D" + pad) + (SelectedSeatLetter ?? "");
    }

    private void ParseTrainNo(string trainNo)
    {
        var t = (trainNo ?? "").Trim().ToUpperInvariant();
        var m = Regex.Match(t, @"^([GDCKTZSYL])(\d{1,4})$", RegexOptions.IgnoreCase);
        if (m.Success)
        {
            SelectedTrainNoPrefix = m.Groups[1].Value.ToUpperInvariant();
            TrainNoNumber = m.Groups[2].Value;
            return;
        }

        if (Regex.IsMatch(t, @"^\d{1,4}$"))
        {
            SelectedTrainNoPrefix = "纯数字";
            TrainNoNumber = t;
            return;
        }

        SelectedTrainNoPrefix = "G";
        TrainNoNumber = t;
    }

    private void ParseCoachNo(string coachNo)
    {
        var s = (coachNo ?? "").Trim();
        var isJia = s.Contains('加', StringComparison.Ordinal);
        s = s.Replace("加挂", "", StringComparison.Ordinal)
            .Replace("加", "", StringComparison.Ordinal)
            .Replace("车", "", StringComparison.Ordinal)
            .Trim();
        IsJiaChe = isJia;
        CoachNoInput = s;
    }

    private void ParseSeatNo(string seatNo)
    {
        var s = (seatNo ?? "").Trim();
        var m = Regex.Match(s, @"^(\d+)(.*)$");
        if (m.Success)
        {
            SeatNoNumber = m.Groups[1].Value;
            var letter = m.Groups[2].Value.Trim();
            RefreshSeatLetters();
            if (!string.IsNullOrEmpty(letter))
                SelectedSeatLetter = letter;
        }
        else SeatNoNumber = s;
    }

    private void ClearHighlights()
    {
        HighlightTrainNo = HighlightDepartStation = HighlightArriveStation = false;
        HighlightDepartDate = HighlightDepartTime = HighlightSeatType = false;
        HighlightCoachNo = HighlightSeatNo = HighlightMoney = false;
        HighlightTicketNumber = HighlightCheckIn = false;
    }

    private static IEnumerable<string> GetSeatLettersFor(string seatType) => seatType switch
    {
        "二等座" => ["A", "B", "C", "D", "F"],
        "一等座" => ["A", "C", "D", "F"],
        "商务座" or "特等座" => ["A", "C", "F"],
        "硬卧代硬座" => ["A", "B", "C", "D"],
        "新空调硬卧" => ["上铺", "中铺", "下铺"],
        "新空调软卧" => ["上铺", "下铺"],
        _ => Array.Empty<string>()
    };

    private static bool TryParseTime(string text, out TimeSpan time)
    {
        time = default;
        if (string.IsNullOrWhiteSpace(text)) return false;
        if (TimeSpan.TryParseExact(text.Trim(), @"hh\:mm", CultureInfo.InvariantCulture, out time)) return true;
        if (TimeSpan.TryParseExact(text.Trim(), @"h\:mm", CultureInfo.InvariantCulture, out time)) return true;
        if (DateTime.TryParse(text, out var dt))
        {
            time = dt.TimeOfDay;
            return true;
        }

        return false;
    }

    private static string FormatTime(TimeSpan t) => $"{(int)t.TotalHours:D2}:{t.Minutes:D2}";

    private static string ReadQuery(IDictionary<string, object> query, string key)
    {
        if (!query.TryGetValue(key, out var v) || v == null) return string.Empty;
        return Uri.UnescapeDataString(v.ToString() ?? "");
    }
}
