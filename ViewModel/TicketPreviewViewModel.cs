using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GuiPiao.Model;
using GuiPiao.Services;
using GuiPiao.Utils;
using GuiPiao.ViewModel.TrainTicketForm;
using Microsoft.Win32;
using TripRecord = GuiPiao.Model.TripItem;

namespace GuiPiao.ViewModel;

/// <summary>
///     车票预览（文档：811×509 票面 + ScrollViewer 缩放、红蓝布局表、导出 PNG、QRCoder）。
/// </summary>
public partial class TicketPreviewViewModel : ObservableObject
{
    private readonly UISettingsService _uiSettingsService;
    private double _ticketPreviewHostWidth = 1000;
    private double _ticketPreviewHostHeight = 700;
    private TicketPreviewDraft? _draftPropertySubscription;
    private readonly PropertyChangedEventHandler? _draftHandler;
    private TripRecord? _tripSourceSubscribed;
    private bool _paymentChannelUiSync;
    private bool _ticketTypeUiSync;

    /// <summary>与录入表单 <see cref="OptionsProvider" /> 一致的席别/附加信息/用途/改签下拉项。</summary>
    private readonly OptionsProvider _previewTripFieldOptions = new();

    /// <summary>支付渠道：在线支付下拉项（空=无；支付宝与微信二选一）。</summary>
    public ObservableCollection<string> PreviewPaymentOnlineOptions { get; } = new()
    {
        string.Empty,
        "支付宝",
        "微信"
    };

    /// <summary>支付渠道：银行下拉项（空=无；七家银行单选）。首项为空可与在线支付组合。</summary>
    public ObservableCollection<string> PreviewPaymentBankOptions { get; } = new();

    private static readonly string[] PaymentBankTokens =
    {
        "农业银行", "建设银行", "工商银行", "交通银行", "招商银行", "邮储银行", "中国银行"
    };

    public ObservableCollection<string> PreviewSeatTypeOptions => _previewTripFieldOptions.SeatTypeOptions;

    public ObservableCollection<string> PreviewAdditionalInfoOptions => _previewTripFieldOptions.AdditionalInfoOptions;

    public ObservableCollection<string> PreviewTicketPurposeOptions => _previewTripFieldOptions.TicketPurposeOptions;

    public ObservableCollection<string> PreviewTicketModificationTypeOptions =>
        _previewTripFieldOptions.TicketModificationTypeOptions;

    public TicketPreviewViewModel()
    {
        _uiSettingsService = new UISettingsService();
        _layoutBlue = ObservableTicketFaceLayout.FromTemplate(TicketFaceLayout.BlueDefault());
        _layoutRed = ObservableTicketFaceLayout.FromTemplate(TicketFaceLayout.RedDefault());
        WireLayoutObservers();
        TryLoadLayoutFromDefaultPath();
        if (SelectedLayoutElementItem == null)
            SelectedLayoutElementItem = LayoutElementItems[0];
        PullEditorFromLayout();
        _draftHandler = (_, args) =>
        {
            if (args.PropertyName == nameof(TicketPreviewDraft.IdNumber)) TryValidateIdentityDigits();
            if (args.PropertyName == nameof(TicketPreviewDraft.PreferDiscountZhe)) RebuildBadgeLetters();
        };
        ThemeManager.ThemeChanged += OnAppThemeChanged;
        LoadSettings();
        RefreshTicketBackgroundImage();
        RefreshTicketBackgroundOpacity();
        RegenerateQr();
        PreviewPaymentBankOptions.Add(string.Empty);
        foreach (var b in PaymentBankTokens)
            PreviewPaymentBankOptions.Add(b);
    }

    private void OnAppThemeChanged(object? sender, EventArgs e)
    {
        RefreshTicketBackgroundOpacity();
    }

    [ObservableProperty] private ObservableCollection<string> _paymentBadgeLetters = new();

    /// <summary>票面「学」简字是否显示（票种含学生）。</summary>
    [ObservableProperty] private bool _showTicketBadgeXue;

    [ObservableProperty] private bool _showTicketBadgeHai;

    [ObservableProperty] private bool _showTicketBadgeWang;

    [ObservableProperty] private bool _showTicketBadgeDiscount;

    /// <summary>优惠票简字：折或惠（与草稿 PreferDiscountZhe 一致）。</summary>
    [ObservableProperty] private string _ticketDiscountBadgeChar = "折";

    [ObservableProperty] private bool _hasPaymentBadgeLetters;

    [ObservableProperty] private ObservableCollection<TicketPreviewDraft> _previewDrafts = new();

    [ObservableProperty] private TicketPreviewDraft? _selectedDraft;

    /// <summary>红票（暖色底图）</summary>
    [ObservableProperty] private bool _isRedTicket;

    /// <summary>打开场景：布局工作台（设置）或主界面行程预览。</summary>
    [ObservableProperty] private TicketPreviewSessionMode _sessionMode = TicketPreviewSessionMode.UserTripPreview;

    /// <summary>票种/支付简字带框</summary>
    [ObservableProperty] private bool _showFramedTicketBadges = true;

    /// <summary>与 <see cref="PreviewPaymentOnlineOptions" /> 同步；写入 <see cref="TripItem.PaymentChannel" />。</summary>
    [ObservableProperty] private string _previewPaymentOnlineSelection = string.Empty;

