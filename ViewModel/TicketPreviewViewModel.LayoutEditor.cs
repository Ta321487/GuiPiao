using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GuiPiao.Model;
using GuiPiao.Services;
using Microsoft.Win32;

namespace GuiPiao.ViewModel;

public sealed class TicketLayoutElementOption
{
    public TicketLayoutElementOption(TicketFaceLayoutElementKind kind, string display)
    {
        Kind = kind;
        Display = display;
    }

    public TicketFaceLayoutElementKind Kind { get; }
    public string Display { get; }
}

public partial class TicketPreviewViewModel
{
    private bool _editorSync;

    /// <summary>布局工作台：位置磁吸步长下拉选项。</summary>
    public double[] WorkbenchSnapStepChoices { get; } = { 1, 4, 8, 16 };

    public ObservableCollection<TicketLayoutElementOption> LayoutElementItems { get; } = new()
    {
        new(TicketFaceLayoutElementKind.TicketSerial, "车票号码"),
        new(TicketFaceLayoutElementKind.CheckInLabel, "检票口·标签"),
        new(TicketFaceLayoutElementKind.CheckInValue, "检票口·内容"),
        new(TicketFaceLayoutElementKind.DepartStation, "出发站"),
        new(TicketFaceLayoutElementKind.DepartStationZhan, "出发站·站字"),
        new(TicketFaceLayoutElementKind.DepartPinyin, "出发站拼音"),
        new(TicketFaceLayoutElementKind.TrainNo, "车次"),
        new(TicketFaceLayoutElementKind.Arrow, "箭头"),
        new(TicketFaceLayoutElementKind.ArriveStation, "到达站"),
        new(TicketFaceLayoutElementKind.ArriveStationZhan, "到达站·站字"),
        new(TicketFaceLayoutElementKind.ArrivePinyin, "到达站拼音"),
        new(TicketFaceLayoutElementKind.DateYearDigits, "日期·年份数字"),
        new(TicketFaceLayoutElementKind.DateNianChar, "日期·「年」字"),
        new(TicketFaceLayoutElementKind.DateMonthDigits, "日期·月份数字"),
        new(TicketFaceLayoutElementKind.DateYueChar, "日期·「月」字"),
        new(TicketFaceLayoutElementKind.DateDayDigits, "日期·日数字"),
        new(TicketFaceLayoutElementKind.DateRiChar, "日期·「日」字"),
        new(TicketFaceLayoutElementKind.DateTimeHm, "日期·发车时分"),
        new(TicketFaceLayoutElementKind.DateKaiChar, "日期·「开」字"),
        new(TicketFaceLayoutElementKind.MoneySymbol, "金额·￥"),
        new(TicketFaceLayoutElementKind.MoneyAmount, "金额·数字"),
        new(TicketFaceLayoutElementKind.MoneyUnit, "金额·元"),
        new(TicketFaceLayoutElementKind.CoachNumber, "车厢号"),
        new(TicketFaceLayoutElementKind.CoachChe, "车厢·车字"),
        new(TicketFaceLayoutElementKind.SeatNumber, "座位号"),
        new(TicketFaceLayoutElementKind.SeatHao, "座位·号字"),
        new(TicketFaceLayoutElementKind.SeatType, "席别"),
        new(TicketFaceLayoutElementKind.TicketModificationType, "改签类型"),
        new(TicketFaceLayoutElementKind.Purpose, "车票用途"),
        new(TicketFaceLayoutElementKind.AdditionalInfo, "附加信息"),
        new(TicketFaceLayoutElementKind.IdName, "证件/姓名"),
        new(TicketFaceLayoutElementKind.HintBox, "提示区"),
        new(TicketFaceLayoutElementKind.Footer, "底部报销说明"),
        new(TicketFaceLayoutElementKind.Qr, "二维码"),
        new(TicketFaceLayoutElementKind.BadgeLetterXue, "简字·学"),
        new(TicketFaceLayoutElementKind.BadgeLetterHai, "简字·孩"),
        new(TicketFaceLayoutElementKind.BadgeLetterWang, "简字·网"),
        new(TicketFaceLayoutElementKind.BadgeLetterDiscount, "简字·折/惠"),
        new(TicketFaceLayoutElementKind.BadgePaymentRow, "支付简字行")
    };

    [ObservableProperty] private string _layoutDefaultFontFamily = string.Empty;
    [ObservableProperty] private TicketLayoutElementOption? _selectedLayoutElementItem;

    /// <summary>
    ///     与「编辑元素」下拉同步的逻辑块类型，供票面 MultiBinding 做工作台隔离（避免仅绑定 SelectedItem 时在 Viewbox 等树下解析失败导致整块 Collapsed）。
    /// </summary>
    [ObservableProperty] private TicketFaceLayoutElementKind? _layoutIsolationTargetKind;

    [ObservableProperty] private bool _isVisualLayoutEdit;

    /// <summary>票面上拖拽微调与「对齐网格」使用的磁吸步长（px）。</summary>
    [ObservableProperty] private double _workbenchLayoutSnapStepPixels = 8;

    /// <summary>布局工作台「复制字号」缓存（箭头为线粗，其余为字号）。</summary>
    [ObservableProperty] private double? _copiedWorkbenchFontSize;

    [ObservableProperty] private double _editorAnchorX;
    [ObservableProperty] private double _editorAnchorY;
    [ObservableProperty] private double _editorFontSize = 12;
    [ObservableProperty] private double _editorExtra = 120;
    [ObservableProperty] private double _editorArrowHeadLength;
    [ObservableProperty] private string _editorFontFamily = string.Empty;

    public bool EditorShowsExtraDimension =>
        SelectedLayoutElementItem?.Kind is TicketFaceLayoutElementKind.HintBox or TicketFaceLayoutElementKind.Qr
            or TicketFaceLayoutElementKind.Arrow;

    public string EditorExtraLabel =>
        SelectedLayoutElementItem?.Kind switch
        {
            TicketFaceLayoutElementKind.Qr => "二维码边长",
            TicketFaceLayoutElementKind.Arrow => "箭头长度（px）",
            _ => "提示区宽度"
        };

    public double EditorExtraMinimum =>
        SelectedLayoutElementItem?.Kind switch
        {
            TicketFaceLayoutElementKind.Arrow => 20,
            TicketFaceLayoutElementKind.Qr => 40,
            _ => 40
        };

    public double EditorExtraMaximum =>
        SelectedLayoutElementItem?.Kind switch
        {
            TicketFaceLayoutElementKind.Arrow => 140,
            TicketFaceLayoutElementKind.Qr => 400,
            _ => 780
        };

    public double EditorFontSizeMinimum =>
        SelectedLayoutElementItem?.Kind == TicketFaceLayoutElementKind.Arrow ? 0.5 : 6;

    public double EditorFontSizeMaximum =>
        SelectedLayoutElementItem?.Kind == TicketFaceLayoutElementKind.Arrow ? 4 : 72;

    public bool EditorUsesStationZhanGap =>
        SelectedLayoutElementItem?.Kind is TicketFaceLayoutElementKind.DepartStationZhan
            or TicketFaceLayoutElementKind.ArriveStationZhan;

    public string EditorAnchorXLabel =>
        EditorUsesStationZhanGap ? "站字间距（px）"
        : SelectedLayoutElementItem?.Kind == TicketFaceLayoutElementKind.DepartStation && CurrentDepartStationHanCount > 0
            ? $"左边距（当前 {CurrentDepartStationHanCount} 字，{StationFaceHanCountLayout.ReferenceHanCount} 字为基准）"
        : SelectedLayoutElementItem?.Kind == TicketFaceLayoutElementKind.ArriveStation && CurrentArriveStationHanCount > 0
            ? $"左边距（当前 {CurrentArriveStationHanCount} 字，{StationFaceHanCountLayout.ReferenceHanCount} 字为基准）"
        : "X（Canvas.Left）";

    public string EditorAnchorYLabel =>
        EditorUsesStationZhanGap ? "站字上偏移（px）" : "Y（Canvas.Top）";

    public double EditorAnchorXMaximum => EditorUsesStationZhanGap ? 120 : WorkbenchFaceWidth;

    public double EditorAnchorYMaximum => EditorUsesStationZhanGap ? 40 : WorkbenchFaceHeight;

    public double EditorAnchorYMinimum => EditorUsesStationZhanGap ? -20 : 0;

    public string EditorFontSizeLabel =>
        SelectedLayoutElementItem?.Kind == TicketFaceLayoutElementKind.Arrow ? "线粗（px）" : "字号";

    public bool EditorShowsArrowHeadFineTune =>
        SelectedLayoutElementItem?.Kind == TicketFaceLayoutElementKind.Arrow;

    public bool EditorShowsFontSize => SelectedLayoutElementItem?.Kind != TicketFaceLayoutElementKind.Qr;

    /// <summary>有选中编辑元素即可复制当前块的主尺寸（字号、箭头线粗或二维码边长）。</summary>
    public bool CanCopyWorkbenchFontSize => SelectedLayoutElementItem != null;

    /// <summary>有缓存且已选中目标元素即可粘贴（目标为二维码时写入边长，箭头为线粗，其余为字号）。</summary>
    public bool CanPasteWorkbenchFontSize => CopiedWorkbenchFontSize is not null && SelectedLayoutElementItem != null;

