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

/// <summary>
///     火车票表单 ViewModel 基类（重构版），用于添加和编辑车票的共享逻辑
/// </summary>
public abstract partial class TrainTicketFormViewModelBase : ObservableObject
{
    // 使用静态共享实例，避免每个窗口都创建新的 Repository 和加载车站数据
    protected static TrainRideRepository? _sharedTrainRideRepository;
    protected static StationRepository? _sharedStationRepository;
    protected static TicketTagRepository? _sharedTicketTagRepository;
    protected static GeneralSettingsService? _sharedGeneralSettingsService;
    protected static LogService? _sharedLogService;
    protected static ObservableCollection<string>? _sharedStationNames;
    protected static ObservableCollection<TicketTag>? _sharedAvailableTags;
    protected static readonly object _initLock = new();

    /// <summary>
    ///     表单字段名称集合（用于撤销重做）
    /// </summary>
    private static readonly HashSet<string> _formFieldNames = new()
    {
        nameof(TrainNoNumber), nameof(SelectedTrainNoPrefix),
        nameof(DepartStationInput), nameof(ArriveStationInput),
        nameof(DepartDateTime), nameof(DepartTimeValue), nameof(ArriveTimeValue), nameof(ArriveDayOffset),
        nameof(CoachNoInput), nameof(IsJiaChe), nameof(SeatNoNumber), nameof(SelectedSeatLetter),
        nameof(IsNoSeat), nameof(MoneyText), nameof(SeatType),
        nameof(AdditionalInfo), nameof(TicketPurpose), nameof(TicketModificationType),
        nameof(Hint), nameof(SelectedStatus),
        nameof(IsStudentTicket), nameof(IsDiscountTicket), nameof(IsOnlineTicket), nameof(IsChildTicket),
        nameof(IsAlipay), nameof(IsWeChat), nameof(IsABC), nameof(IsCCB),
        nameof(IsICBC), nameof(IsBCOM), nameof(IsCMB), nameof(IsPSBC), nameof(IsBOC),
        nameof(TicketNumber), nameof(CheckInLocation)
        // 注意：SelectedTagIds 是集合属性，在 ToggleTagSelection 中手动处理撤销重做
    };

    protected readonly BusinessRuleEngine _businessRuleEngine;
    protected readonly DataTransformer _dataTransformer;
    protected readonly DefaultValueLoader _defaultValueLoader;

    // 表单数据（核心DTO）
    protected readonly TrainTicketFormData _formData;
    protected readonly FormValidator _formValidator;
    protected readonly OptionsProvider _optionsProvider;
    protected readonly StationQueryService _stationQueryService;

    // 解耦的组件
    protected readonly UndoRedoManager _undoRedoManager;
    protected bool _isApplyingRescheduleData = false;
    private bool _isLoadingDefaults;
    private bool _isLoadingExistingData;
    private bool _isProcessingLinkedChanges;
    private bool _isSaving;
    private bool _isCleanedUp;

    // 状态标记
    private bool _isUndoingOrRedoing;

    // 原始值备份
    private TrainTicketFormData? _originalFormData;