    /// <summary>与 <see cref="PreviewPaymentBankOptions" /> 同步；写入 <see cref="TripItem.PaymentChannel" />。</summary>
    [ObservableProperty] private string _previewPaymentBankSelection = string.Empty;

    /// <summary>票种：学生票（与儿童票互斥）。</summary>
    [ObservableProperty] private bool _previewTicketFlagStudent;

    /// <summary>票种：儿童票（与学生票互斥）。</summary>
    [ObservableProperty] private bool _previewTicketFlagChild;

    /// <summary>票种：优惠票。</summary>
    [ObservableProperty] private bool _previewTicketFlagDiscount;

    /// <summary>票种：网络售票。</summary>
    [ObservableProperty] private bool _previewTicketFlagOnline;

    /// <summary>编码区文本 → 二维码</summary>
    [ObservableProperty] private string _encodingText = string.Empty;

    [ObservableProperty] private ImageSource? _qrCodeSource;

    [ObservableProperty] private ImageSource? _ticketBackgroundImage;

    /// <summary>深色主题下底图略降不透明度（文档）</summary>
    [ObservableProperty] private double _ticketBackgroundOpacity = 1.0;

    private readonly ObservableTicketFaceLayout _layoutBlue;
    private readonly ObservableTicketFaceLayout _layoutRed;

    public ObservableTicketFaceLayout ActiveLayout => IsRedTicket ? _layoutRed : _layoutBlue;

    public double ArrowCanvasLeft => ActiveLayout.ArrowLeft + (SelectedDraft?.ArrowOffsetAdjustPx ?? 0);

    public bool HasSelectedDraft => SelectedDraft != null;

    public bool HasMultipleDrafts => PreviewDrafts.Count > 1;

    /// <summary>当前草稿出发站去掉「站」后为 1～5 个汉字时，可调校票面字间距与按字数左边距。</summary>
    public bool DepartStationCharacterSpacingAdjustable =>
        SelectedDraft != null &&
        StationFaceHanCountLayout.IsAdjustableHanCount(CurrentDepartStationHanCount);

    /// <summary>当前草稿到达站满足同上条件时，可调校票面字间距与按字数左边距。</summary>
    public bool ArriveStationCharacterSpacingAdjustable =>
        SelectedDraft != null &&
        StationFaceHanCountLayout.IsAdjustableHanCount(CurrentArriveStationHanCount);

    public int CurrentDepartStationHanCount =>
        SelectedDraft == null
            ? 0
            : StationFaceHanCountLayout.GetBodyHanCount(SelectedDraft.Source.DepartStation);

    public int CurrentArriveStationHanCount =>
        SelectedDraft == null
            ? 0
            : StationFaceHanCountLayout.GetBodyHanCount(SelectedDraft.Source.ArriveStation);

    public string WorkbenchDepartStationLayoutCaption =>
        CurrentDepartStationHanCount > 0
            ? $"当前出发站 {CurrentDepartStationHanCount} 字：X 写入该字数左边距（{StationFaceHanCountLayout.ReferenceHanCount} 字档同时更新基准）"
            : string.Empty;

    public string WorkbenchArriveStationLayoutCaption =>
        CurrentArriveStationHanCount > 0
            ? $"当前到达站 {CurrentArriveStationHanCount} 字：X 写入该字数左边距（{StationFaceHanCountLayout.ReferenceHanCount} 字档同时更新基准）"
            : string.Empty;

    /// <summary>票面出发站主体（1～5 汉字时含该字数档布局字间距）。</summary>
    public string PreviewDepartStationText =>
        SelectedDraft == null
            ? string.Empty
            : TicketPreviewDraft.FormatStationNameForPreviewFace(
                SelectedDraft.Source.DepartStation,
                StationFaceHanCountLayout.GetDepartSpacing(ActiveLayout, CurrentDepartStationHanCount));

    /// <summary>票面到达站主体；语义同 <see cref="PreviewDepartStationText" />。</summary>
    public string PreviewArriveStationText =>
        SelectedDraft == null
            ? string.Empty
            : TicketPreviewDraft.FormatStationNameForPreviewFace(
                SelectedDraft.Source.ArriveStation,
                StationFaceHanCountLayout.GetArriveSpacing(ActiveLayout, CurrentArriveStationHanCount));

    /// <summary>出发站整行（站名+站字）在 Canvas 上的 Left：基准 + 当前字数左边距微调。</summary>
    public double PreviewDepartStationCanvasLeft =>
        StationFaceHanCountLayout.GetDepartCanvasLeft(ActiveLayout, CurrentDepartStationHanCount);

    /// <summary>到达站整行 Canvas Left。</summary>
    public double PreviewArriveStationCanvasLeft =>
        StationFaceHanCountLayout.GetArriveCanvasLeft(ActiveLayout, CurrentArriveStationHanCount);