    /// <summary>字号格式刷状态说明。</summary>
    public string WorkbenchCopiedFontSizeCaption =>
        CopiedWorkbenchFontSize is { } d
            ? $"已缓存：{d:0.##}（粘贴时：正文→字号，箭头→线粗，二维码→边长，均按目标允许范围夹紧）"
            : "未复制";

    public bool EditorShowsFontFamily =>
        SelectedLayoutElementItem?.Kind is not (TicketFaceLayoutElementKind.Qr or TicketFaceLayoutElementKind.Arrow);

    private IReadOnlyList<string>? _layoutDefaultFontPickerItemsCache;
    private IReadOnlyList<string>? _editorFontPickerItemsCache;

    /// <summary>全局后备字体下拉项：系统字体 + 当前值（若为浏览得到的非列表项则插在前列）。</summary>
    public IReadOnlyList<string> LayoutDefaultFontPickerItems =>
        _layoutDefaultFontPickerItemsCache ??= BuildLayoutDefaultFontPickerItems();

    /// <summary>本元素字体下拉项：首项为空表示不单独指定；其余为系统字体 + 当前非列表值。</summary>
    public IReadOnlyList<string> EditorFontPickerItems =>
        _editorFontPickerItemsCache ??= BuildEditorFontPickerItems();

    partial void OnLayoutDefaultFontFamilyChanged(string value)
    {
        InvalidateLayoutDefaultFontPickerItems();
        OnPropertyChanged(nameof(ActiveLayout));
    }

    partial void OnSelectedLayoutElementItemChanged(TicketLayoutElementOption? value)
    {
        LayoutIsolationTargetKind = value?.Kind;
        PullEditorFromLayout();
        InvalidateEditorFontPickerItems();
        OnPropertyChanged(nameof(EditorShowsExtraDimension));
        OnPropertyChanged(nameof(EditorExtraLabel));
        OnPropertyChanged(nameof(EditorExtraMinimum));
        OnPropertyChanged(nameof(EditorExtraMaximum));
        OnPropertyChanged(nameof(EditorFontSizeMinimum));
        OnPropertyChanged(nameof(EditorFontSizeMaximum));
        OnPropertyChanged(nameof(EditorFontSizeLabel));
        OnPropertyChanged(nameof(EditorShowsArrowHeadFineTune));
        OnPropertyChanged(nameof(EditorShowsFontFamily));
        OnPropertyChanged(nameof(EditorShowsFontSize));
        OnPropertyChanged(nameof(CanCopyWorkbenchFontSize));
        OnPropertyChanged(nameof(CanPasteWorkbenchFontSize));
        OnPropertyChanged(nameof(EditorUsesStationZhanGap));
        OnPropertyChanged(nameof(EditorAnchorXLabel));
        OnPropertyChanged(nameof(EditorAnchorYLabel));
        OnPropertyChanged(nameof(EditorAnchorXMaximum));
        OnPropertyChanged(nameof(EditorAnchorYMaximum));
        OnPropertyChanged(nameof(EditorAnchorYMinimum));
    }

    partial void OnCopiedWorkbenchFontSizeChanged(double? value)
    {
        OnPropertyChanged(nameof(CanPasteWorkbenchFontSize));
        OnPropertyChanged(nameof(WorkbenchCopiedFontSizeCaption));
    }

    partial void OnEditorArrowHeadLengthChanged(double value)
    {
        if (_editorSync) return;
        PushEditorToLayout();
    }

    private void InvalidateLayoutDefaultFontPickerItems()
    {
        _layoutDefaultFontPickerItemsCache = null;
        OnPropertyChanged(nameof(LayoutDefaultFontPickerItems));
    }

    private void InvalidateEditorFontPickerItems()
    {
        _editorFontPickerItemsCache = null;
        OnPropertyChanged(nameof(EditorFontPickerItems));
    }

    private IReadOnlyList<string> BuildLayoutDefaultFontPickerItems()
    {
        var sys = FontFamilyPickerSupport.SystemFontFamilySources;
        var cur = (LayoutDefaultFontFamily ?? "").Trim();
        if (string.IsNullOrEmpty(cur)) return sys;
        foreach (var s in sys)
            if (string.Equals(s, cur, StringComparison.OrdinalIgnoreCase))
                return sys;
        var merged = new List<string>(sys.Count + 1) { cur };
        foreach (var s in sys)
            if (!string.Equals(s, cur, StringComparison.OrdinalIgnoreCase))
                merged.Add(s);
        return merged;
    }

    private IReadOnlyList<string> BuildEditorFontPickerItems()
    {
        var sys = FontFamilyPickerSupport.SystemFontFamilySources;
        var cur = (EditorFontFamily ?? "").Trim();
        var list = new List<string> { string.Empty };
        if (!string.IsNullOrEmpty(cur) &&
            !sys.Any(s => string.Equals(s, cur, StringComparison.OrdinalIgnoreCase)))
            list.Add(cur);
        foreach (var s in sys)
        {
            if (string.IsNullOrEmpty(s)) continue;
            if (list.Any(x => string.Equals(x, s, StringComparison.OrdinalIgnoreCase))) continue;
            list.Add(s);
        }

        return list;
    }

    partial void OnEditorAnchorXChanged(double value)
    {
        if (_editorSync) return;
        PushEditorToLayout();
    }

    partial void OnEditorAnchorYChanged(double value)
    {
        if (_editorSync) return;
        PushEditorToLayout();
    }

    partial void OnEditorFontSizeChanged(double value)
    {
        if (_editorSync) return;
        PushEditorToLayout();
    }

    partial void OnEditorExtraChanged(double value)
    {
        if (_editorSync) return;
        PushEditorToLayout();
    }

    partial void OnEditorFontFamilyChanged(string value)
    {
        if (_editorSync) return;
        PushEditorToLayout();
    }

    public void ApplyLayoutDrag(double dx, double dy)
    {
        if (SelectedLayoutElementItem == null) return;
        var L = ActiveLayout;
        switch (SelectedLayoutElementItem.Kind)
        {
            case TicketFaceLayoutElementKind.TicketSerial: L.TicketSerialLeft += dx; L.TicketSerialTop += dy; break;
            case TicketFaceLayoutElementKind.CheckInLabel: L.CheckInLeft += dx; L.CheckInTop += dy; break;
            case TicketFaceLayoutElementKind.CheckInValue: L.CheckInValueLeft += dx; L.CheckInValueTop += dy; break;
            case TicketFaceLayoutElementKind.DepartStation:
                ApplyDepartStationEffectiveLeft(L, StationFaceHanCountLayout.GetDepartCanvasLeft(L, CurrentDepartStationHanCount) + dx);
                L.DepartStationTop += dy;
                break;
            case TicketFaceLayoutElementKind.DepartStationZhan: L.DepartStationZhanGapLeft += dx; L.DepartStationZhanOffsetTop += dy; break;
            case TicketFaceLayoutElementKind.DepartPinyin: L.DepartPinyinLeft += dx; L.DepartPinyinTop += dy; break;
            case TicketFaceLayoutElementKind.TrainNo: L.TrainNoLeft += dx; L.TrainNoTop += dy; break;
            case TicketFaceLayoutElementKind.Arrow: L.ArrowLeft += dx; L.ArrowTop += dy; break;
            case TicketFaceLayoutElementKind.ArriveStation:
                ApplyArriveStationEffectiveLeft(L, StationFaceHanCountLayout.GetArriveCanvasLeft(L, CurrentArriveStationHanCount) + dx);
                L.ArriveStationTop += dy;
                break;
            case TicketFaceLayoutElementKind.ArriveStationZhan: L.ArriveStationZhanGapLeft += dx; L.ArriveStationZhanOffsetTop += dy; break;
            case TicketFaceLayoutElementKind.ArrivePinyin: L.ArrivePinyinLeft += dx; L.ArrivePinyinTop += dy; break;
            case TicketFaceLayoutElementKind.DateYearDigits: L.DateYearDigitsLeft += dx; L.DateYearDigitsTop += dy; break;
            case TicketFaceLayoutElementKind.DateNianChar: L.DateNianCharLeft += dx; L.DateNianCharTop += dy; break;
            case TicketFaceLayoutElementKind.DateMonthDigits: L.DateMonthDigitsLeft += dx; L.DateMonthDigitsTop += dy; break;
            case TicketFaceLayoutElementKind.DateYueChar: L.DateYueCharLeft += dx; L.DateYueCharTop += dy; break;
            case TicketFaceLayoutElementKind.DateDayDigits: L.DateDayDigitsLeft += dx; L.DateDayDigitsTop += dy; break;
            case TicketFaceLayoutElementKind.DateRiChar: L.DateRiCharLeft += dx; L.DateRiCharTop += dy; break;
            case TicketFaceLayoutElementKind.DateTimeHm: L.DateTimeHmLeft += dx; L.DateTimeHmTop += dy; break;
            case TicketFaceLayoutElementKind.DateKaiChar: L.DateKaiCharLeft += dx; L.DateKaiCharTop += dy; break;
            case TicketFaceLayoutElementKind.MoneyRow:
            case TicketFaceLayoutElementKind.MoneySymbol:
                L.MoneySymbolLeft += dx; L.MoneySymbolTop += dy; break;
            case TicketFaceLayoutElementKind.MoneyAmount:
                L.MoneyAmountLeft += dx; L.MoneyAmountTop += dy; break;
            case TicketFaceLayoutElementKind.MoneyUnit:
                L.MoneyUnitLeft += dx; L.MoneyUnitTop += dy; break;
            case TicketFaceLayoutElementKind.CoachSeat:
            case TicketFaceLayoutElementKind.CoachNumber:
                L.CoachNumberLeft += dx; L.CoachNumberTop += dy; break;
            case TicketFaceLayoutElementKind.CoachChe:
                L.CoachCheLeft += dx; L.CoachCheTop += dy; break;
            case TicketFaceLayoutElementKind.SeatNumber:
                L.SeatNumberLeft += dx; L.SeatNumberTop += dy; break;
            case TicketFaceLayoutElementKind.SeatHao:
                L.SeatHaoLeft += dx; L.SeatHaoTop += dy; break;
            case TicketFaceLayoutElementKind.SeatType: L.SeatTypeRight += dx; L.SeatTypeTop += dy; break;
            case TicketFaceLayoutElementKind.TicketModificationType:
                L.TicketModificationTypeLeft += dx;
                L.TicketModificationTypeTop += dy;
                break;
            case TicketFaceLayoutElementKind.Purpose: L.PurposeLeft += dx; L.PurposeTop += dy; break;
            case TicketFaceLayoutElementKind.AdditionalInfo: L.AdditionalInfoLeft += dx; L.AdditionalInfoTop += dy; break;
            case TicketFaceLayoutElementKind.IdName: L.IdNameLeft += dx; L.IdNameTop += dy; break;
            case TicketFaceLayoutElementKind.HintBox: L.HintBoxLeft += dx; L.HintBoxTop += dy; break;
            case TicketFaceLayoutElementKind.Footer: L.FooterLeft += dx; L.FooterTop += dy; break;
            case TicketFaceLayoutElementKind.Qr: L.QrLeft += dx; L.QrTop += dy; break;
            case TicketFaceLayoutElementKind.BadgeLetterXue: L.BadgeLetterXueLeft += dx; L.BadgeLetterXueTop += dy; break;
            case TicketFaceLayoutElementKind.BadgeLetterHai: L.BadgeLetterHaiLeft += dx; L.BadgeLetterHaiTop += dy; break;
            case TicketFaceLayoutElementKind.BadgeLetterWang: L.BadgeLetterWangLeft += dx; L.BadgeLetterWangTop += dy; break;
            case TicketFaceLayoutElementKind.BadgeLetterDiscount:
                L.BadgeLetterDiscountLeft += dx;
                L.BadgeLetterDiscountTop += dy;
                break;
            case TicketFaceLayoutElementKind.BadgePaymentRow: L.BadgePaymentRowLeft += dx; L.BadgePaymentRowTop += dy; break;
        }

        OnPropertyChanged(nameof(ArrowCanvasLeft));
        PullEditorFromLayout();
    }