    public TrainTicketFormViewModelBase()
    {
        // 初始化解耦组件（注意初始化顺序）
        _undoRedoManager = new UndoRedoManager();
        _formValidator = FormValidator.CreateDefault();
        _dataTransformer = new DataTransformer();
        _optionsProvider = new OptionsProvider();
        _businessRuleEngine = new BusinessRuleEngine(_optionsProvider);

        // 初始化表单数据
        _formData = new TrainTicketFormData();

        // 初始化绑定属性
        _selectedTrainNoPrefix = _formData.SelectedTrainNoPrefix;
        _trainNoNumber = _formData.TrainNoNumber;
        _departStationInput = _formData.DepartStationInput;
        _arriveStationInput = _formData.ArriveStationInput;
        _departDateTime = _formData.DepartDateTime;
        _departTimeValue = _formData.DepartTimeValue;
        _arriveTimeValue = _formData.ArriveTimeValue;
        _arriveDayOffset = _formData.ArriveDayOffset;
        _coachNoInput = _formData.CoachNoInput;
        _isJiaChe = _formData.IsJiaChe;
        _seatNoNumber = _formData.SeatNoNumber;
        _selectedSeatLetter = _formData.SelectedSeatLetter;
        _isNoSeat = _formData.IsNoSeat;
        _seatType = _formData.SeatType;
        _moneyText = _formData.MoneyText;
        _additionalInfo = _formData.AdditionalInfo;
        _ticketPurpose = _formData.TicketPurpose;
        _ticketModificationType = _formData.TicketModificationType;
        _selectedStatus = _formData.SelectedStatus;
        _hint = _formData.Hint;
        _selectedHint = _formData.SelectedHint;
        _ticketNumber = _formData.TicketNumber;
        _checkInLocation = _formData.CheckInLocation;
        _departStationCode = _formData.DepartStationCode;
        _arriveStationCode = _formData.ArriveStationCode;
        _departStationPinyin = _formData.DepartStationPinyin;
        _arriveStationPinyin = _formData.ArriveStationPinyin;
        // 创建新的集合实例，避免引用问题
        _selectedTagIds = new ObservableCollection<int>(_formData.SelectedTagIds);
        _seatLetterOptions = _optionsProvider.GetSeatLetterOptions(_seatType);

        // 初始化共享实例
        lock (_initLock)
        {
            _sharedTrainRideRepository ??= new TrainRideRepository();
            _sharedStationRepository ??= new StationRepository();
            _sharedTicketTagRepository ??= new TicketTagRepository();
            _sharedGeneralSettingsService ??= new GeneralSettingsService();
            _sharedLogService ??= new LogService();

            // 共享车站列表
            if (_sharedStationNames == null)
            {
                _sharedStationNames = new ObservableCollection<string>();
                StationNames = _sharedStationNames;

                if (!DesignerProperties.GetIsInDesignMode(new DependencyObject()))
                    _ = Task.Run(async () => await LoadStationsAsync());
            }
            else
            {
                StationNames = _sharedStationNames;
            }

            // 共享可用标签列表
            if (_sharedAvailableTags == null)
            {
                _sharedAvailableTags = new ObservableCollection<TicketTag>();
                AvailableTags = _sharedAvailableTags;

                if (!DesignerProperties.GetIsInDesignMode(new DependencyObject()))
                    _ = Task.Run(async () => await LoadTagsAsync());
            }
            else
            {
                AvailableTags = _sharedAvailableTags;
            }
        }

        // 初始化默认值加载器
        _defaultValueLoader = new DefaultValueLoader(_generalSettingsService, _optionsProvider);

        // 初始化车站查询服务
        _stationQueryService = new StationQueryService(_stationRepository);

        // 配置撤销重做管理器
        _undoRedoManager.Initialize(_generalSettingsService.Config.MaxUndoSteps);
        _undoRedoManager.SetCurrentData(_formData);
        _undoRedoManager.StateRestored += OnUndoRedoStateRestored;
        _undoRedoManager.StateSaved += OnUndoRedoStateSaved;

        // 订阅属性变更事件
        SetupPropertyChangeHandlers();
    }

    // 实例字段指向共享实例
    protected TrainRideRepository _trainRideRepository => _sharedTrainRideRepository!;
    protected StationRepository _stationRepository => _sharedStationRepository!;
    protected TicketTagRepository _ticketTagRepository => _sharedTicketTagRepository!;
    protected GeneralSettingsService _generalSettingsService => _sharedGeneralSettingsService!;
    protected LogService _logService => _sharedLogService!;

    /// <summary>
    ///     设置属性变更处理程序
    /// </summary>
    private void SetupPropertyChangeHandlers()
    {
        PropertyChanged += OnFormPropertyChanged;
    }

    /// <summary>
    ///     窗口关闭后释放撤销重做与属性变更订阅。
    /// </summary>
    public void Cleanup()
    {
        if (_isCleanedUp)
            return;

        _isCleanedUp = true;
        PropertyChanged -= OnFormPropertyChanged;
        _undoRedoManager.StateRestored -= OnUndoRedoStateRestored;
        _undoRedoManager.StateSaved -= OnUndoRedoStateSaved;
    }

    private void OnFormPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // 保存撤销状态（在同步到FormData之前，保存当前状态）
        if (!_isUndoingOrRedoing && !_isLoadingDefaults && !_isProcessingLinkedChanges && !_isLoadingExistingData
            && _formFieldNames.Contains(e.PropertyName) && _generalSettingsService.Config.EnableUndo)
        {
            _undoRedoManager.BeginPropertyChange(e.PropertyName);
            AddOperationHistory(e.PropertyName);
        }

        // 同步到FormData
        SyncToFormData(e.PropertyName);

        // 监听席别变化，更新座位号选项
        if (e.PropertyName == nameof(SeatType)) UpdateSeatLetterOptions();

        // 监听无座复选框变化
        if (e.PropertyName == nameof(IsNoSeat))
        {
            IsSeatNoInputEnabled = !IsNoSeat;
            IsSeatLetterEnabled = !IsNoSeat && _optionsProvider.IsSeatLetterVisible(SeatType);
        }