    public int ActiveDepartStationCharacterSpacing
    {
        get
        {
            var han = CurrentDepartStationHanCount;
            return han > 0
                ? StationFaceHanCountLayout.GetDepartSpacing(ActiveLayout, han)
                : ActiveLayout.DepartStationCharacterSpacing;
        }
        set
        {
            var han = CurrentDepartStationHanCount;
            if (han > 0)
                StationFaceHanCountLayout.SetDepartSpacing(ActiveLayout, han, value);
            else
                ActiveLayout.DepartStationCharacterSpacing = value;
            OnPropertyChanged(nameof(ActiveDepartStationCharacterSpacing));
            OnPropertyChanged(nameof(PreviewDepartStationText));
        }
    }

    public int ActiveArriveStationCharacterSpacing
    {
        get
        {
            var han = CurrentArriveStationHanCount;
            return han > 0
                ? StationFaceHanCountLayout.GetArriveSpacing(ActiveLayout, han)
                : ActiveLayout.ArriveStationCharacterSpacing;
        }
        set
        {
            var han = CurrentArriveStationHanCount;
            if (han > 0)
                StationFaceHanCountLayout.SetArriveSpacing(ActiveLayout, han, value);
            else
                ActiveLayout.ArriveStationCharacterSpacing = value;
            OnPropertyChanged(nameof(ActiveArriveStationCharacterSpacing));
            OnPropertyChanged(nameof(PreviewArriveStationText));
        }
    }

    /// <summary>出发站「站」字相对站名主体的 Margin（水平间距 + 上偏移）。</summary>
    public Thickness PreviewDepartStationZhanMargin =>
        new(ActiveLayout.DepartStationZhanGapLeft, ActiveLayout.DepartStationZhanOffsetTop, 0, 0);

    /// <summary>到达站「站」字相对站名主体的 Margin。</summary>
    public Thickness PreviewArriveStationZhanMargin =>
        new(ActiveLayout.ArriveStationZhanGapLeft, ActiveLayout.ArriveStationZhanOffsetTop, 0, 0);

    public bool IsLayoutWorkbench => SessionMode == TicketPreviewSessionMode.LayoutWorkbench;

    /// <summary>主界面预览模式下，左侧行程/身份/Hint 只读，避免误改列表数据（不写库）。</summary>
    public bool IsTripFieldsReadOnly => SessionMode == TicketPreviewSessionMode.UserTripPreview;

    public string WindowTitle =>
        SessionMode == TicketPreviewSessionMode.LayoutWorkbench ? "票面参数调整" : "车票预览";

    public string IdentitySectionTitle =>
        IsTripFieldsReadOnly ? "身份信息（预览只读）" : "身份信息（可编辑）";

    partial void OnSessionModeChanged(TicketPreviewSessionMode value)
    {
        OnPropertyChanged(nameof(IsLayoutWorkbench));
        OnPropertyChanged(nameof(IsTripFieldsReadOnly));
        OnPropertyChanged(nameof(WindowTitle));
        OnPropertyChanged(nameof(IdentitySectionTitle));
        if (value == TicketPreviewSessionMode.UserTripPreview)
            IsVisualLayoutEdit = false;
    }

    [ObservableProperty] private string _currentZoom = "FitWindow";

    [ObservableProperty] private int _brightness = 100;

    [ObservableProperty] private double _scaleX = 1.0;

    [ObservableProperty] private double _scaleY = 1.0;

    [ObservableProperty] private string _ticketInfo = "未选择行程";

    public string ZoomPercentText => $"{Math.Round(ScaleX * 100)}%";

    public double BrightnessFactor => Brightness / 100.0;

    public Brush BrightnessOverlayBrush
    {
        get
        {
            if (Brightness > 100)
            {
                var opacity = (Brightness - 100) / 100.0 * 0.3;
                return new SolidColorBrush(Color.FromArgb((byte)(opacity * 255), 255, 255, 255));
            }

            if (Brightness < 100)
            {
                var opacity = (100 - Brightness) / 100.0 * 0.5;
                return new SolidColorBrush(Color.FromArgb((byte)(opacity * 255), 0, 0, 0));
            }

            return Brushes.Transparent;
        }
    }

    public HorizontalAlignment ImageHorizontalAlignment =>
        _uiSettingsService?.Config?.TicketCentered ?? true ? HorizontalAlignment.Center : HorizontalAlignment.Left;

    public VerticalAlignment ImageVerticalAlignment =>
        _uiSettingsService?.Config?.TicketCentered ?? true ? VerticalAlignment.Center : VerticalAlignment.Top;

    public bool AllowMouseWheelZoom =>
        _uiSettingsService?.Config?.AllowMouseWheelZoom ?? true;

    partial void OnSelectedDraftChanged(TicketPreviewDraft? value)
    {
        if (_draftPropertySubscription != null) _draftPropertySubscription.PropertyChanged -= _draftHandler;

        _draftPropertySubscription = value;
        if (value != null) value.PropertyChanged += _draftHandler;

        SubscribeTripSource(value?.Source);

        OnPropertyChanged(nameof(HasSelectedDraft));
        if (value == null)
        {
            TicketInfo = "未选择行程";
            RefreshStationNameFaceDisplay();
            PullPaymentChannelUiFromTrip(null);
            PullTicketTypeUiFromTrip(null);
            return;
        }

        TicketInfo =
            $"{value.Source.TrainNo} {value.Source.DepartStation} → {value.Source.ArriveStation} | {value.Source.DepartDate}";
        EncodingText = value.Source.TicketNumber ?? string.Empty;
        RebuildBadgeLetters();
        OnPropertyChanged(nameof(ActiveLayout));
        OnPropertyChanged(nameof(ArrowCanvasLeft));
        RefreshTripFieldDropdownOptions(value?.Source);
        RefreshStationNameFaceDisplay();
    }