    private const double WorkbenchFaceWidth = 811;
    private const double WorkbenchFaceHeight = 509;

    private Point _workbenchSurfaceDragMouse0;
    private double _workbenchSurfaceDragAnchor0X;
    private double _workbenchSurfaceDragAnchor0Y;

    /// <summary>
    /// 在票面上拖拽微调开始时调用：记录鼠标与当前锚点，后续用 <see cref="ApplyWorkbenchSurfaceDrag"/> 按绝对位移 + 网格磁吸更新。
    /// </summary>
    public void BeginWorkbenchSurfaceDrag(double surfaceMouseX, double surfaceMouseY)
    {
        if (SelectedLayoutElementItem == null || !IsVisualLayoutEdit) return;
        PullEditorFromLayout();
        _workbenchSurfaceDragMouse0 = new Point(surfaceMouseX, surfaceMouseY);
        _workbenchSurfaceDragAnchor0X = EditorAnchorX;
        _workbenchSurfaceDragAnchor0Y = EditorAnchorY;
    }

    /// <summary>
    /// 按票面坐标更新选中块位置（相对按下点 + 所选步长磁吸），避免增量吸附导致光标与块错位。
    /// </summary>
    public void ApplyWorkbenchSurfaceDrag(double surfaceMouseX, double surfaceMouseY)
    {
        if (SelectedLayoutElementItem == null || !IsVisualLayoutEdit) return;
        var tx = _workbenchSurfaceDragAnchor0X + surfaceMouseX - _workbenchSurfaceDragMouse0.X;
        var ty = _workbenchSurfaceDragAnchor0Y + surfaceMouseY - _workbenchSurfaceDragMouse0.Y;
        tx = ClampWorkbench(tx, 0, WorkbenchFaceWidth);
        ty = ClampWorkbench(ty, 0, WorkbenchFaceHeight);
        var step = NormalizeWorkbenchSnapStep(WorkbenchLayoutSnapStepPixels);
        tx = SnapWorkbenchCoord(tx, step);
        ty = SnapWorkbenchCoord(ty, step);
        _editorSync = true;
        try
        {
            EditorAnchorX = tx;
            EditorAnchorY = ty;
        }
        finally
        {
            _editorSync = false;
        }

        PushEditorToLayout();
        OnPropertyChanged(nameof(ArrowCanvasLeft));
        PullEditorFromLayout();
    }

    private static double SnapWorkbenchCoord(double v, double step) => Math.Round(v / step) * step;

    private static double ClampWorkbench(double v, double min, double max) => Math.Min(max, Math.Max(min, v));

    private static double NormalizeWorkbenchSnapStep(double step) => step <= 0 ? 8 : step;

    private void ApplyEditorAnchorsSnapped(double step)
    {
        if (SelectedLayoutElementItem == null) return;
        step = NormalizeWorkbenchSnapStep(step);
        var (xMin, xMax, yMin, yMax) = GetEditorAnchorClampRanges();
        var x = ClampWorkbench(SnapWorkbenchCoord(EditorAnchorX, step), xMin, xMax);
        var y = ClampWorkbench(SnapWorkbenchCoord(EditorAnchorY, step), yMin, yMax);
        _editorSync = true;
        try
        {
            EditorAnchorX = x;
            EditorAnchorY = y;
        }
        finally
        {
            _editorSync = false;
        }

        PushEditorToLayout();
        PullEditorFromLayout();
        OnPropertyChanged(nameof(ArrowCanvasLeft));
    }

    private void ApplyEditorAnchorDelta(double dx, double dy)
    {
        if (SelectedLayoutElementItem == null) return;
        var (xMin, xMax, yMin, yMax) = GetEditorAnchorClampRanges();
        var x = ClampWorkbench(EditorAnchorX + dx, xMin, xMax);
        var y = ClampWorkbench(EditorAnchorY + dy, yMin, yMax);
        _editorSync = true;
        try
        {
            EditorAnchorX = x;
            EditorAnchorY = y;
        }
        finally
        {
            _editorSync = false;
        }

        PushEditorToLayout();
        PullEditorFromLayout();
        OnPropertyChanged(nameof(ArrowCanvasLeft));
    }

    /// <summary>将当前编辑元素的 X/Y 吸附到所选步长的网格（与票面上拖拽磁吸一致）。</summary>
    [RelayCommand]
    private void SnapWorkbenchPositionToGrid()
    {
        if (SelectedLayoutElementItem == null) return;
        ApplyEditorAnchorsSnapped(WorkbenchLayoutSnapStepPixels);
    }

    private (double XMin, double XMax, double YMin, double YMax) GetEditorAnchorClampRanges() =>
        EditorUsesStationZhanGap
            ? (0, EditorAnchorXMaximum, EditorAnchorYMinimum, EditorAnchorYMaximum)
            : (0, WorkbenchFaceWidth, 0, WorkbenchFaceHeight);

    /// <summary>将当前编辑元素的 X/Y 取整到整数像素。</summary>
    [RelayCommand]
    private void RoundWorkbenchPositionToInteger()
    {
        if (SelectedLayoutElementItem == null) return;
        var (xMin, xMax, yMin, yMax) = GetEditorAnchorClampRanges();
        var x = ClampWorkbench(Math.Round(EditorAnchorX), xMin, xMax);
        var y = ClampWorkbench(Math.Round(EditorAnchorY), yMin, yMax);
        _editorSync = true;
        try
        {
            EditorAnchorX = x;
            EditorAnchorY = y;
        }
        finally
        {
            _editorSync = false;
        }

        PushEditorToLayout();
        PullEditorFromLayout();
        OnPropertyChanged(nameof(ArrowCanvasLeft));
    }

    /// <summary>按当前磁吸步长平移锚点（CommandParameter：L R U D）。</summary>
    [RelayCommand]
    private void NudgeWorkbenchPositionByStep(string? direction)
    {
        if (SelectedLayoutElementItem == null || string.IsNullOrWhiteSpace(direction)) return;
        var step = NormalizeWorkbenchSnapStep(WorkbenchLayoutSnapStepPixels);
        var (dx, dy) = direction.Trim().ToUpperInvariant() switch
        {
            "L" => (-step, 0d),
            "R" => (step, 0d),
            "U" => (0d, -step),
            "D" => (0d, step),
            _ => (0d, 0d)
        };

        if (dx == 0 && dy == 0) return;
        ApplyEditorAnchorDelta(dx, dy);
    }