        // 监听附加信息变化，更新车票用途选项
        if (e.PropertyName == nameof(AdditionalInfo) && !_isProcessingLinkedChanges)
        {
            _isProcessingLinkedChanges = true;
            _optionsProvider.UpdateTicketPurposeOptions(AdditionalInfo, _optionsProvider.TicketPurposeOptions,
                TicketPurpose);
            _isProcessingLinkedChanges = false;
        }

        // 监听车票用途变化，更新附加信息选项
        if (e.PropertyName == nameof(TicketPurpose) && !_isProcessingLinkedChanges)
        {
            _isProcessingLinkedChanges = true;
            _optionsProvider.UpdateAdditionalInfoOptions(TicketPurpose, _optionsProvider.AdditionalInfoOptions,
                AdditionalInfo);
            _isProcessingLinkedChanges = false;
        }

        // 监听提示信息变化
        if (e.PropertyName == nameof(SelectedHint))
        {
            if (SelectedHint == "自定义")
                ShowCustomHintDialog();
            else
                Hint = SelectedHint;
        }

        // 监听出发车站变化，自动查询车站信息和联想
        if (e.PropertyName == nameof(DepartStationInput))
        {
            _logService?.Info("TrainTicketFormViewModelBase",
                $"[DEBUG] DepartStationInput 属性变更，新值: '{DepartStationInput}'");
            _logService?.Info("TrainTicketFormViewModelBase",
                $"[DEBUG] 状态检查: _isProcessingLinkedChanges={_isProcessingLinkedChanges}, _isUndoingOrRedoing={_isUndoingOrRedoing}, _isLoadingDefaults={_isLoadingDefaults}, _isLoadingExistingData={_isLoadingExistingData}, _isSaving={_isSaving}, _isApplyingRescheduleData={_isApplyingRescheduleData}");

            if (!_isProcessingLinkedChanges && !_isUndoingOrRedoing && !_isLoadingDefaults &&
                !_isLoadingExistingData && !_isSaving && !_isApplyingRescheduleData)
            {
                _logService?.Info("TrainTicketFormViewModelBase", "[DEBUG] 条件满足，开始执行查询和联想搜索");
                _ = QueryDepartStationInfoAsync();
                _ = SearchDepartStationSuggestionsAsync();
            }
            else
            {
                _logService?.Info("TrainTicketFormViewModelBase", "[DEBUG] 条件不满足，跳过查询和联想搜索");
            }
        }

        // 监听到达车站变化，自动查询车站信息和联想
        if (e.PropertyName == nameof(ArriveStationInput))
        {
            _logService?.Info("TrainTicketFormViewModelBase",
                $"[DEBUG] ArriveStationInput 属性变更，新值: '{ArriveStationInput}'");
            _logService?.Info("TrainTicketFormViewModelBase",
                $"[DEBUG] 状态检查: _isProcessingLinkedChanges={_isProcessingLinkedChanges}, _isUndoingOrRedoing={_isUndoingOrRedoing}, _isLoadingDefaults={_isLoadingDefaults}, _isLoadingExistingData={_isLoadingExistingData}, _isSaving={_isSaving}, _isApplyingRescheduleData={_isApplyingRescheduleData}");

            if (!_isProcessingLinkedChanges && !_isUndoingOrRedoing && !_isLoadingDefaults &&
                !_isLoadingExistingData && !_isSaving && !_isApplyingRescheduleData)
            {
                _logService?.Info("TrainTicketFormViewModelBase", "[DEBUG] 条件满足，开始执行查询和联想搜索");
                _ = QueryArriveStationInfoAsync();
                _ = SearchArriveStationSuggestionsAsync();
            }
            else
            {
                _logService?.Info("TrainTicketFormViewModelBase", "[DEBUG] 条件不满足，跳过查询和联想搜索");
            }
        }

        // 处理票种类型互斥（学生票与儿童票）
        if (e.PropertyName == nameof(IsStudentTicket) || e.PropertyName == nameof(IsChildTicket))
            if (!_isProcessingLinkedChanges && !_isUndoingOrRedoing && !_isLoadingDefaults &&
                !_isLoadingExistingData)
            {
                _isProcessingLinkedChanges = true;
                _businessRuleEngine.HandleTicketTypeMutex(_formData, e.PropertyName);
                SyncFromFormData();
                _isProcessingLinkedChanges = false;
            }