    private void RefreshStationNameFaceDisplay()
    {
        OnPropertyChanged(nameof(CurrentDepartStationHanCount));
        OnPropertyChanged(nameof(CurrentArriveStationHanCount));
        OnPropertyChanged(nameof(WorkbenchDepartStationLayoutCaption));
        OnPropertyChanged(nameof(WorkbenchArriveStationLayoutCaption));
        OnPropertyChanged(nameof(DepartStationCharacterSpacingAdjustable));
        OnPropertyChanged(nameof(ArriveStationCharacterSpacingAdjustable));
        OnPropertyChanged(nameof(ActiveDepartStationCharacterSpacing));
        OnPropertyChanged(nameof(ActiveArriveStationCharacterSpacing));
        OnPropertyChanged(nameof(PreviewDepartStationText));
        OnPropertyChanged(nameof(PreviewArriveStationText));
        OnPropertyChanged(nameof(PreviewDepartStationCanvasLeft));
        OnPropertyChanged(nameof(PreviewArriveStationCanvasLeft));
        if (IsLayoutWorkbench && SelectedLayoutElementItem?.Kind is TicketFaceLayoutElementKind.DepartStation
            or TicketFaceLayoutElementKind.ArriveStation)
            PullEditorFromLayout();
        OnPropertyChanged(nameof(EditorAnchorXLabel));
    }

    private void SubscribeTripSource(TripRecord? trip)
    {
        if (_tripSourceSubscribed != null) _tripSourceSubscribed.PropertyChanged -= OnTripSourcePropertyChanged;
        _tripSourceSubscribed = trip;
        if (trip != null) trip.PropertyChanged += OnTripSourcePropertyChanged;
    }

    private void OnTripSourcePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (SelectedDraft?.Source != sender) return;
        RebuildBadgeLetters();
        TicketInfo =
            $"{SelectedDraft.Source.TrainNo} {SelectedDraft.Source.DepartStation} → {SelectedDraft.Source.ArriveStation} | {SelectedDraft.Source.DepartDate}";
        OnPropertyChanged(nameof(ArrowCanvasLeft));
        if (string.IsNullOrEmpty(e.PropertyName) ||
            e.PropertyName is nameof(TripRecord.DepartStation) or nameof(TripRecord.ArriveStation))
            RefreshStationNameFaceDisplay();

        if (e.PropertyName == nameof(TripRecord.TicketNumber) || string.IsNullOrEmpty(e.PropertyName))
            EncodingText = SelectedDraft.Source.TicketNumber ?? string.Empty;