    /// <summary>平移 1 像素（CommandParameter：L R U D）。</summary>
    [RelayCommand]
    private void NudgeWorkbenchPositionOnePixel(string? direction)
    {
        if (SelectedLayoutElementItem == null || string.IsNullOrWhiteSpace(direction)) return;
        var (dx, dy) = direction.Trim().ToUpperInvariant() switch
        {
            "L" => (-1d, 0d),
            "R" => (1d, 0d),
            "U" => (0d, -1d),
            "D" => (0d, 1d),
            _ => (0d, 0d)
        };

        if (dx == 0 && dy == 0) return;
        ApplyEditorAnchorDelta(dx, dy);
    }

    /// <summary>当前块「复制字号」缓存的数值：正文为字号，箭头为线粗，二维码为边长（与 <see cref="EditorExtra" /> 一致）。</summary>
    private double GetWorkbenchCopiedSizeValue()
    {
        return SelectedLayoutElementItem?.Kind == TicketFaceLayoutElementKind.Qr ? EditorExtra : EditorFontSize;
    }

    /// <summary>将缓存数值写入当前块：二维码写边长，箭头写线粗，其余写字号（按各块允许范围夹紧）。</summary>
    private void ApplyWorkbenchPastedSizeValue(double raw)
    {
        if (SelectedLayoutElementItem == null) return;
        switch (SelectedLayoutElementItem.Kind)
        {
            case TicketFaceLayoutElementKind.Qr:
                EditorExtra = Math.Clamp(raw, 40, 400);
                break;
            case TicketFaceLayoutElementKind.Arrow:
                EditorFontSize = Math.Clamp(raw, 0.5, 4);
                break;
            default:
                EditorFontSize = Math.Clamp(raw, 6, 72);
                break;
        }
    }

    /// <summary>复制当前元素的「字号」或箭头「线粗」或二维码「边长」，供粘贴到其他元素。</summary>
    [RelayCommand]
    private void CopyWorkbenchFontSize()
    {
        if (SelectedLayoutElementItem == null) return;
        CopiedWorkbenchFontSize = GetWorkbenchCopiedSizeValue();
    }

    /// <summary>将已复制的数值粘贴到当前元素（按目标类型解释为字号、线粗或边长，并夹紧）。</summary>
    [RelayCommand]
    private void PasteWorkbenchFontSize()
    {
        if (SelectedLayoutElementItem == null || !CopiedWorkbenchFontSize.HasValue) return;
        ApplyWorkbenchPastedSizeValue(CopiedWorkbenchFontSize.Value);
    }

    private static string ReadEditorFontFamilyFromLayout(string? elementFontFamily) =>
        string.IsNullOrWhiteSpace(elementFontFamily) ? string.Empty : elementFontFamily.Trim();

