using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using GuiPiao.DataAccess;
using GuiPiao.Messages;
using GuiPiao.Model;
using GuiPiao.Services;
using GuiPiao.View;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace GuiPiao.ViewModel.TrainTicketForm
{
    /// <summary>
    /// 火车票表单 ViewModel 基类（重构版），用于添加和编辑车票的共享逻辑
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
        protected static readonly object _initLock = new object();

        // 实例字段指向共享实例
        protected TrainRideRepository _trainRideRepository => _sharedTrainRideRepository!;
        protected StationRepository _stationRepository => _sharedStationRepository!;
        protected TicketTagRepository _ticketTagRepository => _sharedTicketTagRepository!;
        protected GeneralSettingsService _generalSettingsService => _sharedGeneralSettingsService!;
        protected LogService _logService => _sharedLogService!;

        // 解耦的组件
        protected readonly UndoRedoManager _undoRedoManager;
        protected readonly FormValidator _formValidator;
        protected readonly DataTransformer _dataTransformer;
        protected readonly BusinessRuleEngine _businessRuleEngine;
        protected readonly OptionsProvider _optionsProvider;
        protected readonly DefaultValueLoader _defaultValueLoader;
        protected readonly StationQueryService _stationQueryService;

        // 表单数据（核心DTO）
        protected readonly TrainTicketFormData _formData;

        #region 绑定属性（代理到FormData）

        // 车次号相关属性
        public ObservableCollection<string> TrainNoPrefixes => _optionsProvider.TrainNoPrefixes;

        [ObservableProperty]
        private string _selectedTrainNoPrefix;

        [ObservableProperty]
        private string _trainNoNumber;

        public string TrainNo => _formData.TrainNo;

        // 车站相关属性
        [ObservableProperty]
        private string _departStationInput;

        [ObservableProperty]
        private string _arriveStationInput;

        public string DepartStation => _formData.DepartStation;
        public string ArriveStation => _formData.ArriveStation;

        // 日期时间相关属性
        [ObservableProperty]
        private DateTime? _departDateTime;

        public string DepartDate => _formData.DepartDate;

        [ObservableProperty]
        private DateTime? _departTimeValue;

        public string DepartTime => _formData.DepartTime;

        // 车厢号相关属性
        [ObservableProperty]
        private string _coachNoInput;

        public string CoachNo => _formData.CoachNo;

        // 座位号相关属性
        [ObservableProperty]
        private string _seatNoNumber;

        [ObservableProperty]
        private ObservableCollection<string> _seatLetterOptions;

        [ObservableProperty]
        private string _selectedSeatLetter;

        [ObservableProperty]
        private bool _isNoSeat;

        [ObservableProperty]
        private bool _isSeatNoInputEnabled = true;

        [ObservableProperty]
        private bool _isSeatLetterEnabled = true;

        [ObservableProperty]
        private bool _isSeatLetterVisible = true;

        public string SeatNo => _formData.SeatNo;

        // 席别相关属性
        public ObservableCollection<string> SeatTypeOptions => _optionsProvider.SeatTypeOptions;

        [ObservableProperty]
        private string _seatType;

        // 金额相关属性
        [ObservableProperty]
        private string _moneyText;

        public decimal Money => _formData.Money;

        // 附加信息相关属性
        public ObservableCollection<string> AdditionalInfoOptions => _optionsProvider.AdditionalInfoOptions;

        [ObservableProperty]
        private string _additionalInfo;

        // 车票用途相关属性
        public ObservableCollection<string> TicketPurposeOptions => _optionsProvider.TicketPurposeOptions;

        [ObservableProperty]
        private string _ticketPurpose;

        // 改签类型相关属性
        public ObservableCollection<string> TicketModificationTypeOptions => _optionsProvider.TicketModificationTypeOptions;

        [ObservableProperty]
        private string _ticketModificationType;

        // 状态相关属性（仅新增窗口）
        [ObservableProperty]
        private bool _isStatusVisible = false;

        public ObservableCollection<string> StatusOptions => _optionsProvider.StatusOptions;

        [ObservableProperty]
        private string _selectedStatus;

        public int StatusValue => _formData.StatusValue;

        // 票种类型相关属性
        [ObservableProperty]
        private bool _isStudentTicket;

        [ObservableProperty]
        private bool _isDiscountTicket;

        [ObservableProperty]
        private bool _isOnlineTicket;

        [ObservableProperty]
        private bool _isChildTicket;

        public int TicketTypeFlags
        {
            get => _formData.TicketTypeFlags;
            set => _formData.TicketTypeFlags = value;
        }

        // 支付渠道相关属性
        [ObservableProperty]
        private bool _isAlipay;

        [ObservableProperty]
        private bool _isWeChat;

        [ObservableProperty]
        private bool _isABC;

        [ObservableProperty]
        private bool _isCCB;

        [ObservableProperty]
        private bool _isICBC;

        [ObservableProperty]
        private bool _isBCOM;

        [ObservableProperty]
        private bool _isCMB;

        [ObservableProperty]
        private bool _isPSBC;

        [ObservableProperty]
        private bool _isBOC;

        public int PaymentChannelFlags
        {
            get => _formData.PaymentChannelFlags;
            set => _formData.PaymentChannelFlags = value;
        }

        // 提示信息相关属性
        public ObservableCollection<string> HintOptions => _optionsProvider.HintOptions;

        [ObservableProperty]
        private string _selectedHint;

        [ObservableProperty]
        private string _hint;

        // 其他属性
        [ObservableProperty]
        private string _ticketNumber;

        [ObservableProperty]
        private string _checkInLocation;

        [ObservableProperty]
        private string _departStationCode;

        [ObservableProperty]
        private string _arriveStationCode;

        [ObservableProperty]
        private string _departStationPinyin;

        [ObservableProperty]
        private string _arriveStationPinyin;

        [ObservableProperty]
        private ObservableCollection<string> _stationNames;

        // 车站联想相关属性
        [ObservableProperty]
        private ObservableCollection<string> _departStationSuggestions = new();

        [ObservableProperty]
        private ObservableCollection<string> _arriveStationSuggestions = new();

        [ObservableProperty]
        private bool _isDepartStationDropdownOpen = false;

        [ObservableProperty]
        private bool _isArriveStationDropdownOpen = false;

        [ObservableProperty]
        private int _departStationSelectedIndex = -1;

        [ObservableProperty]
        private int _arriveStationSelectedIndex = -1;

        [ObservableProperty]
        private ObservableCollection<TicketTag> _availableTags;

        [ObservableProperty]
        private ObservableCollection<int> _selectedTagIds;

        [ObservableProperty]
        private string _windowTitle = "火车票";

        [ObservableProperty]
        private string _saveButtonText = "保存";

        [ObservableProperty]
        private bool _isEditMode = false;

        [ObservableProperty]
        private int? _editTicketId = null;

        [ObservableProperty]
        private bool _hasUnsavedChanges = false;

        [ObservableProperty]
        private bool _canUndo = false;

        [ObservableProperty]
        private bool _canRedo = false;

        [ObservableProperty]
        private ObservableCollection<OperationHistoryItem> _operationHistory = new();

        [ObservableProperty]
        private bool _isOperationHistoryExpanded = false;

        // 改签相关属性
        [ObservableProperty]
        private bool _isRescheduleMode = false;

        [ObservableProperty]
        private int _originalTicketId = 0;

        [ObservableProperty]
        private string _originalTicketStatus = string.Empty;

        [ObservableProperty]
        private bool _isDepartStationReadOnly = false;

        [ObservableProperty]
        private bool _isArriveStationReadOnly = false;

        [ObservableProperty]
        private bool _isRescheduleTypeChangeDestination = false;

        #endregion

        // 状态标记
        private bool _isUndoingOrRedoing = false;
        private bool _isLoadingDefaults = false;
        private bool _isProcessingLinkedChanges = false;
        private bool _isLoadingExistingData = false;
        private bool _isSaving = false;
        protected bool _isApplyingRescheduleData = false;

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
            _coachNoInput = _formData.CoachNoInput;
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
                    {
                        _ = Task.Run(async () => await LoadStationsAsync());
                    }
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
                    {
                        _ = Task.Run(async () => await LoadTagsAsync());
                    }
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

        /// <summary>
        /// 表单字段名称集合（用于撤销重做）
        /// </summary>
        private static readonly HashSet<string> _formFieldNames = new HashSet<string>
        {
            nameof(TrainNoNumber), nameof(SelectedTrainNoPrefix),
            nameof(DepartStationInput), nameof(ArriveStationInput),
            nameof(DepartDateTime), nameof(DepartTimeValue),
            nameof(CoachNoInput), nameof(SeatNoNumber), nameof(SelectedSeatLetter),
            nameof(IsNoSeat), nameof(MoneyText), nameof(SeatType),
            nameof(AdditionalInfo), nameof(TicketPurpose), nameof(TicketModificationType),
            nameof(Hint), nameof(SelectedStatus),
            nameof(IsStudentTicket), nameof(IsDiscountTicket), nameof(IsOnlineTicket), nameof(IsChildTicket),
            nameof(IsAlipay), nameof(IsWeChat), nameof(IsABC), nameof(IsCCB),
            nameof(IsICBC), nameof(IsBCOM), nameof(IsCMB), nameof(IsPSBC), nameof(IsBOC),
            nameof(TicketNumber), nameof(CheckInLocation)
            // 注意：SelectedTagIds 是集合属性，在 ToggleTagSelection 中手动处理撤销重做
        };

        /// <summary>
        /// 设置属性变更处理程序
        /// </summary>
        private void SetupPropertyChangeHandlers()
        {
            PropertyChanged += (s, e) =>
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
                if (e.PropertyName == nameof(SeatType))
                {
                    UpdateSeatLetterOptions();
                }

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
                    _optionsProvider.UpdateTicketPurposeOptions(AdditionalInfo, _optionsProvider.TicketPurposeOptions, TicketPurpose);
                    _isProcessingLinkedChanges = false;
                }

                // 监听车票用途变化，更新附加信息选项
                if (e.PropertyName == nameof(TicketPurpose) && !_isProcessingLinkedChanges)
                {
                    _isProcessingLinkedChanges = true;
                    _optionsProvider.UpdateAdditionalInfoOptions(TicketPurpose, _optionsProvider.AdditionalInfoOptions, AdditionalInfo);
                    _isProcessingLinkedChanges = false;
                }

                // 监听提示信息变化
                if (e.PropertyName == nameof(SelectedHint))
                {
                    if (SelectedHint == "自定义")
                    {
                        ShowCustomHintDialog();
                    }
                    else
                    {
                        Hint = SelectedHint;
                    }
                }

                // 监听出发车站变化，自动查询车站信息和联想
                if (e.PropertyName == nameof(DepartStationInput))
                {
                    _logService?.Info("TrainTicketFormViewModelBase", $"[DEBUG] DepartStationInput 属性变更，新值: '{DepartStationInput}'");
                    _logService?.Info("TrainTicketFormViewModelBase", $"[DEBUG] 状态检查: _isProcessingLinkedChanges={_isProcessingLinkedChanges}, _isUndoingOrRedoing={_isUndoingOrRedoing}, _isLoadingDefaults={_isLoadingDefaults}, _isLoadingExistingData={_isLoadingExistingData}, _isSaving={_isSaving}, _isApplyingRescheduleData={_isApplyingRescheduleData}");
                    
                    if (!_isProcessingLinkedChanges && !_isUndoingOrRedoing && !_isLoadingDefaults && !_isLoadingExistingData && !_isSaving && !_isApplyingRescheduleData)
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
                    _logService?.Info("TrainTicketFormViewModelBase", $"[DEBUG] ArriveStationInput 属性变更，新值: '{ArriveStationInput}'");
                    _logService?.Info("TrainTicketFormViewModelBase", $"[DEBUG] 状态检查: _isProcessingLinkedChanges={_isProcessingLinkedChanges}, _isUndoingOrRedoing={_isUndoingOrRedoing}, _isLoadingDefaults={_isLoadingDefaults}, _isLoadingExistingData={_isLoadingExistingData}, _isSaving={_isSaving}, _isApplyingRescheduleData={_isApplyingRescheduleData}");
                    
                    if (!_isProcessingLinkedChanges && !_isUndoingOrRedoing && !_isLoadingDefaults && !_isLoadingExistingData && !_isSaving && !_isApplyingRescheduleData)
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
                {
                    if (!_isProcessingLinkedChanges && !_isUndoingOrRedoing && !_isLoadingDefaults && !_isLoadingExistingData)
                    {
                        _isProcessingLinkedChanges = true;
                        _businessRuleEngine.HandleTicketTypeMutex(_formData, e.PropertyName);
                        SyncFromFormData();
                        _isProcessingLinkedChanges = false;
                    }
                }

                // 处理支付渠道互斥
                var paymentProperties = new[] { nameof(IsAlipay), nameof(IsWeChat), nameof(IsABC), nameof(IsCCB), nameof(IsICBC), nameof(IsBCOM), nameof(IsCMB), nameof(IsPSBC), nameof(IsBOC) };
                if (paymentProperties.Contains(e.PropertyName))
                {
                    if (!_isProcessingLinkedChanges && !_isUndoingOrRedoing && !_isLoadingDefaults && !_isLoadingExistingData)
                    {
                        _isProcessingLinkedChanges = true;
                        _businessRuleEngine.HandlePaymentChannelMutex(_formData, e.PropertyName);
                        SyncFromFormData();
                        _isProcessingLinkedChanges = false;
                    }
                }

                // 执行业务规则
                if (!_isProcessingLinkedChanges && !_isUndoingOrRedoing && !_isLoadingDefaults && !_isLoadingExistingData)
                {
                    _isProcessingLinkedChanges = true;
                    var modified = _businessRuleEngine.Execute(_formData, e.PropertyName, _optionsProvider.TicketPurposeOptions);
                    if (modified)
                    {
                        SyncFromFormData();
                    }
                    _isProcessingLinkedChanges = false;
                }

                CheckForChanges();
                UpdateUndoRedoCommands();
            };
        }

        /// <summary>
        /// 同步属性到FormData
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
            if (string.IsNullOrEmpty(propertyName) || propertyName == nameof(CoachNoInput))
                _formData.CoachNoInput = CoachNoInput;
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
        /// 从FormData同步到属性
        /// </summary>
        private void SyncFromFormData()
        {
            SelectedTrainNoPrefix = _formData.SelectedTrainNoPrefix;
            TrainNoNumber = _formData.TrainNoNumber;
            DepartStationInput = _formData.DepartStationInput;
            ArriveStationInput = _formData.ArriveStationInput;
            DepartDateTime = _formData.DepartDateTime;
            DepartTimeValue = _formData.DepartTimeValue;
            CoachNoInput = _formData.CoachNoInput;
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
            _logService?.Info("TrainTicketFormViewModelBase", $"[SyncFromFormData] SelectedTagIds 已同步: [{string.Join(",", SelectedTagIds)}]");
        }

        /// <summary>
        /// 撤销重做状态恢复回调
        /// </summary>
        private void OnUndoRedoStateRestored(FormState state, bool isUndo)
        {
            _isUndoingOrRedoing = true;
            try
            {
                state.ApplyTo(_formData);
                SyncFromFormData();

                if (isUndo)
                    MarkOperationAsUndone(state.PropertyName);
                else
                    UnmarkOperationAsUndone(state.PropertyName);

                CheckForChanges();
            }
            finally
            {
                _isUndoingOrRedoing = false;
            }
        }

        /// <summary>
        /// 撤销重做状态保存回调
        /// </summary>
        private void OnUndoRedoStateSaved(FormState state)
        {
            // 状态已保存，可以在这里添加额外逻辑
        }

        /// <summary>
        /// 更新撤销重做命令状态
        /// </summary>
        private void UpdateUndoRedoCommands()
        {
            // 实时读取配置，确保设置变更后立即生效
            var enableUndo = _generalSettingsService.Config.EnableUndo;
            var canUndo = _undoRedoManager.CanUndo && enableUndo;
            var canRedo = _undoRedoManager.CanRedo && enableUndo;

            _logService?.Info("TrainTicketFormViewModelBase", $"UpdateUndoRedoCommands: EnableUndo={enableUndo}, Manager.CanUndo={_undoRedoManager.CanUndo}, CanUndo={canUndo}");

            CanUndo = canUndo;
            CanRedo = canRedo;

            // 通知命令的 CanExecute 状态已变更
            UndoCommand.NotifyCanExecuteChanged();
            RedoCommand.NotifyCanExecuteChanged();
        }

        /// <summary>
        /// 刷新撤销重做设置（在设置变更后调用）
        /// </summary>
        public void RefreshUndoRedoSettings()
        {
            _logService?.Info("TrainTicketFormViewModelBase", "RefreshUndoRedoSettings 被调用");

            // 重新加载配置
            _generalSettingsService.RefreshConfig();

            _logService?.Info("TrainTicketFormViewModelBase", $"刷新后 EnableUndo={_generalSettingsService.Config.EnableUndo}, MaxUndoSteps={_generalSettingsService.Config.MaxUndoSteps}");

            // 更新最大撤销步数
            _undoRedoManager.Initialize(_generalSettingsService.Config.MaxUndoSteps);

            // 更新命令状态
            UpdateUndoRedoCommands();
        }

        /// <summary>
        /// 更新座位字母选项
        /// </summary>
        private void UpdateSeatLetterOptions()
        {
            SeatLetterOptions = _optionsProvider.GetSeatLetterOptions(SeatType);
            IsSeatLetterVisible = _optionsProvider.IsSeatLetterVisible(SeatType);
            IsSeatLetterEnabled = !IsNoSeat && IsSeatLetterVisible;

            if (SeatLetterOptions.Count > 0 && !SeatLetterOptions.Contains(SelectedSeatLetter))
            {
                SelectedSeatLetter = SeatLetterOptions[0];
            }
            else if (SeatLetterOptions.Count == 0)
            {
                SelectedSeatLetter = string.Empty;
            }
        }

        /// <summary>
        /// 异步查询出发车站信息
        /// </summary>
        protected async Task QueryDepartStationInfoAsync()
        {
            if (string.IsNullOrWhiteSpace(DepartStationInput))
            {
                DepartStationCode = string.Empty;
                DepartStationPinyin = string.Empty;
                return;
            }

            try
            {
                var station = await _stationQueryService.QueryStationAsync(DepartStationInput);
                if (station != null)
                {
                    DepartStationCode = station.StationCode ?? string.Empty;
                    DepartStationPinyin = station.StationPinyin ?? string.Empty;
                    _logService?.Info("TrainTicketFormViewModelBase", $"查询出发车站成功: {DepartStationInput} -> 代码:{DepartStationCode}, 拼音:{DepartStationPinyin}");
                }
                else
                {
                    DepartStationCode = string.Empty;
                    DepartStationPinyin = string.Empty;
                    _logService?.Info("TrainTicketFormViewModelBase", $"未找到出发车站: {DepartStationInput}");
                }
            }
            catch (Exception ex)
            {
                _logService?.Error("TrainTicketFormViewModelBase", $"查询出发车站失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 异步查询到达车站信息
        /// </summary>
        protected async Task QueryArriveStationInfoAsync()
        {
            if (string.IsNullOrWhiteSpace(ArriveStationInput))
            {
                ArriveStationCode = string.Empty;
                ArriveStationPinyin = string.Empty;
                return;
            }

            try
            {
                var station = await _stationQueryService.QueryStationAsync(ArriveStationInput);
                if (station != null)
                {
                    ArriveStationCode = station.StationCode ?? string.Empty;
                    ArriveStationPinyin = station.StationPinyin ?? string.Empty;
                    _logService?.Info("TrainTicketFormViewModelBase", $"查询到达车站成功: {ArriveStationInput} -> 代码:{ArriveStationCode}, 拼音:{ArriveStationPinyin}");
                }
                else
                {
                    ArriveStationCode = string.Empty;
                    ArriveStationPinyin = string.Empty;
                    _logService?.Info("TrainTicketFormViewModelBase", $"未找到到达车站: {ArriveStationInput}");
                }
            }
            catch (Exception ex)
            {
                _logService?.Error("TrainTicketFormViewModelBase", $"查询到达车站失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 异步搜索出发车站联想建议
        /// </summary>
        private async Task SearchDepartStationSuggestionsAsync()
        {
            _logService?.Info("TrainTicketFormViewModelBase", $"[DEBUG] SearchDepartStationSuggestionsAsync 开始执行，输入: '{DepartStationInput}'");
            
            if (string.IsNullOrWhiteSpace(DepartStationInput) || DepartStationInput.Length < 1)
            {
                _logService?.Info("TrainTicketFormViewModelBase", "[DEBUG] 输入为空或长度小于1，清空建议并关闭下拉框");
                if (DepartStationSuggestions.Count > 0)
                    DepartStationSuggestions.Clear();
                IsDepartStationDropdownOpen = false;
                return;
            }

            try
            {
                _logService?.Info("TrainTicketFormViewModelBase", $"[DEBUG] 开始搜索车站，关键词: '{DepartStationInput}'");
                var suggestions = await _stationQueryService.SmartSearchStationNamesAsync(DepartStationInput);
                _logService?.Info("TrainTicketFormViewModelBase", $"[DEBUG] 搜索完成，找到 {suggestions.Count} 个建议");
                
                // 使用临时集合避免多次触发CollectionChanged事件
                var newSuggestions = new ObservableCollection<string>(suggestions);
                DepartStationSuggestions = newSuggestions;
                OnPropertyChanged(nameof(DepartStationSuggestions));
                
                // 有建议时打开下拉框
                bool shouldOpen = DepartStationSuggestions.Count > 0;
                _logService?.Info("TrainTicketFormViewModelBase", $"[DEBUG] 建议数量: {DepartStationSuggestions.Count}, 是否打开下拉框: {shouldOpen}");
                IsDepartStationDropdownOpen = shouldOpen;
                DepartStationSelectedIndex = -1;
                
                _logService?.Info("TrainTicketFormViewModelBase", $"[DEBUG] IsDepartStationDropdownOpen 设置为: {IsDepartStationDropdownOpen}");
            }
            catch (Exception ex)
            {
                _logService?.Error("TrainTicketFormViewModelBase", $"[DEBUG] 搜索出发车站联想失败: {ex.Message}");
                _logService?.Error("TrainTicketFormViewModelBase", $"[DEBUG] 异常详情: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// 异步搜索到达车站联想建议
        /// </summary>
        private async Task SearchArriveStationSuggestionsAsync()
        {
            _logService?.Info("TrainTicketFormViewModelBase", $"[DEBUG] SearchArriveStationSuggestionsAsync 开始执行，输入: '{ArriveStationInput}'");
            
            if (string.IsNullOrWhiteSpace(ArriveStationInput) || ArriveStationInput.Length < 1)
            {
                _logService?.Info("TrainTicketFormViewModelBase", "[DEBUG] 输入为空或长度小于1，清空建议并关闭下拉框");
                if (ArriveStationSuggestions.Count > 0)
                    ArriveStationSuggestions.Clear();
                IsArriveStationDropdownOpen = false;
                return;
            }

            try
            {
                _logService?.Info("TrainTicketFormViewModelBase", $"[DEBUG] 开始搜索车站，关键词: '{ArriveStationInput}'");
                var suggestions = await _stationQueryService.SmartSearchStationNamesAsync(ArriveStationInput);
                _logService?.Info("TrainTicketFormViewModelBase", $"[DEBUG] 搜索完成，找到 {suggestions.Count} 个建议");
                
                // 使用临时集合避免多次触发CollectionChanged事件
                var newSuggestions = new ObservableCollection<string>(suggestions);
                ArriveStationSuggestions = newSuggestions;
                OnPropertyChanged(nameof(ArriveStationSuggestions));
                
                // 有建议时打开下拉框
                bool shouldOpen = ArriveStationSuggestions.Count > 0;
                _logService?.Info("TrainTicketFormViewModelBase", $"[DEBUG] 建议数量: {ArriveStationSuggestions.Count}, 是否打开下拉框: {shouldOpen}");
                IsArriveStationDropdownOpen = shouldOpen;
                ArriveStationSelectedIndex = -1;
                
                _logService?.Info("TrainTicketFormViewModelBase", $"[DEBUG] IsArriveStationDropdownOpen 设置为: {IsArriveStationDropdownOpen}");
            }
            catch (Exception ex)
            {
                _logService?.Error("TrainTicketFormViewModelBase", $"[DEBUG] 搜索到达车站联想失败: {ex.Message}");
                _logService?.Error("TrainTicketFormViewModelBase", $"[DEBUG] 异常详情: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// 出发车站文本改变命令（供 AutoCompleteTextBox 使用）
        /// </summary>
        [RelayCommand]
        private async Task DepartStationTextChanged(string keyword)
        {
            DepartStationInput = keyword;
            await SearchDepartStationSuggestionsAsync();
        }

        /// <summary>
        /// 到达车站文本改变命令（供 AutoCompleteTextBox 使用）
        /// </summary>
        [RelayCommand]
        private async Task ArriveStationTextChanged(string keyword)
        {
            ArriveStationInput = keyword;
            await SearchArriveStationSuggestionsAsync();
        }

        /// <summary>
        /// 选择出发车站联想项
        /// </summary>
        [RelayCommand]
        public void SelectDepartStation(string suggestion)
        {
            if (string.IsNullOrEmpty(suggestion))
                return;

            _isProcessingLinkedChanges = true;
            try
            {
                DepartStationInput = suggestion;
                IsDepartStationDropdownOpen = false;
                DepartStationSuggestions.Clear();
                
                // 触发车站信息查询
                _ = QueryDepartStationInfoAsync();
            }
            finally
            {
                _isProcessingLinkedChanges = false;
            }
        }

        /// <summary>
        /// 选择到达车站联想项
        /// </summary>
        [RelayCommand]
        public void SelectArriveStation(string suggestion)
        {
            if (string.IsNullOrEmpty(suggestion))
                return;

            _isProcessingLinkedChanges = true;
            try
            {
                ArriveStationInput = suggestion;
                IsArriveStationDropdownOpen = false;
                ArriveStationSuggestions.Clear();
                
                // 触发车站信息查询
                _ = QueryArriveStationInfoAsync();
            }
            finally
            {
                _isProcessingLinkedChanges = false;
            }
        }

        /// <summary>
        /// 显示自定义提示信息对话框
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
        /// 加载默认配置值
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
                _logService?.Info("TrainTicketFormViewModelBase", $"[LoadFromTrainRide] SyncFromFormData 后 SelectedTagIds: [{string.Join(",", SelectedTagIds)}]");
                
                UpdateSeatLetterOptions();
                BackupOriginalValues();
                _logService?.Info("TrainTicketFormViewModelBase", $"[LoadFromTrainRide] BackupOriginalValues 后 _originalFormData.SelectedTagIds: [{string.Join(",", _originalFormData.SelectedTagIds)}]");
                
                // 触发 SelectedTagIds 属性变更通知，让 UI 更新标签视觉状态
                OnPropertyChanged(nameof(SelectedTagIds));
                _logService?.Info("TrainTicketFormViewModelBase", "[LoadFromTrainRide] 已触发 SelectedTagIds PropertyChanged 事件");
            }
            finally
            {
                _isLoadingDefaults = false;
                _undoRedoManager.SetInitialState(FormState.FromFormData(_formData.Clone(), string.Empty));
            }
        }

        /// <summary>
        /// 加载默认标签
        /// </summary>
        private async void LoadDefaultTags()
        {
            try
            {
                var defaultTags = await _ticketTagRepository.GetDefaultTagsAsync();
                if (defaultTags.Any())
                {
                    foreach (var tag in defaultTags)
                    {
                        if (!SelectedTagIds.Contains(tag.Id))
                        {
                            SelectedTagIds.Add(tag.Id);
                        }
                    }
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
        /// 备份原始值
        /// </summary>
        protected void BackupOriginalValues()
        {
            _originalFormData = _formData.Clone();
            HasUnsavedChanges = false;
        }

        /// <summary>
        /// 检查是否有未保存的更改
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
            {
                _logService?.Info("TrainTicketFormViewModelBase", $"CheckForChanges: HasUnsavedChanges 从 {HasUnsavedChanges} 变为 {hasChanges}");
            }
            HasUnsavedChanges = hasChanges;
        }

        /// <summary>
        /// 比较两个表单数据是否相等
        /// </summary>
        private bool AreFormDataEqual(TrainTicketFormData a, TrainTicketFormData b)
        {
            return a.TrainNoNumber == b.TrainNoNumber &&
                   a.SelectedTrainNoPrefix == b.SelectedTrainNoPrefix &&
                   a.DepartStationInput == b.DepartStationInput &&
                   a.ArriveStationInput == b.ArriveStationInput &&
                   a.DepartDateTime == b.DepartDateTime &&
                   a.DepartTimeValue == b.DepartTimeValue &&
                   a.CoachNoInput == b.CoachNoInput &&
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
                   Enumerable.SequenceEqual(a.SelectedTagIds.OrderBy(x => x), b.SelectedTagIds.OrderBy(x => x));
        }

        /// <summary>
        /// 添加操作历史记录
        /// </summary>
        private void AddOperationHistory(string propertyName)
        {
            var description = GetPropertyDescription(propertyName);
            var newValue = GetPropertyValue(propertyName);

            var item = new OperationHistoryItem
            {
                Index = OperationHistory.Count + 1,
                PropertyName = propertyName,
                Description = description,
                NewValue = newValue,
                Timestamp = DateTime.Now,
                IsUndone = false
            };

            OperationHistory.Insert(0, item);

            var maxSteps = _generalSettingsService.Config.MaxUndoSteps;
            while (OperationHistory.Count > maxSteps && maxSteps > 0)
            {
                OperationHistory.RemoveAt(OperationHistory.Count - 1);
            }
        }

        /// <summary>
        /// 获取属性描述
        /// </summary>
        private string GetPropertyDescription(string propertyName)
        {
            return propertyName switch
            {
                nameof(TrainNoNumber) => "修改车次号",
                nameof(SelectedTrainNoPrefix) => "修改车次前缀",
                nameof(DepartStationInput) => "修改出发车站",
                nameof(ArriveStationInput) => "修改到达车站",
                nameof(DepartDateTime) => "修改出发日期",
                nameof(DepartTimeValue) => "修改出发时间",
                nameof(CoachNoInput) => "修改车厢号",
                nameof(SeatNoNumber) => "修改座位号",
                nameof(SelectedSeatLetter) => "修改座位字母",
                nameof(IsNoSeat) => "修改无座状态",
                nameof(MoneyText) => "修改金额",
                nameof(SeatType) => "修改席别",
                nameof(AdditionalInfo) => "修改附加信息",
                nameof(TicketPurpose) => "修改车票用途",
                nameof(TicketModificationType) => "修改改签类型",
                nameof(Hint) => "修改提示信息",
                nameof(SelectedStatus) => "修改状态",
                nameof(IsStudentTicket) => "修改学生票",
                nameof(IsDiscountTicket) => "修改优惠票",
                nameof(IsOnlineTicket) => "修改网络售票",
                nameof(IsChildTicket) => "修改儿童票",
                nameof(IsAlipay) => "修改支付宝",
                nameof(IsWeChat) => "修改微信",
                nameof(IsABC) => "修改农业银行",
                nameof(IsCCB) => "修改建设银行",
                nameof(IsICBC) => "修改工商银行",
                nameof(IsBCOM) => "修改交通银行",
                nameof(IsCMB) => "修改招商银行",
                nameof(IsPSBC) => "修改邮储银行",
                nameof(IsBOC) => "修改中国银行",
                nameof(TicketNumber) => "修改取票号",
                nameof(CheckInLocation) => "修改检票位置",
                nameof(SelectedTagIds) => "修改标签",
                _ => $"修改 {propertyName}"
            };
        }

        /// <summary>
        /// 获取属性当前值
        /// </summary>
        private string GetPropertyValue(string propertyName)
        {
            return propertyName switch
            {
                nameof(TrainNoNumber) => TrainNoNumber ?? string.Empty,
                nameof(SelectedTrainNoPrefix) => SelectedTrainNoPrefix ?? string.Empty,
                nameof(DepartStationInput) => DepartStationInput ?? string.Empty,
                nameof(ArriveStationInput) => ArriveStationInput ?? string.Empty,
                nameof(DepartDateTime) => DepartDateTime?.ToString("yyyy-MM-dd") ?? string.Empty,
                nameof(DepartTimeValue) => DepartTimeValue?.ToString("HH:mm") ?? string.Empty,
                nameof(CoachNoInput) => CoachNoInput ?? string.Empty,
                nameof(SeatNoNumber) => SeatNoNumber ?? string.Empty,
                nameof(SelectedSeatLetter) => SelectedSeatLetter ?? string.Empty,
                nameof(IsNoSeat) => IsNoSeat ? "是" : "否",
                nameof(MoneyText) => MoneyText ?? string.Empty,
                nameof(SeatType) => SeatType ?? string.Empty,
                nameof(AdditionalInfo) => AdditionalInfo ?? string.Empty,
                nameof(TicketPurpose) => TicketPurpose ?? string.Empty,
                nameof(TicketModificationType) => TicketModificationType ?? string.Empty,
                nameof(Hint) => Hint ?? string.Empty,
                nameof(SelectedStatus) => SelectedStatus ?? string.Empty,
                nameof(IsStudentTicket) => IsStudentTicket ? "是" : "否",
                nameof(IsDiscountTicket) => IsDiscountTicket ? "是" : "否",
                nameof(IsOnlineTicket) => IsOnlineTicket ? "是" : "否",
                nameof(IsChildTicket) => IsChildTicket ? "是" : "否",
                nameof(IsAlipay) => IsAlipay ? "是" : "否",
                nameof(IsWeChat) => IsWeChat ? "是" : "否",
                nameof(IsABC) => IsABC ? "是" : "否",
                nameof(IsCCB) => IsCCB ? "是" : "否",
                nameof(IsICBC) => IsICBC ? "是" : "否",
                nameof(IsBCOM) => IsBCOM ? "是" : "否",
                nameof(IsCMB) => IsCMB ? "是" : "否",
                nameof(IsPSBC) => IsPSBC ? "是" : "否",
                nameof(IsBOC) => IsBOC ? "是" : "否",
                nameof(TicketNumber) => TicketNumber ?? string.Empty,
                nameof(CheckInLocation) => CheckInLocation ?? string.Empty,
                nameof(SelectedTagIds) => GetSelectedTagsDisplayValue(),
                _ => string.Empty
            };
        }

        /// <summary>
        /// 获取已选标签的显示值
        /// </summary>
        private string GetSelectedTagsDisplayValue()
        {
            if (SelectedTagIds == null || SelectedTagIds.Count == 0)
                return "无标签";

            var tagNames = new List<string>();
            foreach (var tagId in SelectedTagIds)
            {
                var tag = AvailableTags?.FirstOrDefault(t => t.Id == tagId);
                if (tag != null)
                {
                    tagNames.Add(tag.Name);
                }
            }

            return tagNames.Count > 0 ? string.Join(", ", tagNames) : "无标签";
        }

        /// <summary>
        /// 标记操作为已撤销
        /// </summary>
        private void MarkOperationAsUndone(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName)) return;

            // 查找最新的未撤销记录（列表最前面的是最新的）
            var item = OperationHistory.FirstOrDefault(h => h.PropertyName == propertyName && !h.IsUndone);
            if (item != null)
            {
                item.IsUndone = true;
                _logService?.Info("TrainTicketFormViewModelBase", $"标记操作为已撤销: {propertyName}");
            }
        }

        /// <summary>
        /// 取消标记操作为已撤销
        /// </summary>
        private void UnmarkOperationAsUndone(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName)) return;

            // 查找最新的已撤销记录（列表最前面的是最新的）
            var item = OperationHistory.FirstOrDefault(h => h.PropertyName == propertyName && h.IsUndone);
            if (item != null)
            {
                item.IsUndone = false;
                _logService?.Info("TrainTicketFormViewModelBase", $"取消标记操作为已撤销: {propertyName}");
            }
        }

        /// <summary>
        /// 切换操作历史面板展开/折叠
        /// </summary>
        [RelayCommand]
        public void ToggleOperationHistory()
        {
            IsOperationHistoryExpanded = !IsOperationHistoryExpanded;
        }

        /// <summary>
        /// 切换标签选择状态
        /// </summary>
        [RelayCommand]
        public void ToggleTagSelection(int tagId)
        {
            _logService?.Info("TrainTicketFormViewModelBase", $"[ToggleTagSelection] 开始: tagId={tagId}");
            _logService?.Info("TrainTicketFormViewModelBase", $"[ToggleTagSelection] 操作前 SelectedTagIds.Count={SelectedTagIds.Count}");
            
            // 记录操作前状态（UndoRedoManager只保存变更前的状态）
            // 注意：SelectedTagIds 不在 _formFieldNames 中，不会触发自动撤销重做
            _undoRedoManager.BeginPropertyChange(nameof(SelectedTagIds));
            AddOperationHistory(nameof(SelectedTagIds));

            if (SelectedTagIds.Contains(tagId))
            {
                _logService?.Info("TrainTicketFormViewModelBase", $"[ToggleTagSelection] 移除标签 {tagId}");
                SelectedTagIds.Remove(tagId);
            }
            else
            {
                _logService?.Info("TrainTicketFormViewModelBase", $"[ToggleTagSelection] 添加标签 {tagId}");
                SelectedTagIds.Add(tagId);
            }

            _logService?.Info("TrainTicketFormViewModelBase", $"[ToggleTagSelection] 操作后 SelectedTagIds.Count={SelectedTagIds.Count}");
            
            // 触发属性变更通知，让 PropertyChanged 事件处理器同步到 FormData
            OnPropertyChanged(nameof(SelectedTagIds));
            
            _logService?.Info("TrainTicketFormViewModelBase", "[ToggleTagSelection] 完成: OnPropertyChanged 已触发");
        }

        /// <summary>
        /// 撤销命令
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanUndo))]
        public void Undo()
        {
            _undoRedoManager.Undo();
            UpdateUndoRedoCommands();
        }

        /// <summary>
        /// 重做命令
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanRedo))]
        public void Redo()
        {
            _undoRedoManager.Redo();
            UpdateUndoRedoCommands();
        }

        /// <summary>
        /// 放弃更改，恢复到原始值
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
        /// 从现有车票加载数据（编辑模式使用）
        /// </summary>
        public virtual void LoadFromTrainRide(TrainRideInfo trainRide)
        {
            if (trainRide == null) return;

            _logService?.Info("TrainTicketFormViewModelBase", $"[LoadFromTrainRide] 开始加载车票 ID={trainRide.Id}");
            _logService?.Info("TrainTicketFormViewModelBase", $"[LoadFromTrainRide] 实体标签数量: {trainRide.Tags?.Count ?? 0}");
            if (trainRide.Tags != null && trainRide.Tags.Any())
            {
                _logService?.Info("TrainTicketFormViewModelBase", $"[LoadFromTrainRide] 实体标签 IDs: [{string.Join(",", trainRide.Tags.Select(t => t.Id))}]");
            }

            _isLoadingExistingData = true;
            try
            {
                EditTicketId = trainRide.Id;

                // 使用 DataTransformer 进行转换
                var data = _dataTransformer.FromEntity(trainRide);
                _logService?.Info("TrainTicketFormViewModelBase", $"[LoadFromTrainRide] 转换后 data.SelectedTagIds: [{string.Join(",", data.SelectedTagIds)}]");
                
                data.CopyTo(_formData);
                _logService?.Info("TrainTicketFormViewModelBase", $"[LoadFromTrainRide] CopyTo 后 _formData.SelectedTagIds: [{string.Join(",", _formData.SelectedTagIds)}]");

                // 确保自定义提示信息在选项列表中
                if (!string.IsNullOrEmpty(_formData.Hint))
                {
                    _optionsProvider.EnsureHintInOptions(_formData.Hint);
                }

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
        /// 创建 TrainRideInfo 对象
        /// </summary>
        protected virtual TrainRideInfo CreateTrainRideInfo()
        {
            return _dataTransformer.ToEntity(_formData, EditTicketId ?? 0);
        }

        /// <summary>
        /// 验证表单数据
        /// </summary>
        protected virtual bool ValidateForm()
        {
            var result = _formValidator.Validate(_formData);

            if (!result.IsValid)
            {
                MessageBoxWindow.Show(result.Errors.First().ErrorMessage, "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        /// <summary>
        /// 检查是否有必填项未填写
        /// </summary>
        public bool HasRequiredFieldsEmpty()
        {
            return _formValidator.HasRequiredFieldsEmpty(_formData);
        }

        /// <summary>
        /// 获取未填写的必填项列表
        /// </summary>
        public List<string> GetEmptyRequiredFields()
        {
            return _formValidator.GetEmptyRequiredFields(_formData);
        }

        /// <summary>
        /// 异步加载车站列表
        /// </summary>
        protected async Task LoadStationsAsync()
        {
            var stations = await _stationRepository.GetAllStationsAsync();
            foreach (var station in stations)
            {
                StationNames.Add(station.StationName);
            }
        }

        /// <summary>
        /// 异步加载标签列表
        /// </summary>
        protected async Task LoadTagsAsync()
        {
            try
            {
                var tags = await _ticketTagRepository.GetAllTagsAsync();
                foreach (var tag in tags)
                {
                    AvailableTags.Add(tag);
                }
            }
            catch (Exception ex)
            {
                _logService?.Error("TrainTicketFormViewModelBase", $"加载标签失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 保存命令
        /// </summary>
        [RelayCommand]
        protected async Task SaveAsync()
        {
            _isSaving = true;
            try
            {
                await ExecuteSaveAsync();
                // 只有在保存成功后才重置未保存更改标志
                BackupOriginalValues();
            }
            finally
            {
                _isSaving = false;
            }
        }

        /// <summary>
        /// 执行保存操作（子类实现具体逻辑）
        /// </summary>
        protected abstract Task ExecuteSaveAsync();

        /// <summary>
        /// 保存标签关联（公共方法）
        /// </summary>
        protected async Task SaveTagsAsync(int ticketId)
        {
            // 无论是否有选中标签，都要更新数据库（空列表表示删除所有标签）
            var tagIdsToSave = SelectedTagIds ?? new ObservableCollection<int>();
            await _ticketTagRepository.SetTagsToRideAsync(ticketId, tagIdsToSave);
        }

        /// <summary>
        /// 显示保存成功消息并关闭窗口
        /// </summary>
        protected void ShowSaveSuccessAndClose(string message)
        {
            // 发送车票保存成功消息，通知行程列表刷新
            WeakReferenceMessenger.Default.Send(new TicketSavedMessage
            {
                TicketId = EditTicketId,
                IsEditMode = IsEditMode,
                TrainNo = TrainNo
            });

            MessageBoxWindow.Show(message, "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            CloseWindow();
        }

        /// <summary>
        /// 显示保存失败消息
        /// </summary>
        protected void ShowSaveError(string operation, string errorMessage)
        {
            _logService?.Error(GetType().Name, $"{operation}火车票失败: {errorMessage}");
            MessageBoxWindow.Show($"{operation}失败：{errorMessage}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        /// <summary>
        /// 记录保存日志
        /// </summary>
        protected void LogSaveOperation(string operation)
        {
            _logService?.Info(GetType().Name, $"{operation}火车票: {TrainNo} {DepartStation}->{ArriveStation}");
        }

        /// <summary>
        /// 取消命令
        /// </summary>
        [RelayCommand]
        protected void Cancel()
        {
            CloseWindow();
        }

        /// <summary>
        /// 关闭窗口
        /// </summary>
        protected void CloseWindow()
        {
            var window = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.DataContext == this);
            window?.Close();
        }
    }
}