        if (string.IsNullOrEmpty(e.PropertyName) ||
            e.PropertyName is nameof(TripRecord.AdditionalInfo) or nameof(TripRecord.TicketPurpose)
                or nameof(TripRecord.SeatType) or nameof(TripRecord.TicketModificationType)
                or nameof(TripRecord.TicketType) or nameof(TripRecord.PaymentChannel))
            RefreshTripFieldDropdownOptions(SelectedDraft.Source);
    }

    /// <summary>
    ///     保证当前行程字段值出现在下拉项中，并按表单规则联动附加信息/车票用途选项。
    /// </summary>
    private void RefreshTripFieldDropdownOptions(TripRecord? source)
    {
        if (source == null) return;

        ApplyTicketAndPaymentMutualExclusion(source);

        EnsureStringInObservable(_previewTripFieldOptions.SeatTypeOptions, source.SeatType);
        _previewTripFieldOptions.UpdateTicketPurposeOptions(
            source.AdditionalInfo ?? string.Empty,
            _previewTripFieldOptions.TicketPurposeOptions,
            source.TicketPurpose ?? string.Empty);
        _previewTripFieldOptions.UpdateAdditionalInfoOptions(
            source.TicketPurpose ?? string.Empty,
            _previewTripFieldOptions.AdditionalInfoOptions,
            source.AdditionalInfo ?? string.Empty);
        EnsureStringInObservable(_previewTripFieldOptions.TicketModificationTypeOptions, source.TicketModificationType);
        PullTicketTypeUiFromTrip(source);
        PullPaymentChannelUiFromTrip(source);
    }

    private static void EnsureStringInObservable(ObservableCollection<string> options, string? value)
    {
        if (string.IsNullOrEmpty(value)) return;
        if (!options.Contains(value)) options.Add(value);
    }

    private void PullPaymentChannelUiFromTrip(TripRecord? source)
    {
        _paymentChannelUiSync = true;
        try
        {
            if (source == null)
            {
                PreviewPaymentOnlineSelection = string.Empty;
                PreviewPaymentBankSelection = string.Empty;
                return;
            }

            ParsePaymentChannelDisplayToUi(source.PaymentChannel, out var online, out var bank);
            PreviewPaymentOnlineSelection = online;
            PreviewPaymentBankSelection = bank;
        }
        finally
        {
            _paymentChannelUiSync = false;
        }
    }

    private void PushPaymentChannelFromUiIfNeeded()
    {
        if (_paymentChannelUiSync || SelectedDraft?.Source == null) return;
        var built = BuildPaymentChannelFromUiParts(PreviewPaymentOnlineSelection, PreviewPaymentBankSelection);
        var normalized = NormalizePaymentChannelDisplayString(built);
        var cur = SelectedDraft.Source.PaymentChannel?.Trim() ?? string.Empty;
        if (!string.Equals(cur, normalized, StringComparison.Ordinal))
            SelectedDraft.Source.PaymentChannel = normalized;
    }

    private static void ParsePaymentChannelDisplayToUi(string? payment, out string online, out string bank)
    {
        online = string.Empty;
        bank = string.Empty;
        var parts = SplitCommaDisplayParts(payment ?? string.Empty);
        var hasAli = parts.Any(p => p.Contains("支付宝", StringComparison.Ordinal));
        var hasWx = parts.Any(p => p.Contains("微信", StringComparison.Ordinal));
        if (hasAli) online = "支付宝";
        else if (hasWx) online = "微信";

        foreach (var b in PaymentBankTokens)
        {
            foreach (var p in parts)
            {
                if (p.Contains(b, StringComparison.Ordinal))
                {
                    bank = b;
                    return;
                }
            }
        }
    }

    private static string BuildPaymentChannelFromUiParts(string? online, string? bank)
    {
        var o = (online ?? string.Empty).Trim();
        var bk = (bank ?? string.Empty).Trim();
        var list = new List<string>();
        if (o.Length > 0) list.Add(o);
        if (bk.Length > 0) list.Add(bk);
        return JoinCommaDisplayParts(list);
    }

    partial void OnPreviewPaymentOnlineSelectionChanged(string value) => PushPaymentChannelFromUiIfNeeded();

    partial void OnPreviewPaymentBankSelectionChanged(string value) => PushPaymentChannelFromUiIfNeeded();

    private void PullTicketTypeUiFromTrip(TripRecord? source)
    {
        _ticketTypeUiSync = true;
        try
        {
            if (source == null)
            {
                PreviewTicketFlagStudent = false;
                PreviewTicketFlagChild = false;
                PreviewTicketFlagDiscount = false;
                PreviewTicketFlagOnline = false;
                return;
            }

            ParseTicketTypeToFlags(source.TicketType, out var st, out var dis, out var onl, out var ch);
            PreviewTicketFlagStudent = st;
            PreviewTicketFlagDiscount = dis;
            PreviewTicketFlagOnline = onl;
            PreviewTicketFlagChild = ch;
        }
        finally
        {
            _ticketTypeUiSync = false;
        }
    }

    private void PushTicketTypeFromUiIfNeeded()
    {
        if (_ticketTypeUiSync || SelectedDraft?.Source == null) return;
        var built = BuildTicketTypeFromFlags(PreviewTicketFlagStudent, PreviewTicketFlagDiscount, PreviewTicketFlagOnline,
            PreviewTicketFlagChild);
        var normalized = NormalizeTicketTypeDisplayString(built);
        var cur = SelectedDraft.Source.TicketType?.Trim() ?? string.Empty;
        if (!string.Equals(cur, normalized, StringComparison.Ordinal))
            SelectedDraft.Source.TicketType = normalized;
    }

    private static void ParseTicketTypeToFlags(string? ticketType, out bool student, out bool discount, out bool online,
        out bool child)
    {
        var t = ticketType ?? string.Empty;
        student = t.Contains("学生", StringComparison.Ordinal);
        discount = t.Contains("优惠", StringComparison.Ordinal);
        online = t.Contains("网络", StringComparison.Ordinal);
        child = t.Contains("儿童", StringComparison.Ordinal);
    }

    /// <summary>与列表 flags 转文字顺序一致：<see cref="TripListViewModel" /> 中学、优、网、儿。</summary>
    private static string BuildTicketTypeFromFlags(bool student, bool discount, bool online, bool child)
    {
        var list = new List<string>();
        if (student) list.Add("学生票");
        if (discount) list.Add("优惠票");
        if (online) list.Add("网络售票");
        if (child) list.Add("儿童票");
        return JoinCommaDisplayParts(list);
    }

    partial void OnPreviewTicketFlagStudentChanged(bool value)
    {
        if (_ticketTypeUiSync) return;
        if (value && PreviewTicketFlagChild)
        {
            _ticketTypeUiSync = true;
            try
            {
                PreviewTicketFlagChild = false;
            }
            finally
            {
                _ticketTypeUiSync = false;
            }
        }

        PushTicketTypeFromUiIfNeeded();
    }

    partial void OnPreviewTicketFlagChildChanged(bool value)
    {
        if (_ticketTypeUiSync) return;
        if (value && PreviewTicketFlagStudent)
        {
            _ticketTypeUiSync = true;
            try
            {
                PreviewTicketFlagStudent = false;
            }
            finally
            {
                _ticketTypeUiSync = false;
            }
        }

        PushTicketTypeFromUiIfNeeded();
    }

    partial void OnPreviewTicketFlagDiscountChanged(bool value)
    {
        if (_ticketTypeUiSync) return;
        PushTicketTypeFromUiIfNeeded();
    }

    partial void OnPreviewTicketFlagOnlineChanged(bool value)
    {
        if (_ticketTypeUiSync) return;
        PushTicketTypeFromUiIfNeeded();
    }

    /// <summary>
    /// 与 <see cref="BusinessRuleEngine.HandleTicketTypeMutex"/> / <see cref="BusinessRuleEngine.HandlePaymentChannelMutex"/> 一致：
    /// 学生票与儿童票不得并存；支付宝与微信不得并存；多个银行简称只保留第一个。
    /// </summary>
    private static void ApplyTicketAndPaymentMutualExclusion(TripRecord source)
    {
        var tt0 = source.TicketType?.Trim() ?? string.Empty;
        var pc0 = source.PaymentChannel?.Trim() ?? string.Empty;
        var tt = NormalizeTicketTypeDisplayString(tt0);
        var pc = NormalizePaymentChannelDisplayString(pc0);
        if (!string.Equals(tt0, tt, StringComparison.Ordinal))
            source.TicketType = tt;
        if (!string.Equals(pc0, pc, StringComparison.Ordinal))
            source.PaymentChannel = pc;
    }

    private static List<string> SplitCommaDisplayParts(string s) =>
        string.IsNullOrWhiteSpace(s)
            ? new List<string>()
            : s.Split(new[] { ',', '，' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => x.Length > 0)
                .ToList();

    private static string JoinCommaDisplayParts(IReadOnlyList<string> parts) =>
        parts.Count == 0 ? string.Empty : string.Join(", ", parts);

    private static string NormalizeTicketTypeDisplayString(string ticketType)
    {
        var parts = SplitCommaDisplayParts(ticketType);
        if (parts.Count == 0) return string.Empty;
        var hasStudent = parts.Any(p => p.Contains("学生", StringComparison.Ordinal));
        var hasChild = parts.Any(p => p.Contains("儿童", StringComparison.Ordinal));
        if (hasStudent && hasChild)
            parts = parts.Where(p => !p.Contains("儿童", StringComparison.Ordinal)).ToList();
        return JoinCommaDisplayParts(parts);
    }

    private static bool IsPaymentBankToken(string p)
    {
        foreach (var b in PaymentBankTokens)
        {
            if (p.Contains(b, StringComparison.Ordinal)) return true;
        }

        return false;
    }

    private static string NormalizePaymentChannelDisplayString(string payment)
    {
        var parts = SplitCommaDisplayParts(payment);
        if (parts.Count == 0) return string.Empty;
        var hasAli = parts.Any(p => p.Contains("支付宝", StringComparison.Ordinal));
        var hasWx = parts.Any(p => p.Contains("微信", StringComparison.Ordinal));
        if (hasAli && hasWx)
            parts = parts.Where(p => !p.Contains("微信", StringComparison.Ordinal)).ToList();

        var bankKept = false;
        var result = new List<string>();
        foreach (var p in parts)
        {
            if (IsPaymentBankToken(p))
            {
                if (bankKept) continue;
                bankKept = true;
                result.Add(p);
                continue;
            }

            result.Add(p);
        }

        return JoinCommaDisplayParts(result);
    }

    partial void OnIsRedTicketChanged(bool value)
    {
        OnPropertyChanged(nameof(ActiveLayout));
        OnPropertyChanged(nameof(ArrowCanvasLeft));
        OnPropertyChanged(nameof(PreviewDepartStationText));
        OnPropertyChanged(nameof(PreviewArriveStationText));
        OnPropertyChanged(nameof(BadgeCornerRadius));
        RefreshTicketBackgroundImage();
        PullEditorFromLayout();
    }

    partial void OnEncodingTextChanged(string value) => RegenerateQr();

    partial void OnShowFramedTicketBadgesChanged(bool value)
    {
        OnPropertyChanged(nameof(ActiveLayout));
        OnPropertyChanged(nameof(BadgeCornerRadius));
    }

    public CornerRadius BadgeCornerRadius =>
        ShowFramedTicketBadges
            ? (IsRedTicket ? new CornerRadius(2) : new CornerRadius(11))
            : new CornerRadius(0);

    public void SetTripItem(TripRecord? tripItem)
    {
        if (tripItem == null)
        {
            SetSourceTrips(Array.Empty<TripRecord>());
            return;
        }

        SetSourceTrips(new[] { tripItem });
    }

    public void SetSourceTrips(IEnumerable<TripRecord> trips)
    {
        SelectedDraft = null;
        foreach (var d in PreviewDrafts.ToList())
            d.Dispose();
        PreviewDrafts.Clear();
        foreach (var t in trips ?? Enumerable.Empty<TripRecord>())
        {
            ApplyTicketAndPaymentMutualExclusion(t);
            var d = new TicketPreviewDraft(t);
            if (string.IsNullOrWhiteSpace(d.IdMask) && !string.IsNullOrWhiteSpace(d.IdNumber))
                d.IdMask = TicketPreviewDraft.ComputeDefaultIdMask(d.IdNumber);
            PreviewDrafts.Add(d);
        }

        SelectedDraft = PreviewDrafts.FirstOrDefault();
        OnPropertyChanged(nameof(HasMultipleDrafts));
        EncodingText = SelectedDraft?.Source.TicketNumber ?? string.Empty;
        RebuildBadgeLetters();
        RefreshTripFieldDropdownOptions(SelectedDraft?.Source);
    }

    private void RebuildBadgeLetters()
    {
        PaymentBadgeLetters.Clear();
        if (SelectedDraft == null)
        {
            ShowTicketBadgeXue = false;
            ShowTicketBadgeHai = false;
            ShowTicketBadgeWang = false;
            ShowTicketBadgeDiscount = false;
            TicketDiscountBadgeChar = "折";
            HasPaymentBadgeLetters = false;
            return;
        }

        var tt = SelectedDraft.Source.TicketType ?? string.Empty;
        ShowTicketBadgeXue = tt.Contains("学生", StringComparison.Ordinal);
        ShowTicketBadgeHai = tt.Contains("儿童", StringComparison.Ordinal);
        ShowTicketBadgeWang = !SelectedDraft.SuppressNetworkTicketBadge &&
                              tt.Contains("网络", StringComparison.Ordinal);
        ShowTicketBadgeDiscount = tt.Contains("优惠", StringComparison.Ordinal);
        TicketDiscountBadgeChar = SelectedDraft.PreferDiscountZhe ? "折" : "惠";
        foreach (var ch in SelectedDraft.PaymentBadgeLetters())
            if (!string.IsNullOrEmpty(ch))
                PaymentBadgeLetters.Add(ch);
        HasPaymentBadgeLetters = PaymentBadgeLetters.Count > 0;
    }

    private void LoadSettings()
    {
        var config = _uiSettingsService.Config;
        CurrentZoom = config.DefaultZoom;
        Brightness = config.DisplayBrightness;
        ApplyZoom(CurrentZoom);
    }

    /// <summary>
    /// 由预览区 <see cref="System.Windows.Controls.ScrollViewer"/> 在 Loaded/SizeChanged 时上报可用宽高，
    /// 用于「适应窗口」按 811×509 计算缩放（不再使用外层 Viewbox 以免抵消 LayoutTransform）。
    /// </summary>
    public void NotifyTicketPreviewHostSize(double width, double height)
    {
        if (width < 8 || height < 8) return;
        width = Math.Max(1.0, width);
        height = Math.Max(1.0, height);
        if (Math.Abs(_ticketPreviewHostWidth - width) < 0.5 && Math.Abs(_ticketPreviewHostHeight - height) < 0.5) return;
        _ticketPreviewHostWidth = width;
        _ticketPreviewHostHeight = height;
        ApplyZoom(CurrentZoom);
    }

    private void ApplyZoom(string zoom)
    {
        const double tw = 811.0;
        const double th = 509.0;
        if (string.Equals(zoom, "FitWindow", StringComparison.OrdinalIgnoreCase))
        {
            var fit = Math.Min(_ticketPreviewHostWidth / tw, _ticketPreviewHostHeight / th);
            if (fit <= 0 || double.IsNaN(fit) || double.IsInfinity(fit)) fit = 1.0;
            ScaleX = fit;
            ScaleY = fit;
        }
        else if (double.TryParse(zoom, System.Globalization.NumberStyles.Integer,
                     System.Globalization.CultureInfo.InvariantCulture, out var percent))
        {
            var scale = percent / 100.0;
            ScaleX = scale;
            ScaleY = scale;
        }

        OnPropertyChanged(nameof(ZoomPercentText));
    }

    partial void OnCurrentZoomChanged(string value) => ApplyZoom(value);

    partial void OnBrightnessChanged(int value)
    {
        OnPropertyChanged(nameof(BrightnessFactor));
        OnPropertyChanged(nameof(BrightnessOverlayBrush));
    }

    private void RefreshTicketBackgroundOpacity()
    {
        TicketBackgroundOpacity = ThemeManager.IsDarkTheme ? 0.88 : 1.0;
    }

    private void RefreshTicketBackgroundImage()
    {
        var file = IsRedTicket ? "redTicket.png" : "blueTicket.png";
        var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Images", file);
        if (!File.Exists(path))
            path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Resources", "Images", file);

        if (!File.Exists(path))
        {
            TicketBackgroundImage = null;
            return;
        }

        try
        {
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.StreamSource = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.EndInit();
            if (bmp.CanFreeze)
                bmp.Freeze();
            TicketBackgroundImage = bmp;
        }
        catch
        {
            TicketBackgroundImage = null;
        }
    }

    private void RegenerateQr()
    {
        QrCodeSource = TicketPreviewQrService.CreateQrBitmap(EncodingText, 5);
    }

    private void TryValidateIdentityDigits()
    {
        if (SelectedDraft == null) return;
        var id = SelectedDraft.IdNumber?.Trim() ?? string.Empty;
        if (id.Length != 18) return;
        if (CommonUtils.ValidateIdCard(id)) return;

        GuiPiao.View.MessageBoxWindow.Show(Application.Current.MainWindow, "身份证号格式或校验位不正确，请重新输入。", "身份信息",
            MessageBoxButton.OK, MessageBoxImage.Warning);
        SelectedDraft.IdNumber = string.Empty;
        SelectedDraft.IdentityInputError = true;
        Application.Current.Dispatcher.BeginInvoke(new Action(() =>
        {
            if (SelectedDraft != null) SelectedDraft.IdentityInputError = false;
        }), System.Windows.Threading.DispatcherPriority.ApplicationIdle, TimeSpan.FromSeconds(2.5));
    }

    /// <summary>文档：按字典更新红/蓝布局参数。</summary>
    public void UpdateLayout(IReadOnlyDictionary<string, object> layoutValues, bool isRedTicket)
    {
        if (layoutValues == null || layoutValues.Count == 0) return;
        var target = isRedTicket ? _layoutRed : _layoutBlue;
        foreach (var kv in layoutValues)
            TicketFaceLayoutPatch.TryApplyKey(target, kv.Key, kv.Value);
        if (isRedTicket != IsRedTicket) return;
        OnPropertyChanged(nameof(ActiveLayout));
        OnPropertyChanged(nameof(ArrowCanvasLeft));
        OnPropertyChanged(nameof(PreviewDepartStationText));
        OnPropertyChanged(nameof(PreviewArriveStationText));
        PullEditorFromLayout();
    }

    [RelayCommand]
    private void SwitchRedBlue(string? mode)
    {
        IsRedTicket = string.Equals(mode, "red", StringComparison.OrdinalIgnoreCase);
    }

    [RelayCommand]
    private void ZoomIn()
    {
        var currentPercent = ScaleX * 100;
        double newPercent = currentPercent switch
        {
            < 50 => 50,
            < 75 => 75,
            < 100 => 100,
            < 125 => 125,
            < 150 => 150,
            < 200 => 200,
            < 300 => 300,
            < 400 => 400,
            _ => 400
        };

        if (newPercent > currentPercent) CurrentZoom = newPercent.ToString();
    }

    [RelayCommand]
    private void ZoomOut()
    {
        var currentPercent = ScaleX * 100;
        double newPercent = currentPercent switch
        {
            > 300 => 300,
            > 200 => 200,
            > 150 => 150,
            > 125 => 125,
            > 100 => 100,
            > 75 => 75,
            > 50 => 50,
            _ => 50
        };

        if (newPercent < currentPercent) CurrentZoom = newPercent.ToString();
    }

    public void HandleMouseWheel(double delta)
    {
        if (!AllowMouseWheelZoom) return;
        if (delta > 0) ZoomIn();
        else ZoomOut();
    }

    [RelayCommand]
    private void ExportPng(FrameworkElement? previewSurface)
    {
        if (previewSurface == null || SelectedDraft == null)
        {
            GuiPiao.View.MessageBoxWindow.Show(Application.Current.MainWindow, "无法导出：缺少票面画布。", "导出图片",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var d = SelectedDraft;
        var name =
            $"{TicketPreviewDraft.TrimTrailingStation(d.Source.DepartStation)}-{d.Source.TrainNo}-{TicketPreviewDraft.TrimTrailingStation(d.Source.ArriveStation)}.png";
        foreach (var c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');

        var dlg = new SaveFileDialog
        {
            Filter = "PNG 图片|*.png",
            FileName = name
        };

        if (dlg.ShowDialog() != true) return;
        if (string.IsNullOrWhiteSpace(dlg.FileName))
        {
            GuiPiao.View.MessageBoxWindow.Show(Application.Current.MainWindow, "未选择有效文件名。", "导出图片",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var sx = ScaleX;
        var sy = ScaleY;
        try
        {
            const int w = 811;
            const int h = 509;
            ScaleX = 1.0;
            ScaleY = 1.0;
            previewSurface.Measure(new Size(w, h));
            previewSurface.Arrange(new Rect(0, 0, w, h));
            previewSurface.UpdateLayout();

            var rt = new RenderTargetBitmap(w, h, 96, 96, PixelFormats.Pbgra32);
            var dv = new DrawingVisual();
            using (var dc = dv.RenderOpen())
            {
                dc.DrawRectangle(Brushes.White, null, new Rect(0, 0, w, h));
                var vb = new VisualBrush(previewSurface) { Stretch = Stretch.None, AlignmentX = AlignmentX.Left, AlignmentY = AlignmentY.Top };
                dc.DrawRectangle(vb, null, new Rect(0, 0, w, h));
            }

            rt.Render(dv);

            var enc = new PngBitmapEncoder();
            enc.Frames.Add(BitmapFrame.Create(rt));
            using var fs = File.Create(dlg.FileName);
            enc.Save(fs);

            GuiPiao.View.MessageBoxWindow.Show(Application.Current.MainWindow, $"已保存：{dlg.FileName}", "导出图片",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            GuiPiao.View.MessageBoxWindow.Show(Application.Current.MainWindow, $"导出失败：{ex.Message}", "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            ScaleX = sx;
            ScaleY = sy;
            OnPropertyChanged(nameof(ZoomPercentText));
        }
    }

    [RelayCommand]
    private void CloseWindow(Window? window)
    {
        window?.Close();
    }

    public string GetCurrentZoomSetting() => CurrentZoom;

    public int GetCurrentBrightnessSetting() => Brightness;

    /// <summary>窗口关闭时解除主题/草稿订阅，避免泄漏。</summary>
    public void DetachWindowListeners()
    {
        ThemeManager.ThemeChanged -= OnAppThemeChanged;
        UnwireLayoutObservers();
        if (_draftPropertySubscription != null) _draftPropertySubscription.PropertyChanged -= _draftHandler;
        _draftPropertySubscription = null;
        SubscribeTripSource(null);
    }
}