    private void PullEditorFromLayout()
    {
        if (SelectedLayoutElementItem == null) return;
        _editorSync = true;
        try
        {
            var L = ActiveLayout;
            switch (SelectedLayoutElementItem.Kind)
            {
                case TicketFaceLayoutElementKind.TicketSerial:
                    EditorAnchorX = L.TicketSerialLeft;
                    EditorAnchorY = L.TicketSerialTop;
                    EditorFontSize = L.TicketSerialFont;
                    EditorFontFamily = L.TicketSerialFontFamily ?? string.Empty;
                    break;
                case TicketFaceLayoutElementKind.CheckInLabel:
                    EditorAnchorX = L.CheckInLeft;
                    EditorAnchorY = L.CheckInTop;
                    EditorFontSize = L.CheckInFont;
                    EditorFontFamily = L.CheckInFontFamily ?? string.Empty;
                    break;
                case TicketFaceLayoutElementKind.CheckInValue:
                    EditorAnchorX = L.CheckInValueLeft;
                    EditorAnchorY = L.CheckInValueTop;
                    EditorFontSize = L.CheckInValueFont > 0.01 ? L.CheckInValueFont : L.CheckInFont;
                    EditorFontFamily = ReadEditorFontFamilyFromLayout(L.CheckInValueFontFamily);
                    break;
                case TicketFaceLayoutElementKind.DepartStation:
                    EditorAnchorX = StationFaceHanCountLayout.GetDepartCanvasLeft(L, CurrentDepartStationHanCount);
                    EditorAnchorY = L.DepartStationTop;
                    EditorFontSize = L.DepartStationNameFont > 0.01 ? L.DepartStationNameFont : L.StationNameFont;
                    EditorFontFamily = ReadEditorFontFamilyFromLayout(L.DepartStationNameFontFamily);
                    break;
                case TicketFaceLayoutElementKind.DepartStationZhan:
                    EditorAnchorX = L.DepartStationZhanGapLeft;
                    EditorAnchorY = L.DepartStationZhanOffsetTop;
                    EditorFontSize = L.DepartStationZhanFont;
                    EditorFontFamily = L.DepartStationZhanFontFamily ?? string.Empty;
                    break;
                case TicketFaceLayoutElementKind.DepartPinyin:
                    EditorAnchorX = L.DepartPinyinLeft;
                    EditorAnchorY = L.DepartPinyinTop;
                    EditorFontSize = L.PinyinFont;
                    EditorFontFamily = L.PinyinFontFamily ?? string.Empty;
                    break;
                case TicketFaceLayoutElementKind.TrainNo:
                    EditorAnchorX = L.TrainNoLeft;
                    EditorAnchorY = L.TrainNoTop;
                    EditorFontSize = L.TrainNoFont;
                    EditorFontFamily = L.TrainNoFontFamily ?? string.Empty;
                    break;
                case TicketFaceLayoutElementKind.Arrow:
                    EditorAnchorX = L.ArrowLeft;
                    EditorAnchorY = L.ArrowTop;
                    EditorFontSize = L.ArrowStrokeThickness > 0.05 ? L.ArrowStrokeThickness : 1.15;
                    EditorExtra = L.ArrowLength > 0.5 ? L.ArrowLength : 54;
                    EditorArrowHeadLength = L.ArrowHeadLength;
                    EditorFontFamily = L.ArrowFontFamily ?? string.Empty;
                    break;
                case TicketFaceLayoutElementKind.ArriveStation:
                    EditorAnchorX = StationFaceHanCountLayout.GetArriveCanvasLeft(L, CurrentArriveStationHanCount);
                    EditorAnchorY = L.ArriveStationTop;
                    EditorFontSize = L.ArriveStationNameFont > 0.01 ? L.ArriveStationNameFont : L.StationNameFont;
                    EditorFontFamily = ReadEditorFontFamilyFromLayout(L.ArriveStationNameFontFamily);
                    break;
                case TicketFaceLayoutElementKind.ArriveStationZhan:
                    EditorAnchorX = L.ArriveStationZhanGapLeft;
                    EditorAnchorY = L.ArriveStationZhanOffsetTop;
                    EditorFontSize = L.ArriveStationZhanFont;
                    EditorFontFamily = L.ArriveStationZhanFontFamily ?? string.Empty;
                    break;
                case TicketFaceLayoutElementKind.ArrivePinyin:
                    EditorAnchorX = L.ArrivePinyinLeft;
                    EditorAnchorY = L.ArrivePinyinTop;
                    EditorFontSize = L.PinyinFont;
                    EditorFontFamily = L.PinyinFontFamily ?? string.Empty;
                    break;
                case TicketFaceLayoutElementKind.DateYearDigits:
                    EditorAnchorX = L.DateYearDigitsLeft;
                    EditorAnchorY = L.DateYearDigitsTop;
                    EditorFontSize = L.DateYearDigitsFont;
                    EditorFontFamily = L.DateRowFontFamily ?? string.Empty;
                    break;
                case TicketFaceLayoutElementKind.DateNianChar:
                    EditorAnchorX = L.DateNianCharLeft;
                    EditorAnchorY = L.DateNianCharTop;
                    EditorFontSize = L.DateNianCharFont;
                    EditorFontFamily = L.DateRowFontFamily ?? string.Empty;
                    break;
                case TicketFaceLayoutElementKind.DateMonthDigits:
                    EditorAnchorX = L.DateMonthDigitsLeft;
                    EditorAnchorY = L.DateMonthDigitsTop;
                    EditorFontSize = L.DateMonthDigitsFont;
                    EditorFontFamily = L.DateRowFontFamily ?? string.Empty;
                    break;
                case TicketFaceLayoutElementKind.DateYueChar:
                    EditorAnchorX = L.DateYueCharLeft;
                    EditorAnchorY = L.DateYueCharTop;
                    EditorFontSize = L.DateYueCharFont;
                    EditorFontFamily = L.DateRowFontFamily ?? string.Empty;
                    break;
                case TicketFaceLayoutElementKind.DateDayDigits:
                    EditorAnchorX = L.DateDayDigitsLeft;
                    EditorAnchorY = L.DateDayDigitsTop;
                    EditorFontSize = L.DateDayDigitsFont;
                    EditorFontFamily = L.DateRowFontFamily ?? string.Empty;
                    break;
                case TicketFaceLayoutElementKind.DateRiChar:
                    EditorAnchorX = L.DateRiCharLeft;
                    EditorAnchorY = L.DateRiCharTop;
                    EditorFontSize = L.DateRiCharFont;
                    EditorFontFamily = L.DateRowFontFamily ?? string.Empty;
                    break;
                case TicketFaceLayoutElementKind.DateTimeHm:
                    EditorAnchorX = L.DateTimeHmLeft;
                    EditorAnchorY = L.DateTimeHmTop;
                    EditorFontSize = L.DateTimeHmFont;
                    EditorFontFamily = L.DateRowFontFamily ?? string.Empty;
                    break;
                case TicketFaceLayoutElementKind.DateKaiChar:
                    EditorAnchorX = L.DateKaiCharLeft;
                    EditorAnchorY = L.DateKaiCharTop;
                    EditorFontSize = L.DateKaiCharFont;
                    EditorFontFamily = L.DateRowFontFamily ?? string.Empty;
                    break;
                case TicketFaceLayoutElementKind.MoneyRow:
                case TicketFaceLayoutElementKind.MoneySymbol:
                    EditorAnchorX = L.MoneySymbolLeft;
                    EditorAnchorY = L.MoneySymbolTop;
                    EditorFontSize = L.MoneySymbolFont > 0.01 ? L.MoneySymbolFont : L.MoneyRowFont;
                    EditorFontFamily = L.MoneyRowFontFamily ?? string.Empty;
                    break;
                case TicketFaceLayoutElementKind.MoneyAmount:
                    EditorAnchorX = L.MoneyAmountLeft;
                    EditorAnchorY = L.MoneyAmountTop;
                    EditorFontSize = L.MoneyAmountFont > 0.01 ? L.MoneyAmountFont : L.MoneyRowFont;
                    EditorFontFamily = L.MoneyRowFontFamily ?? string.Empty;
                    break;
                case TicketFaceLayoutElementKind.MoneyUnit:
                    EditorAnchorX = L.MoneyUnitLeft;
                    EditorAnchorY = L.MoneyUnitTop;
                    EditorFontSize = L.MoneyUnitFont > 0.01 ? L.MoneyUnitFont : L.MoneyRowFont;
                    EditorFontFamily = L.MoneyRowFontFamily ?? string.Empty;
                    break;
                case TicketFaceLayoutElementKind.CoachSeat:
                case TicketFaceLayoutElementKind.CoachNumber:
                    EditorAnchorX = L.CoachNumberLeft;
                    EditorAnchorY = L.CoachNumberTop;
                    EditorFontSize = L.CoachNumberFont > 0.01 ? L.CoachNumberFont : L.CoachSeatFont;
                    EditorFontFamily = L.CoachSeatFontFamily ?? string.Empty;
                    break;
                case TicketFaceLayoutElementKind.CoachChe:
                    EditorAnchorX = L.CoachCheLeft;
                    EditorAnchorY = L.CoachCheTop;
                    EditorFontSize = L.CoachCheFont > 0.01 ? L.CoachCheFont : L.CoachSeatFont;
                    EditorFontFamily = L.CoachSeatFontFamily ?? string.Empty;
                    break;
                case TicketFaceLayoutElementKind.SeatNumber:
                    EditorAnchorX = L.SeatNumberLeft;
                    EditorAnchorY = L.SeatNumberTop;
                    EditorFontSize = L.SeatNumberFont > 0.01 ? L.SeatNumberFont : L.CoachSeatFont;
                    EditorFontFamily = L.CoachSeatFontFamily ?? string.Empty;
                    break;
                case TicketFaceLayoutElementKind.SeatHao:
                    EditorAnchorX = L.SeatHaoLeft;
                    EditorAnchorY = L.SeatHaoTop;
                    EditorFontSize = L.SeatHaoFont > 0.01 ? L.SeatHaoFont : L.CoachSeatFont;
                    EditorFontFamily = L.CoachSeatFontFamily ?? string.Empty;
                    break;
                case TicketFaceLayoutElementKind.SeatType:
                    EditorAnchorX = L.SeatTypeRight;
                    EditorAnchorY = L.SeatTypeTop;
                    EditorFontSize = L.SeatTypeFont;
                    EditorFontFamily = L.SeatTypeFontFamily ?? string.Empty;
                    break;
                case TicketFaceLayoutElementKind.TicketModificationType:
                    EditorAnchorX = L.TicketModificationTypeLeft;
                    EditorAnchorY = L.TicketModificationTypeTop;
                    EditorFontSize = L.TicketModificationTypeFont;
                    EditorFontFamily = L.TicketModificationTypeFontFamily ?? string.Empty;
                    break;
                case TicketFaceLayoutElementKind.Purpose:
                    EditorAnchorX = L.PurposeLeft;
                    EditorAnchorY = L.PurposeTop;
                    EditorFontSize = L.PurposeFont;
                    EditorFontFamily = L.PurposeFontFamily ?? string.Empty;
                    break;
                case TicketFaceLayoutElementKind.AdditionalInfo:
                    EditorAnchorX = L.AdditionalInfoLeft;
                    EditorAnchorY = L.AdditionalInfoTop;
                    EditorFontSize = L.AdditionalInfoFont;
                    EditorFontFamily = L.AdditionalInfoFontFamily ?? string.Empty;
                    break;
                case TicketFaceLayoutElementKind.IdName:
                    EditorAnchorX = L.IdNameLeft;
                    EditorAnchorY = L.IdNameTop;
                    EditorFontSize = L.IdNameFont;
                    EditorFontFamily = L.IdNameFontFamily ?? string.Empty;
                    break;
                case TicketFaceLayoutElementKind.HintBox:
                    EditorAnchorX = L.HintBoxLeft;
                    EditorAnchorY = L.HintBoxTop;
                    EditorFontSize = L.HintFont;
                    EditorFontFamily = L.HintFontFamily ?? string.Empty;
                    EditorExtra = L.HintBoxWidth;
                    break;
                case TicketFaceLayoutElementKind.Footer:
                    EditorAnchorX = L.FooterLeft;
                    EditorAnchorY = L.FooterTop;
                    EditorFontSize = L.FooterFont;
                    EditorFontFamily = L.FooterFontFamily ?? string.Empty;
                    break;
                case TicketFaceLayoutElementKind.Qr:
                    EditorAnchorX = L.QrLeft;
                    EditorAnchorY = L.QrTop;
                    EditorExtra = L.QrSize;
                    EditorFontSize = L.QrSize;
                    break;
                case TicketFaceLayoutElementKind.BadgeLetterXue:
                    EditorAnchorX = L.BadgeLetterXueLeft;
                    EditorAnchorY = L.BadgeLetterXueTop;
                    EditorFontSize = L.BadgeLetterXueFont;
                    EditorFontFamily = L.BadgeFontFamily ?? string.Empty;
                    break;
                case TicketFaceLayoutElementKind.BadgeLetterHai:
                    EditorAnchorX = L.BadgeLetterHaiLeft;
                    EditorAnchorY = L.BadgeLetterHaiTop;
                    EditorFontSize = L.BadgeLetterHaiFont;
                    EditorFontFamily = L.BadgeFontFamily ?? string.Empty;
                    break;
                case TicketFaceLayoutElementKind.BadgeLetterWang:
                    EditorAnchorX = L.BadgeLetterWangLeft;
                    EditorAnchorY = L.BadgeLetterWangTop;
                    EditorFontSize = L.BadgeLetterWangFont;
                    EditorFontFamily = L.BadgeFontFamily ?? string.Empty;
                    break;
                case TicketFaceLayoutElementKind.BadgeLetterDiscount:
                    EditorAnchorX = L.BadgeLetterDiscountLeft;
                    EditorAnchorY = L.BadgeLetterDiscountTop;
                    EditorFontSize = L.BadgeLetterDiscountFont;
                    EditorFontFamily = L.BadgeFontFamily ?? string.Empty;
                    break;
                case TicketFaceLayoutElementKind.BadgePaymentRow:
                    EditorAnchorX = L.BadgePaymentRowLeft;
                    EditorAnchorY = L.BadgePaymentRowTop;
                    EditorFontSize = L.BadgePaymentRowFont;
                    EditorFontFamily = L.BadgeFontFamily ?? string.Empty;
                    break;
            }

            OnPropertyChanged(nameof(EditorShowsExtraDimension));
            OnPropertyChanged(nameof(EditorExtraLabel));
            OnPropertyChanged(nameof(EditorShowsFontSize));
            OnPropertyChanged(nameof(EditorShowsFontFamily));
        }
        finally
        {
            _editorSync = false;
        }
    }