        // 处理支付渠道互斥
        var paymentProperties = new[]
        {
            nameof(IsAlipay), nameof(IsWeChat), nameof(IsABC), nameof(IsCCB), nameof(IsICBC), nameof(IsBCOM),
            nameof(IsCMB), nameof(IsPSBC), nameof(IsBOC)
        };
        if (paymentProperties.Contains(e.PropertyName))
            if (!_isProcessingLinkedChanges && !_isUndoingOrRedoing && !_isLoadingDefaults &&
                !_isLoadingExistingData)
            {
                _isProcessingLinkedChanges = true;
                _businessRuleEngine.HandlePaymentChannelMutex(_formData, e.PropertyName);
                SyncFromFormData();
                _isProcessingLinkedChanges = false;
            }

        // 执行业务规则
        if (!_isProcessingLinkedChanges && !_isUndoingOrRedoing && !_isLoadingDefaults && !_isLoadingExistingData)
        {
            _isProcessingLinkedChanges = true;
            var modified =
                _businessRuleEngine.Execute(_formData, e.PropertyName, _optionsProvider.TicketPurposeOptions);
            if (modified) SyncFromFormData();
            _isProcessingLinkedChanges = false;
        }

        CheckForChanges();
        UpdateUndoRedoCommands();
    }

    /// <summary>
    ///     同步属性到FormData
    /// </summary>
    private void SyncToFormData(string? propertyName = null)
    {
        if (string.IsNullOrEmpty(propertyName) || propertyName == nameof(SelectedTrainNoPrefix))
            _formData.SelectedTrainNoPrefix = SelectedTrainNoPrefix;
        if (string.IsNullOrEmpty(propertyName) || propertyName == nameof(TrainNoNumber))
            _formData.TrainNoNumber = TrainNoNumber;
        if (string.IsNullOrEmpty(propertyName) || propertyName == nameof(DepartStationInput))
            _formData.DepartStationInput = DepartStationInput;
        if (string.IsNullOrEmpty(propertyName) || propertyName == nameof(ArriveStationInput))
            _formData.ArriveStationInput = ArriveStationInput;
        if (string.IsNullOrEmpty(propertyName) || propertyName == nameof(DepartDateTime))
            _formData.DepartDateTime = DepartDateTime;
        if (string.IsNullOrEmpty(propertyName) || propertyName == nameof(DepartTimeValue))
            _formData.DepartTimeValue = DepartTimeValue;
        if (string.IsNullOrEmpty(propertyName) || propertyName == nameof(ArriveTimeValue))
            _formData.ArriveTimeValue = ArriveTimeValue;
        if (string.IsNullOrEmpty(propertyName) || propertyName == nameof(ArriveDayOffset))
            _formData.ArriveDayOffset = ArriveDayOffset;
        if (string.IsNullOrEmpty(propertyName) || propertyName == nameof(CoachNoInput))
            _formData.CoachNoInput = CoachNoInput;
        if (string.IsNullOrEmpty(propertyName) || propertyName == nameof(IsJiaChe))
            _formData.IsJiaChe = IsJiaChe;
        if (string.IsNullOrEmpty(propertyName) || propertyName == nameof(SeatNoNumber))
            _formData.SeatNoNumber = SeatNoNumber;
        if (string.IsNullOrEmpty(propertyName) || propertyName == nameof(SelectedSeatLetter))
            _formData.SelectedSeatLetter = SelectedSeatLetter;
        if (string.IsNullOrEmpty(propertyName) || propertyName == nameof(IsNoSeat))
            _formData.IsNoSeat = IsNoSeat;
        if (string.IsNullOrEmpty(propertyName) || propertyName == nameof(SeatType))
            _formData.SeatType = SeatType;
        if (string.IsNullOrEmpty(propertyName) || propertyName == nameof(MoneyText))
            _formData.MoneyText = MoneyText;
        if (string.IsNullOrEmpty(propertyName) || propertyName == nameof(AdditionalInfo))
            _formData.AdditionalInfo = AdditionalInfo;
        if (string.IsNullOrEmpty(propertyName) || propertyName == nameof(TicketPurpose))
            _formData.TicketPurpose = TicketPurpose;
        if (string.IsNullOrEmpty(propertyName) || propertyName == nameof(TicketModificationType))
            _formData.TicketModificationType = TicketModificationType;
        if (string.IsNullOrEmpty(propertyName) || propertyName == nameof(Hint))
            _formData.Hint = Hint;
        if (string.IsNullOrEmpty(propertyName) || propertyName == nameof(SelectedHint))
            _formData.SelectedHint = SelectedHint;
        if (string.IsNullOrEmpty(propertyName) || propertyName == nameof(SelectedStatus))
            _formData.SelectedStatus = SelectedStatus;
        if (string.IsNullOrEmpty(propertyName) || propertyName == nameof(IsStudentTicket))
            _formData.IsStudentTicket = IsStudentTicket;
        if (string.IsNullOrEmpty(propertyName) || propertyName == nameof(IsDiscountTicket))
            _formData.IsDiscountTicket = IsDiscountTicket;
        if (string.IsNullOrEmpty(propertyName) || propertyName == nameof(IsOnlineTicket))
            _formData.IsOnlineTicket = IsOnlineTicket;
        if (string.IsNullOrEmpty(propertyName) || propertyName == nameof(IsChildTicket))
            _formData.IsChildTicket = IsChildTicket;
        if (string.IsNullOrEmpty(propertyName) || propertyName == nameof(IsAlipay))
            _formData.IsAlipay = IsAlipay;
        if (string.IsNullOrEmpty(propertyName) || propertyName == nameof(IsWeChat))
            _formData.IsWeChat = IsWeChat;
        if (string.IsNullOrEmpty(propertyName) || propertyName == nameof(IsABC))
            _formData.IsABC = IsABC;
        if (string.IsNullOrEmpty(propertyName) || propertyName == nameof(IsCCB))
            _formData.IsCCB = IsCCB;
        if (string.IsNullOrEmpty(propertyName) || propertyName == nameof(IsICBC))
            _formData.IsICBC = IsICBC;
        if (string.IsNullOrEmpty(propertyName) || propertyName == nameof(IsBCOM))
            _formData.IsBCOM = IsBCOM;
        if (string.IsNullOrEmpty(propertyName) || propertyName == nameof(IsCMB))
            _formData.IsCMB = IsCMB;
        if (string.IsNullOrEmpty(propertyName) || propertyName == nameof(IsPSBC))
            _formData.IsPSBC = IsPSBC;
        if (string.IsNullOrEmpty(propertyName) || propertyName == nameof(IsBOC))
            _formData.IsBOC = IsBOC;
        if (string.IsNullOrEmpty(propertyName) || propertyName == nameof(TicketNumber))
            _formData.TicketNumber = TicketNumber;
        if (string.IsNullOrEmpty(propertyName) || propertyName == nameof(CheckInLocation))
            _formData.CheckInLocation = CheckInLocation;
        if (string.IsNullOrEmpty(propertyName) || propertyName == nameof(DepartStationCode))
            _formData.DepartStationCode = DepartStationCode;
        if (string.IsNullOrEmpty(propertyName) || propertyName == nameof(ArriveStationCode))
            _formData.ArriveStationCode = ArriveStationCode;
        if (string.IsNullOrEmpty(propertyName) || propertyName == nameof(DepartStationPinyin))
            _formData.DepartStationPinyin = DepartStationPinyin;
        if (string.IsNullOrEmpty(propertyName) || propertyName == nameof(ArriveStationPinyin))
            _formData.ArriveStationPinyin = ArriveStationPinyin;
        if (string.IsNullOrEmpty(propertyName) || propertyName == nameof(SelectedTagIds))
            _formData.SelectedTagIds = new ObservableCollection<int>(SelectedTagIds);
    }

    /// <summary>
    ///     用克隆的表单数据覆盖当前表单并同步到绑定属性（加载/批次切换时使用）。
    /// </summary>
    protected void ApplyFormDataClone(TrainTicketFormData data)
    {
        if (data == null) return;
        _isLoadingExistingData = true;
        try
        {
            data.CopyTo(_formData);
            SyncFromFormData();
            UpdateSeatLetterOptions();
        }
        finally
        {
            _isLoadingExistingData = false;
        }
    }

    /// <summary>
    ///     从FormData同步到属性
    /// </summary>
    protected void SyncFromFormData()
    {
        SelectedTrainNoPrefix = _formData.SelectedTrainNoPrefix;
        TrainNoNumber = _formData.TrainNoNumber;
        DepartStationInput = _formData.DepartStationInput;
        ArriveStationInput = _formData.ArriveStationInput;
        DepartDateTime = _formData.DepartDateTime;
        DepartTimeValue = _formData.DepartTimeValue;
        ArriveTimeValue = _formData.ArriveTimeValue;
        ArriveDayOffset = _formData.ArriveDayOffset;
        CoachNoInput = _formData.CoachNoInput;
        IsJiaChe = _formData.IsJiaChe;
        SeatNoNumber = _formData.SeatNoNumber;
        SelectedSeatLetter = _formData.SelectedSeatLetter;
        IsNoSeat = _formData.IsNoSeat;
        SeatType = _formData.SeatType;
        MoneyText = _formData.MoneyText;
        AdditionalInfo = _formData.AdditionalInfo;
        TicketPurpose = _formData.TicketPurpose;
        TicketModificationType = _formData.TicketModificationType;
        Hint = _formData.Hint;
        SelectedHint = _formData.SelectedHint;
        SelectedStatus = _formData.SelectedStatus;
        IsStudentTicket = _formData.IsStudentTicket;
        IsDiscountTicket = _formData.IsDiscountTicket;
        IsOnlineTicket = _formData.IsOnlineTicket;
        IsChildTicket = _formData.IsChildTicket;
        IsAlipay = _formData.IsAlipay;
        IsWeChat = _formData.IsWeChat;
        IsABC = _formData.IsABC;
        IsCCB = _formData.IsCCB;
        IsICBC = _formData.IsICBC;
        IsBCOM = _formData.IsBCOM;
        IsCMB = _formData.IsCMB;
        IsPSBC = _formData.IsPSBC;
        IsBOC = _formData.IsBOC;
        TicketNumber = _formData.TicketNumber;
        CheckInLocation = _formData.CheckInLocation;
        DepartStationCode = _formData.DepartStationCode;
        ArriveStationCode = _formData.ArriveStationCode;
        DepartStationPinyin = _formData.DepartStationPinyin;
        ArriveStationPinyin = _formData.ArriveStationPinyin;
        SelectedTagIds = new ObservableCollection<int>(_formData.SelectedTagIds);
        _logService?.Info("TrainTicketFormViewModelBase",
            $"[SyncFromFormData] SelectedTagIds 已同步: [{string.Join(",", SelectedTagIds)}]");
    }


    /// <summary>
    ///     更新座位字母选项
    /// </summary>
    private void UpdateSeatLetterOptions()
    {
        SeatLetterOptions = _optionsProvider.GetSeatLetterOptions(SeatType);
        IsSeatLetterVisible = _optionsProvider.IsSeatLetterVisible(SeatType);
        IsSeatLetterEnabled = !IsNoSeat && IsSeatLetterVisible;

        if (SeatLetterOptions.Count > 0 && !SeatLetterOptions.Contains(SelectedSeatLetter))
            SelectedSeatLetter = SeatLetterOptions[0];
        else if (SeatLetterOptions.Count == 0) SelectedSeatLetter = string.Empty;
    }

    /// <summary>
    ///     显示自定义提示信息对话框
    /// </summary>
    private void ShowCustomHintDialog()
    {
        var dialog = new InputDialogWindow("请输入自定义提示信息", "自定义提示", Hint);
        if (dialog.ShowDialog() == true)
        {
            var newHint = dialog.InputText;
            if (!string.IsNullOrEmpty(newHint))
            {
                Hint = newHint;
                _optionsProvider.AddCustomHint(newHint);
                SelectedHint = newHint;
            }
        }
        else
        {
            // 取消时，恢复之前的选择
            if (!string.IsNullOrEmpty(Hint) && HintOptions.Contains(Hint))
            {
                SelectedHint = Hint;
            }
            else
            {
                SelectedHint = HintOptions[0];
                Hint = SelectedHint;
            }
        }
    }

    /// <summary>
    ///     加载默认配置值
    /// </summary>
    protected virtual void LoadDefaultValues()
    {
        _isLoadingDefaults = true;
        try
        {
            // 重新加载配置，确保获取最新设置
            _generalSettingsService.RefreshConfig();

            // 根据设置重新排序选项
            _defaultValueLoader.ReorderOptionsBySettings();

            _defaultValueLoader.LoadDefaults(_formData, IsStatusVisible);

            // 加载默认标签
            LoadDefaultTags();

            SyncFromFormData();
            _logService?.Info("TrainTicketFormViewModelBase",
                $"[LoadFromTrainRide] SyncFromFormData 后 SelectedTagIds: [{string.Join(",", SelectedTagIds)}]");

            UpdateSeatLetterOptions();
            BackupOriginalValues();
            _logService?.Info("TrainTicketFormViewModelBase",
                $"[LoadFromTrainRide] BackupOriginalValues 后 _originalFormData.SelectedTagIds: [{string.Join(",", _originalFormData.SelectedTagIds)}]");

            // 触发 SelectedTagIds 属性变更通知，让 UI 更新标签视觉状态
            OnPropertyChanged(nameof(SelectedTagIds));
            _logService?.Info("TrainTicketFormViewModelBase",
                "[LoadFromTrainRide] 已触发 SelectedTagIds PropertyChanged 事件");
        }
        finally
        {
            _isLoadingDefaults = false;
            _undoRedoManager.SetInitialState(FormState.FromFormData(_formData.Clone(), string.Empty));
        }
    }

    /// <summary>
    ///     加载默认标签
    /// </summary>
    private async void LoadDefaultTags()
    {
        try
        {
            var defaultTags = await _ticketTagRepository.GetDefaultTagsAsync();
            if (defaultTags.Any())
            {
                foreach (var tag in defaultTags)
                    if (!SelectedTagIds.Contains(tag.Id))
                        SelectedTagIds.Add(tag.Id);

                // 触发属性变更通知，让 PropertyChanged 事件处理器同步到 FormData
                OnPropertyChanged(nameof(SelectedTagIds));
            }
        }
        catch (Exception ex)
        {
            _logService?.Error("TrainTicketFormViewModelBase", $"加载默认标签失败: {ex.Message}");
        }
    }

    /// <summary>
    ///     备份原始值
    /// </summary>
    protected void BackupOriginalValues()
    {
        _originalFormData = _formData.Clone();
        HasUnsavedChanges = false;
    }

    /// <summary>
    ///     将脏检查基线设为指定快照（批次切换恢复时使用）。
    /// </summary>
    protected void SetOriginalFormData(TrainTicketFormData? original)
    {
        _originalFormData = original?.Clone();
        CheckForChanges();
    }

    /// <summary>
    ///     当前脏检查基线的克隆；无基线时返回 null。
    /// </summary>
    protected TrainTicketFormData? CloneOriginalFormData()
    {
        return _originalFormData?.Clone();
    }

    /// <summary>
    ///     检查是否有未保存的更改
    /// </summary>
    public void CheckForChanges()
    {
        // 保存期间跳过检查
        if (_isSaving)
        {
            _logService?.Info("TrainTicketFormViewModelBase", "CheckForChanges: 保存期间跳过检查");
            return;
        }

        if (_originalFormData == null)
        {
            HasUnsavedChanges = false;
            return;
        }

        var hasChanges = !AreFormDataEqual(_formData, _originalFormData);
        if (hasChanges != HasUnsavedChanges)
            _logService?.Info("TrainTicketFormViewModelBase",
                $"CheckForChanges: HasUnsavedChanges 从 {HasUnsavedChanges} 变为 {hasChanges}");
        HasUnsavedChanges = hasChanges;
    }

    /// <summary>
    ///     比较两个表单数据是否相等
    /// </summary>
    private bool AreFormDataEqual(TrainTicketFormData a, TrainTicketFormData b)
    {
        return a.TrainNoNumber == b.TrainNoNumber &&
               a.SelectedTrainNoPrefix == b.SelectedTrainNoPrefix &&
               a.DepartStationInput == b.DepartStationInput &&
               a.ArriveStationInput == b.ArriveStationInput &&
               a.DepartDateTime == b.DepartDateTime &&
               a.DepartTimeValue == b.DepartTimeValue &&
               a.ArriveTimeValue == b.ArriveTimeValue &&
               a.ArriveDayOffset == b.ArriveDayOffset &&
               a.CoachNoInput == b.CoachNoInput &&
               a.IsJiaChe == b.IsJiaChe &&
               a.SeatNoNumber == b.SeatNoNumber &&
               a.SelectedSeatLetter == b.SelectedSeatLetter &&
               a.IsNoSeat == b.IsNoSeat &&
               a.MoneyText == b.MoneyText &&
               a.SeatType == b.SeatType &&
               a.AdditionalInfo == b.AdditionalInfo &&
               a.TicketPurpose == b.TicketPurpose &&
               a.TicketModificationType == b.TicketModificationType &&
               a.Hint == b.Hint &&
               a.SelectedStatus == b.SelectedStatus &&
               a.IsStudentTicket == b.IsStudentTicket &&
               a.IsDiscountTicket == b.IsDiscountTicket &&
               a.IsOnlineTicket == b.IsOnlineTicket &&
               a.IsChildTicket == b.IsChildTicket &&
               a.IsAlipay == b.IsAlipay &&
               a.IsWeChat == b.IsWeChat &&
               a.IsABC == b.IsABC &&
               a.IsCCB == b.IsCCB &&
               a.IsICBC == b.IsICBC &&
               a.IsBCOM == b.IsBCOM &&
               a.IsCMB == b.IsCMB &&
               a.IsPSBC == b.IsPSBC &&
               a.IsBOC == b.IsBOC &&
               a.TicketNumber == b.TicketNumber &&
               a.CheckInLocation == b.CheckInLocation &&
               a.SelectedTagIds.OrderBy(x => x).SequenceEqual(b.SelectedTagIds.OrderBy(x => x));
    }


    /// <summary>
    ///     放弃更改，恢复到原始值
    /// </summary>
    public void DiscardChanges()
    {
        if (_originalFormData != null)
        {
            _originalFormData.CopyTo(_formData);
            SyncFromFormData();
            HasUnsavedChanges = false;
        }
    }

    /// <summary>
    ///     从现有车票加载数据（编辑模式使用）
    /// </summary>
    public virtual void LoadFromTrainRide(TrainRideInfo trainRide)
    {
        if (trainRide == null) return;

        _logService?.Info("TrainTicketFormViewModelBase", $"[LoadFromTrainRide] 开始加载车票 ID={trainRide.Id}");
        _logService?.Info("TrainTicketFormViewModelBase", $"[LoadFromTrainRide] 实体标签数量: {trainRide.Tags?.Count ?? 0}");
        if (trainRide.Tags != null && trainRide.Tags.Any())
            _logService?.Info("TrainTicketFormViewModelBase",
                $"[LoadFromTrainRide] 实体标签 IDs: [{string.Join(",", trainRide.Tags.Select(t => t.Id))}]");

        _isLoadingExistingData = true;
        try
        {
            EditTicketId = trainRide.Id;

            // 使用 DataTransformer 进行转换
            var data = _dataTransformer.FromEntity(trainRide);
            _logService?.Info("TrainTicketFormViewModelBase",
                $"[LoadFromTrainRide] 转换后 data.SelectedTagIds: [{string.Join(",", data.SelectedTagIds)}]");

            data.CopyTo(_formData);
            _logService?.Info("TrainTicketFormViewModelBase",
                $"[LoadFromTrainRide] CopyTo 后 _formData.SelectedTagIds: [{string.Join(",", _formData.SelectedTagIds)}]");

            // 确保自定义提示信息在选项列表中
            if (!string.IsNullOrEmpty(_formData.Hint)) _optionsProvider.EnsureHintInOptions(_formData.Hint);

            SyncFromFormData();
            UpdateSeatLetterOptions();
            BackupOriginalValues();
        }
        finally
        {
            _isLoadingExistingData = false;
            // 确保加载完成后重置未保存更改标志
            // 防止异步操作（如 UpdateTicketPurposeOptions）导致标志被设置
            HasUnsavedChanges = false;
            // 清空操作历史，因为加载现有数据不应该有历史记录
            OperationHistory.Clear();
            _undoRedoManager.SetInitialState(FormState.FromFormData(_formData.Clone(), string.Empty));
        }
    }

    /// <summary>
    ///     创建 TrainRideInfo 对象
    /// </summary>
    protected virtual TrainRideInfo CreateTrainRideInfo()
    {
        return _dataTransformer.ToEntity(_formData, EditTicketId ?? 0);
    }

    /// <summary>
    ///     验证表单数据
    /// </summary>
    protected virtual bool ValidateForm()
    {
        var result = _formValidator.Validate(_formData);

        if (!result.IsValid)
        {
            MessageBoxWindow.Show(result.Errors.First().ErrorMessage, "提示", MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return false;
        }

        return true;
    }

    /// <summary>
    ///     静默校验（不弹窗），供 OCR「直接保存」使用。
    /// </summary>
    public bool TryValidateSilent(out string? errorMessage)
    {
        var result = _formValidator.Validate(_formData);
        if (result.IsValid)
        {
            errorMessage = null;
            return true;
        }

        errorMessage = result.Errors.FirstOrDefault()?.ErrorMessage ?? "表单校验未通过";
        return false;
    }


    /// <summary>
    ///     异步加载车站列表
    /// </summary>
    protected async Task LoadStationsAsync()
    {
        var stations = await _stationRepository.GetAllStationsAsync();
        foreach (var station in stations) StationNames.Add(station.StationName);
    }

    /// <summary>
    ///     异步加载标签列表
    /// </summary>
    protected async Task LoadTagsAsync()
    {
        try
        {
            var tags = await _ticketTagRepository.GetAllTagsAsync();
            foreach (var tag in tags) AvailableTags.Add(tag);
        }
        catch (Exception ex)
        {
            _logService?.Error("TrainTicketFormViewModelBase", $"加载标签失败: {ex.Message}");
        }
    }
}