    private void PushEditorToLayout()
    {
        if (_editorSync || SelectedLayoutElementItem == null) return;
        var L = ActiveLayout;
        switch (SelectedLayoutElementItem.Kind)
        {
            case TicketFaceLayoutElementKind.TicketSerial:
                L.TicketSerialLeft = EditorAnchorX;
                L.TicketSerialTop = EditorAnchorY;
                L.TicketSerialFont = EditorFontSize;
                L.TicketSerialFontFamily = NullIfEmpty(EditorFontFamily);
                break;
            case TicketFaceLayoutElementKind.CheckInLabel:
                L.CheckInLeft = EditorAnchorX;
                L.CheckInTop = EditorAnchorY;
                L.CheckInFont = EditorFontSize;
                L.CheckInFontFamily = NullIfEmpty(EditorFontFamily);
                break;
            case TicketFaceLayoutElementKind.CheckInValue:
                L.CheckInValueLeft = EditorAnchorX;
                L.CheckInValueTop = EditorAnchorY;
                L.CheckInValueFont = EditorFontSize;
                L.CheckInValueFontFamily = NullIfEmpty(EditorFontFamily);
                break;
            case TicketFaceLayoutElementKind.DepartStation:
                ApplyDepartStationEffectiveLeft(L, EditorAnchorX);
                L.DepartStationTop = EditorAnchorY;
                L.DepartStationNameFont = EditorFontSize;
                L.DepartStationNameFontFamily = NullIfEmpty(EditorFontFamily);
                break;
            case TicketFaceLayoutElementKind.DepartStationZhan:
                L.DepartStationZhanGapLeft = EditorAnchorX;
                L.DepartStationZhanOffsetTop = EditorAnchorY;
                L.DepartStationZhanFont = EditorFontSize;
                L.DepartStationZhanFontFamily = NullIfEmpty(EditorFontFamily);
                break;
            case TicketFaceLayoutElementKind.DepartPinyin:
                L.DepartPinyinLeft = EditorAnchorX;
                L.DepartPinyinTop = EditorAnchorY;
                L.PinyinFont = EditorFontSize;
                L.PinyinFontFamily = NullIfEmpty(EditorFontFamily);
                break;
            case TicketFaceLayoutElementKind.TrainNo:
                L.TrainNoLeft = EditorAnchorX;
                L.TrainNoTop = EditorAnchorY;
                L.TrainNoFont = EditorFontSize;
                L.TrainNoFontFamily = NullIfEmpty(EditorFontFamily);
                break;
            case TicketFaceLayoutElementKind.Arrow:
                L.ArrowLeft = EditorAnchorX;
                L.ArrowTop = EditorAnchorY;
                L.ArrowStrokeThickness = EditorFontSize;
                L.ArrowLength = EditorExtra;
                L.ArrowHeadLength = EditorArrowHeadLength;
                L.ArrowFontFamily = NullIfEmpty(EditorFontFamily);
                L.ArrowFont = Math.Clamp(L.ArrowLength / 2.35, 12.0, 44.0);
                break;
            case TicketFaceLayoutElementKind.ArriveStation:
                ApplyArriveStationEffectiveLeft(L, EditorAnchorX);
                L.ArriveStationTop = EditorAnchorY;
                L.ArriveStationNameFont = EditorFontSize;
                L.ArriveStationNameFontFamily = NullIfEmpty(EditorFontFamily);
                break;
            case TicketFaceLayoutElementKind.ArriveStationZhan:
                L.ArriveStationZhanGapLeft = EditorAnchorX;
                L.ArriveStationZhanOffsetTop = EditorAnchorY;
                L.ArriveStationZhanFont = EditorFontSize;
                L.ArriveStationZhanFontFamily = NullIfEmpty(EditorFontFamily);
                break;
            case TicketFaceLayoutElementKind.ArrivePinyin:
                L.ArrivePinyinLeft = EditorAnchorX;
                L.ArrivePinyinTop = EditorAnchorY;
                L.PinyinFont = EditorFontSize;
                L.PinyinFontFamily = NullIfEmpty(EditorFontFamily);
                break;
            case TicketFaceLayoutElementKind.DateYearDigits:
                L.DateYearDigitsLeft = EditorAnchorX;
                L.DateYearDigitsTop = EditorAnchorY;
                L.DateYearDigitsFont = EditorFontSize;
                L.DateRowFontFamily = NullIfEmpty(EditorFontFamily);
                break;
            case TicketFaceLayoutElementKind.DateNianChar:
                L.DateNianCharLeft = EditorAnchorX;
                L.DateNianCharTop = EditorAnchorY;
                L.DateNianCharFont = EditorFontSize;
                L.DateRowFontFamily = NullIfEmpty(EditorFontFamily);
                break;
            case TicketFaceLayoutElementKind.DateMonthDigits:
                L.DateMonthDigitsLeft = EditorAnchorX;
                L.DateMonthDigitsTop = EditorAnchorY;
                L.DateMonthDigitsFont = EditorFontSize;
                L.DateRowFontFamily = NullIfEmpty(EditorFontFamily);
                break;
            case TicketFaceLayoutElementKind.DateYueChar:
                L.DateYueCharLeft = EditorAnchorX;
                L.DateYueCharTop = EditorAnchorY;
                L.DateYueCharFont = EditorFontSize;
                L.DateRowFontFamily = NullIfEmpty(EditorFontFamily);
                break;
            case TicketFaceLayoutElementKind.DateDayDigits:
                L.DateDayDigitsLeft = EditorAnchorX;
                L.DateDayDigitsTop = EditorAnchorY;
                L.DateDayDigitsFont = EditorFontSize;
                L.DateRowFontFamily = NullIfEmpty(EditorFontFamily);
                break;
            case TicketFaceLayoutElementKind.DateRiChar:
                L.DateRiCharLeft = EditorAnchorX;
                L.DateRiCharTop = EditorAnchorY;
                L.DateRiCharFont = EditorFontSize;
                L.DateRowFontFamily = NullIfEmpty(EditorFontFamily);
                break;
            case TicketFaceLayoutElementKind.DateTimeHm:
                L.DateTimeHmLeft = EditorAnchorX;
                L.DateTimeHmTop = EditorAnchorY;
                L.DateTimeHmFont = EditorFontSize;
                L.DateRowFontFamily = NullIfEmpty(EditorFontFamily);
                break;
            case TicketFaceLayoutElementKind.DateKaiChar:
                L.DateKaiCharLeft = EditorAnchorX;
                L.DateKaiCharTop = EditorAnchorY;
                L.DateKaiCharFont = EditorFontSize;
                L.DateRowFontFamily = NullIfEmpty(EditorFontFamily);
                break;
            case TicketFaceLayoutElementKind.MoneyRow:
            case TicketFaceLayoutElementKind.MoneySymbol:
                L.MoneySymbolLeft = EditorAnchorX;
                L.MoneySymbolTop = EditorAnchorY;
                L.MoneySymbolFont = EditorFontSize;
                L.MoneyRowLeft = EditorAnchorX;
                L.MoneyRowTop = EditorAnchorY;
                L.MoneyRowFont = EditorFontSize;
                L.MoneyRowFontFamily = NullIfEmpty(EditorFontFamily);
                break;
            case TicketFaceLayoutElementKind.MoneyAmount:
                L.MoneyAmountLeft = EditorAnchorX;
                L.MoneyAmountTop = EditorAnchorY;
                L.MoneyAmountFont = EditorFontSize;
                L.MoneyRowFontFamily = NullIfEmpty(EditorFontFamily);
                break;
            case TicketFaceLayoutElementKind.MoneyUnit:
                L.MoneyUnitLeft = EditorAnchorX;
                L.MoneyUnitTop = EditorAnchorY;
                L.MoneyUnitFont = EditorFontSize;
                L.MoneyRowFontFamily = NullIfEmpty(EditorFontFamily);
                break;
            case TicketFaceLayoutElementKind.CoachSeat:
            case TicketFaceLayoutElementKind.CoachNumber:
                L.CoachNumberLeft = EditorAnchorX;
                L.CoachNumberTop = EditorAnchorY;
                L.CoachNumberFont = EditorFontSize;
                L.CoachSeatRight = EditorAnchorX + 92;
                L.CoachSeatTop = EditorAnchorY;
                L.CoachSeatFont = EditorFontSize;
                L.CoachSeatFontFamily = NullIfEmpty(EditorFontFamily);
                break;
            case TicketFaceLayoutElementKind.CoachChe:
                L.CoachCheLeft = EditorAnchorX;
                L.CoachCheTop = EditorAnchorY;
                L.CoachCheFont = EditorFontSize;
                L.CoachSeatFontFamily = NullIfEmpty(EditorFontFamily);
                break;
            case TicketFaceLayoutElementKind.SeatNumber:
                L.SeatNumberLeft = EditorAnchorX;
                L.SeatNumberTop = EditorAnchorY;
                L.SeatNumberFont = EditorFontSize;
                L.CoachSeatFontFamily = NullIfEmpty(EditorFontFamily);
                break;
            case TicketFaceLayoutElementKind.SeatHao:
                L.SeatHaoLeft = EditorAnchorX;
                L.SeatHaoTop = EditorAnchorY;
                L.SeatHaoFont = EditorFontSize;
                L.CoachSeatFontFamily = NullIfEmpty(EditorFontFamily);
                break;
            case TicketFaceLayoutElementKind.SeatType:
                L.SeatTypeRight = EditorAnchorX;
                L.SeatTypeTop = EditorAnchorY;
                L.SeatTypeFont = EditorFontSize;
                L.SeatTypeFontFamily = NullIfEmpty(EditorFontFamily);
                break;
            case TicketFaceLayoutElementKind.TicketModificationType:
                L.TicketModificationTypeLeft = EditorAnchorX;
                L.TicketModificationTypeTop = EditorAnchorY;
                L.TicketModificationTypeFont = EditorFontSize;
                L.TicketModificationTypeFontFamily = NullIfEmpty(EditorFontFamily);
                break;
            case TicketFaceLayoutElementKind.Purpose:
                L.PurposeLeft = EditorAnchorX;
                L.PurposeTop = EditorAnchorY;
                L.PurposeFont = EditorFontSize;
                L.PurposeFontFamily = NullIfEmpty(EditorFontFamily);
                break;
            case TicketFaceLayoutElementKind.AdditionalInfo:
                L.AdditionalInfoLeft = EditorAnchorX;
                L.AdditionalInfoTop = EditorAnchorY;
                L.AdditionalInfoFont = EditorFontSize;
                L.AdditionalInfoFontFamily = NullIfEmpty(EditorFontFamily);
                break;
            case TicketFaceLayoutElementKind.IdName:
                L.IdNameLeft = EditorAnchorX;
                L.IdNameTop = EditorAnchorY;
                L.IdNameFont = EditorFontSize;
                L.IdNameFontFamily = NullIfEmpty(EditorFontFamily);
                break;
            case TicketFaceLayoutElementKind.HintBox:
                L.HintBoxLeft = EditorAnchorX;
                L.HintBoxTop = EditorAnchorY;
                L.HintFont = EditorFontSize;
                L.HintFontFamily = NullIfEmpty(EditorFontFamily);
                L.HintBoxWidth = Math.Max(40, EditorExtra);
                break;
            case TicketFaceLayoutElementKind.Footer:
                L.FooterLeft = EditorAnchorX;
                L.FooterTop = EditorAnchorY;
                L.FooterFont = EditorFontSize;
                L.FooterFontFamily = NullIfEmpty(EditorFontFamily);
                break;
            case TicketFaceLayoutElementKind.Qr:
                L.QrLeft = EditorAnchorX;
                L.QrTop = EditorAnchorY;
                L.QrSize = Math.Clamp(EditorExtra, 40, 400);
                break;
            case TicketFaceLayoutElementKind.BadgeLetterXue:
                L.BadgeLetterXueLeft = EditorAnchorX;
                L.BadgeLetterXueTop = EditorAnchorY;
                L.BadgeLetterXueFont = EditorFontSize;
                L.BadgeFontFamily = NullIfEmpty(EditorFontFamily);
                break;
            case TicketFaceLayoutElementKind.BadgeLetterHai:
                L.BadgeLetterHaiLeft = EditorAnchorX;
                L.BadgeLetterHaiTop = EditorAnchorY;
                L.BadgeLetterHaiFont = EditorFontSize;
                L.BadgeFontFamily = NullIfEmpty(EditorFontFamily);
                break;
            case TicketFaceLayoutElementKind.BadgeLetterWang:
                L.BadgeLetterWangLeft = EditorAnchorX;
                L.BadgeLetterWangTop = EditorAnchorY;
                L.BadgeLetterWangFont = EditorFontSize;
                L.BadgeFontFamily = NullIfEmpty(EditorFontFamily);
                break;
            case TicketFaceLayoutElementKind.BadgeLetterDiscount:
                L.BadgeLetterDiscountLeft = EditorAnchorX;
                L.BadgeLetterDiscountTop = EditorAnchorY;
                L.BadgeLetterDiscountFont = EditorFontSize;
                L.BadgeFontFamily = NullIfEmpty(EditorFontFamily);
                break;
            case TicketFaceLayoutElementKind.BadgePaymentRow:
                L.BadgePaymentRowLeft = EditorAnchorX;
                L.BadgePaymentRowTop = EditorAnchorY;
                L.BadgePaymentRowFont = EditorFontSize;
                L.BadgeFontFamily = NullIfEmpty(EditorFontFamily);
                break;
        }

        OnPropertyChanged(nameof(ArrowCanvasLeft));
    }

    private static string? NullIfEmpty(string s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private void ApplyDepartStationEffectiveLeft(ObservableTicketFaceLayout layout, double effectiveLeft)
    {
        var han = CurrentDepartStationHanCount;
        if (han == StationFaceHanCountLayout.ReferenceHanCount)
        {
            layout.DepartStationLeft = effectiveLeft;
            StationFaceHanCountLayout.SetDepartLeftOffset(layout, han, 0);
        }
        else if (han > 0)
            StationFaceHanCountLayout.SetDepartLeftOffset(layout, han, effectiveLeft - layout.DepartStationLeft);
        else
            layout.DepartStationLeft = effectiveLeft;

        OnPropertyChanged(nameof(PreviewDepartStationCanvasLeft));
    }

    private void ApplyArriveStationEffectiveLeft(ObservableTicketFaceLayout layout, double effectiveLeft)
    {
        var han = CurrentArriveStationHanCount;
        if (han == StationFaceHanCountLayout.ReferenceHanCount)
        {
            layout.ArriveStationLeft = effectiveLeft;
            StationFaceHanCountLayout.SetArriveLeftOffset(layout, han, 0);
        }
        else if (han > 0)
            StationFaceHanCountLayout.SetArriveLeftOffset(layout, han, effectiveLeft - layout.ArriveStationLeft);
        else
            layout.ArriveStationLeft = effectiveLeft;

        OnPropertyChanged(nameof(PreviewArriveStationCanvasLeft));
    }

    private void OnTicketFaceLayoutSegmentChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrEmpty(e.PropertyName)) return;

        // 811 票面 Canvas 经 Window RelativeSource 绑定 ActiveLayout.*；通知 ActiveLayout 以刷新坐标/字号。
        OnPropertyChanged(nameof(ActiveLayout));

        if (e.PropertyName == nameof(ObservableTicketFaceLayout.ArrowLeft))
            OnPropertyChanged(nameof(ArrowCanvasLeft));
        if (e.PropertyName is nameof(ObservableTicketFaceLayout.DepartStationCharacterSpacing)
            or nameof(ObservableTicketFaceLayout.ArriveStationCharacterSpacing)
            || e.PropertyName.StartsWith("DepartStationSpacing", StringComparison.Ordinal)
            || e.PropertyName.StartsWith("ArriveStationSpacing", StringComparison.Ordinal))
        {
            OnPropertyChanged(nameof(PreviewDepartStationText));
            OnPropertyChanged(nameof(PreviewArriveStationText));
            OnPropertyChanged(nameof(ActiveDepartStationCharacterSpacing));
            OnPropertyChanged(nameof(ActiveArriveStationCharacterSpacing));
        }

        if (e.PropertyName == nameof(ObservableTicketFaceLayout.DepartStationLeft)
            || e.PropertyName.StartsWith("DepartStationLeftOffset", StringComparison.Ordinal))
        {
            OnPropertyChanged(nameof(PreviewDepartStationCanvasLeft));
            if (SelectedLayoutElementItem?.Kind == TicketFaceLayoutElementKind.DepartStation && !_editorSync)
                PullEditorFromLayout();
        }

        if (e.PropertyName == nameof(ObservableTicketFaceLayout.ArriveStationLeft)
            || e.PropertyName.StartsWith("ArriveStationLeftOffset", StringComparison.Ordinal))
        {
            OnPropertyChanged(nameof(PreviewArriveStationCanvasLeft));
            if (SelectedLayoutElementItem?.Kind == TicketFaceLayoutElementKind.ArriveStation && !_editorSync)
                PullEditorFromLayout();
        }

        if (e.PropertyName is nameof(ObservableTicketFaceLayout.DepartStationZhanGapLeft)
            or nameof(ObservableTicketFaceLayout.DepartStationZhanOffsetTop))
            OnPropertyChanged(nameof(PreviewDepartStationZhanMargin));
        if (e.PropertyName is nameof(ObservableTicketFaceLayout.ArriveStationZhanGapLeft)
            or nameof(ObservableTicketFaceLayout.ArriveStationZhanOffsetTop))
            OnPropertyChanged(nameof(PreviewArriveStationZhanMargin));
    }

    private void TryLoadLayoutFromDefaultPath()
    {
        var path = TicketFaceLayoutJson.GetDefaultFilePath();
        if (!TicketFaceLayoutJson.TryLoadFromFile(path, out var dto) || dto == null) return;
        ApplyLayoutFileDto(dto);
    }

    private void ApplyLayoutFileDto(TicketFaceLayoutFileDto dto)
    {
        LayoutDefaultFontFamily = dto.DefaultFontFamily ?? string.Empty;
        if (dto.Blue != null) dto.Blue.ApplyTo(_layoutBlue);
        if (dto.Red != null) dto.Red.ApplyTo(_layoutRed);
        RefreshLayoutPresentationAfterFileLoad();
        RestoreWorkbenchSelectedElementFromDto(dto);
        if (SelectedLayoutElementItem == null)
            SelectedLayoutElementItem = LayoutElementItems[0];
        PullEditorFromLayout();
        // 等 Slider 完成绑定后再拉一次，避免 TwoWay 滑块把旧值写回布局。
        Application.Current?.Dispatcher.BeginInvoke(
            () =>
            {
                RefreshLayoutPresentationAfterFileLoad();
                PullEditorFromLayout();
            },
            System.Windows.Threading.DispatcherPriority.ApplicationIdle);
    }

    private void RefreshLayoutPresentationAfterFileLoad()
    {
        _layoutBlue.NotifyAllPropertiesChanged();
        _layoutRed.NotifyAllPropertiesChanged();
        OnPropertyChanged(nameof(ActiveLayout));
        OnPropertyChanged(nameof(ArrowCanvasLeft));
        OnPropertyChanged(nameof(PreviewDepartStationText));
        OnPropertyChanged(nameof(PreviewArriveStationText));
        OnPropertyChanged(nameof(PreviewDepartStationCanvasLeft));
        OnPropertyChanged(nameof(PreviewArriveStationCanvasLeft));
        OnPropertyChanged(nameof(PreviewDepartStationZhanMargin));
        OnPropertyChanged(nameof(PreviewArriveStationZhanMargin));
        OnPropertyChanged(nameof(ActiveDepartStationCharacterSpacing));
        OnPropertyChanged(nameof(ActiveArriveStationCharacterSpacing));
    }

    private void RestoreWorkbenchSelectedElementFromDto(TicketFaceLayoutFileDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.WorkbenchSelectedElement)) return;
        var raw = dto.WorkbenchSelectedElement.Trim();
        TicketFaceLayoutElementKind kind;
        if (string.Equals(raw, "CheckIn", StringComparison.OrdinalIgnoreCase))
            kind = TicketFaceLayoutElementKind.CheckInLabel;
        else if (!Enum.TryParse<TicketFaceLayoutElementKind>(raw, true, out kind))
            return;
        kind = kind switch
        {
            TicketFaceLayoutElementKind.MoneyRow => TicketFaceLayoutElementKind.MoneySymbol,
            TicketFaceLayoutElementKind.CoachSeat => TicketFaceLayoutElementKind.CoachNumber,
            _ => kind
        };
        foreach (var opt in LayoutElementItems)
        {
            if (opt.Kind != kind) continue;
            SelectedLayoutElementItem = opt;
            return;
        }
    }

    /// <summary>票面上点击命中某块时，同步左侧「编辑元素」下拉（需在「在票面上拖拽微调位置」开启时调用）。</summary>
    public void SelectWorkbenchLayoutElementByKind(TicketFaceLayoutElementKind kind)
    {
        if (!IsVisualLayoutEdit) return;
        foreach (var opt in LayoutElementItems)
        {
            if (opt.Kind != kind) continue;
            if (ReferenceEquals(SelectedLayoutElementItem, opt)) return;
            SelectedLayoutElementItem = opt;
            return;
        }
    }

    /// <summary>窗口 Loaded 后再拉一次编辑器，避免 Slider 初次绑定把错误值写回布局。</summary>
    public void ResyncLayoutEditorAfterViewLoaded() => PullEditorFromLayout();

    private void WireLayoutObservers()
    {
        _layoutBlue.PropertyChanged += OnTicketFaceLayoutSegmentChanged;
        _layoutRed.PropertyChanged += OnTicketFaceLayoutSegmentChanged;
    }

    private void UnwireLayoutObservers()
    {
        _layoutBlue.PropertyChanged -= OnTicketFaceLayoutSegmentChanged;
        _layoutRed.PropertyChanged -= OnTicketFaceLayoutSegmentChanged;
    }

    /// <summary>
    ///     「票面参数调整」窗口关闭时自动写入默认 JSON（与「写入默认路径」相同），下次打开预览会加载。
    ///     失败时提示（如无写权限），成功则静默。
    /// </summary>
    public void TryPersistLayoutOnWorkbenchClosing()
    {
        if (SessionMode != TicketPreviewSessionMode.LayoutWorkbench) return;
        try
        {
            var path = TicketFaceLayoutJson.GetDefaultFilePath();
            var dto = TicketFaceLayoutJson.BuildFileDto(LayoutDefaultFontFamily, _layoutBlue, _layoutRed,
                SelectedLayoutElementItem?.Kind.ToString());
            TicketFaceLayoutJson.SaveToFile(path, dto);
        }
        catch (Exception ex)
        {
            var owner = Application.Current?.MainWindow ?? GetLayoutDialogOwnerWindow();
            GuiPiao.View.MessageBoxWindow.Show(owner,
                $"关闭时自动保存票面布局失败：{ex.Message}\n\n可尝试「导出布局 JSON」保存到桌面等有写权限的位置，或检查程序目录下 Config 是否可写。",
                "票面布局",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    /// <summary>保存/打开对话框与提示框的父窗口：优先当前焦点窗口（票面预览），避免对话框叠在主窗后面像「没反应」。</summary>
    private static Window? GetLayoutDialogOwnerWindow()
    {
        var app = Application.Current;
        if (app?.Windows == null) return app?.MainWindow;
        try
        {
            var active = app.Windows.Cast<Window>().FirstOrDefault(w => w.IsActive);
            if (active != null) return active;
            var preview = app.Windows.OfType<GuiPiao.View.TicketPreviewWindow>().FirstOrDefault();
            return preview ?? app.MainWindow;
        }
        catch
        {
            return app.MainWindow;
        }
    }

    /// <summary>从 ttf/otf/ttc 解析族名并写入全局后备字体或当前编辑元素字体。</summary>
    [RelayCommand]
    private void PickWorkbenchFontFromFile(string? target)
    {
        if (!string.Equals(target, "default", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(target, "editor", StringComparison.OrdinalIgnoreCase))
            return;

        var dlg = new OpenFileDialog
        {
            Filter = "字体文件 (*.ttf;*.otf;*.ttc)|*.ttf;*.otf;*.ttc|所有文件|*.*"
        };
        var owner = GetLayoutDialogOwnerWindow();
        if (dlg.ShowDialog(owner) != true) return;
        if (!FontFamilyPickerSupport.TryResolveFamilySourceFromFontFile(dlg.FileName, out var src) ||
            string.IsNullOrWhiteSpace(src))
        {
            GuiPiao.View.MessageBoxWindow.Show(owner ?? Application.Current?.MainWindow,
                "无法从所选文件解析字体族，请更换字体文件或手动在左侧输入族名。", "字体",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.Equals(target, "default", StringComparison.OrdinalIgnoreCase))
            LayoutDefaultFontFamily = src.Trim();
        else
        {
            EditorFontFamily = src.Trim();
            InvalidateEditorFontPickerItems();
        }
    }

    [RelayCommand]
    private void SaveLayoutJson()
    {
        var owner = GetLayoutDialogOwnerWindow();
        var dlg = new SaveFileDialog
        {
            Filter = "JSON 布局|*.json|所有文件|*.*",
            FileName = "ticket-face-layout.json"
        };
        if (dlg.ShowDialog(owner) != true) return;
        try
        {
            var dto = TicketFaceLayoutJson.BuildFileDto(LayoutDefaultFontFamily, _layoutBlue, _layoutRed,
                SelectedLayoutElementItem?.Kind.ToString());
            TicketFaceLayoutJson.SaveToFile(dlg.FileName, dto);
            GuiPiao.View.MessageBoxWindow.Show(owner, $"已保存：{dlg.FileName}", "票面布局",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            GuiPiao.View.MessageBoxWindow.Show(owner, $"保存失败：{ex.Message}", "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void SaveLayoutJsonToDefaultPath()
    {
        var owner = GetLayoutDialogOwnerWindow();
        try
        {
            var path = TrySaveLayoutDtoToDefaultPath(null);
            if (path == null)
                throw new InvalidOperationException("无法写入默认路径。");
            GuiPiao.View.MessageBoxWindow.Show(owner,
                $"已写入程序默认路径：\n{path}\n启动时将自动加载。", "票面布局",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            GuiPiao.View.MessageBoxWindow.Show(owner, $"保存失败：{ex.Message}", "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>将布局写入程序目录 Config；<paramref name="dtoOverride" /> 非空时写该 DTO（载入 JSON 时用）。</summary>
    private string? TrySaveLayoutDtoToDefaultPath(TicketFaceLayoutFileDto? dtoOverride)
    {
        try
        {
            var path = TicketFaceLayoutJson.GetDefaultFilePath();
            var dto = dtoOverride ?? TicketFaceLayoutJson.BuildFileDto(LayoutDefaultFontFamily, _layoutBlue, _layoutRed,
                SelectedLayoutElementItem?.Kind.ToString());
            TicketFaceLayoutJson.SaveToFile(path, dto);
            return path;
        }
        catch
        {
            return null;
        }
    }

    [RelayCommand]
    private void LoadLayoutJson()
    {
        var owner = GetLayoutDialogOwnerWindow();
        var dlg = new OpenFileDialog { Filter = "JSON 布局|*.json|所有文件|*.*" };
        if (dlg.ShowDialog(owner) != true) return;
        try
        {
            var dto = TicketFaceLayoutJson.Deserialize(File.ReadAllText(dlg.FileName));
            if (dto == null)
            {
                GuiPiao.View.MessageBoxWindow.Show(owner, "JSON 解析失败。", "票面布局",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ApplyLayoutFileDto(dto);
            var saved = TrySaveLayoutDtoToDefaultPath(dto) != null;
            var message = saved ? "布局已载入并已保存。" : "布局已载入，未能保存到默认路径。";
            GuiPiao.View.MessageBoxWindow.Show(owner, message, "票面布局",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            GuiPiao.View.MessageBoxWindow.Show(owner, $"载入失败：{ex.Message}", "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void ResetLayoutToFactoryDefaults()
    {
        var owner = GetLayoutDialogOwnerWindow();
        var r = GuiPiao.View.MessageBoxWindow.Show(owner,
            "确定将红/蓝票面布局恢复为内置默认值吗？", "票面布局",
            MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (r != MessageBoxResult.Yes) return;
        LayoutDefaultFontFamily = string.Empty;
        _layoutBlue.ApplySnapshot(TicketFaceLayout.BlueDefault());
        _layoutRed.ApplySnapshot(TicketFaceLayout.RedDefault());
        OnPropertyChanged(nameof(ActiveLayout));
        OnPropertyChanged(nameof(ArrowCanvasLeft));
        OnPropertyChanged(nameof(PreviewDepartStationText));
        OnPropertyChanged(nameof(PreviewArriveStationText));
        PullEditorFromLayout();
    }
}
