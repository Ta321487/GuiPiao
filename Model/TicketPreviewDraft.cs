using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using GuiPiao.Utils;

namespace GuiPiao.Model;

/// <summary>
///     票面预览草稿：引用 <see cref="TripItem" /> 行程数据；身份与编码区及行程字段可在预览窗编辑，不写回数据库。
/// </summary>
public partial class TicketPreviewDraft : ObservableObject, IDisposable
{
    public TripItem Source { get; }

    [ObservableProperty] private string _idNumber = string.Empty;
    [ObservableProperty] private string _passengerName = string.Empty;
    [ObservableProperty] private string _idMask = string.Empty;

    /// <summary>身份证输入框短时红框（校验失败）</summary>
    [ObservableProperty] private bool _identityInputError;

    /// <summary>
    ///     本窗口内当前这份预览草稿：票种标志含「优惠」时，票面简字用「折」替代「惠」。与主列表只读预览或后续出票规则可不一致。同一窗口若载入多条行程草稿（如从列表一次打开多张票），每条草稿各自一份该选项；从设置进入示例预览时通常仅一条。
    /// </summary>
    [ObservableProperty] private bool _preferDiscountZhe = true;

    private bool _disposed;

    private static readonly string[] SourceDerivedPropertyNames =
    {
        nameof(DisplayTitle),
        nameof(MoneyFormN2),
        nameof(MoneyTicketN1),
        nameof(DepartTimeHm),
        nameof(FooterReceiptLine),
        nameof(DepartDateYear),
        nameof(DepartDateMonth),
        nameof(DepartDateDay),
        nameof(DepartDateChineseLine),
        nameof(DepartStationBodyPlain),
        nameof(ArriveStationBodyPlain),
        nameof(DepartStationSpaced),
        nameof(ArriveStationSpaced),
        nameof(DepartStationShowZhan),
        nameof(ArriveStationShowZhan),
        nameof(HasAdditionalInfo),
        nameof(ArrowOffsetAdjustPx),
        nameof(CoachBodyWithoutChe),
        nameof(CoachShowChe),
        nameof(ShowCoachCheOnFace),
        nameof(IsJiaGuaCoach),
        nameof(ShowCoachJiaOnFace),
        nameof(SeatBodyWithoutHao),
        nameof(SeatShowHao),
        nameof(ShowSeatHaoOnFace),
        nameof(ShowCoachSeatFaceSegments),
        nameof(IsWuzuo),
        nameof(IsSleeperBerth),
        nameof(SleeperBerthNumberPart),
        nameof(SleeperBerthSuffix),
        nameof(SuppressNetworkTicketBadge),
        nameof(HintLines)
    };

    public TicketPreviewDraft(TripItem source)
    {
        Source = source ?? throw new ArgumentNullException(nameof(source));
        Source.PropertyChanged += OnSourcePropertyChanged;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Source.PropertyChanged -= OnSourcePropertyChanged;
    }

    private void OnSourcePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender != Source || _disposed) return;
        foreach (var n in SourceDerivedPropertyNames)
            OnPropertyChanged(n);
    }

    public string DisplayTitle =>
        $"{Source.TrainNo} {Source.DepartDate} {Source.DepartStation}→{Source.ArriveStation}".Trim();

    public static string ComputeDefaultIdMask(string? idNumber)
    {
        var id = (idNumber ?? string.Empty).Trim().Replace(" ", "");
        if (id.Length == 0) return string.Empty;

        // 18 位：前 10 + **** + 后 4（与实体火车票一致）
        if (id.Length >= 18)
        {
            id = id[..18];
            return id[..10] + "****" + id[14..];
        }

        // 15 位旧证：前 8 + *** + 后 3
        if (id.Length == 15)
            return id[..8] + "***" + id[12..];

        // 输入过程中：超过 10 位开始对中间打码，便于票面即时预览
        if (id.Length > 10)
        {
            var tail = id.Length > 14 ? id[14..] : string.Empty;
            return id[..10] + "****" + tail;
        }

        return id;
    }

    partial void OnIdNumberChanged(string value)
    {
        // 始终按身份证号自动生成掩码（与真票一致），覆盖手动改过的掩码
        IdMask = ComputeDefaultIdMask(value);
    }

    /// <summary>左侧表单金额：¥ + N2</summary>
    public string MoneyFormN2
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Source.Money)) return string.Empty;
            var raw = Source.Money.Trim().TrimStart('¥', '￥');
            return decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var d)
                ? $"¥{d:N2}"
                : $"¥{raw}";
        }
    }

    /// <summary>票面金额：一位小数（文档与表单可不一致）</summary>
    public string MoneyTicketN1
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Source.Money)) return string.Empty;
            var raw = Source.Money.Trim().TrimStart('¥', '￥');
            return decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var d)
                ? d.ToString("0.0", CultureInfo.InvariantCulture)
                : raw;
        }
    }

    /// <summary>出发时间 HH:mm（表单与票面）</summary>
    public string DepartTimeHm
    {
        get
        {
            var t = Source.DepartTime?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(t)) return string.Empty;
            return RideDateTime.TryParseTime(t, out var ts)
                ? RideDateTime.FormatTime(ts)
                : t;
        }
    }

    public string FooterReceiptLine =>
        string.IsNullOrWhiteSpace(Source.TicketNumber)
            ? "报销凭证"
            : $"{Source.TicketNumber.Trim()} 报销凭证";

    public bool TryParseDepartDate(out DateTime date) =>
        RideDateTime.TryParseDate(Source.DepartDate, out date);

    public string DepartDateYear =>
        TryParseDepartDate(out var d) ? d.Year.ToString(CultureInfo.InvariantCulture) : string.Empty;

    public string DepartDateMonth =>
        TryParseDepartDate(out var d) ? d.Month.ToString(CultureInfo.InvariantCulture) : string.Empty;

    public string DepartDateDay =>
        TryParseDepartDate(out var d) ? d.Day.ToString(CultureInfo.InvariantCulture) : string.Empty;

    public string DepartDateChineseLine =>
        TryParseDepartDate(out var d) ? $"{d.Year}年{d.Month}月{d.Day}日" : (Source.DepartDate ?? string.Empty);

    /// <summary>出发站名去掉末尾「站」后的主体文本（票面发站块用）。</summary>
    public string DepartStationBodyPlain => TrimTrailingStation(Source.DepartStation);

    /// <summary>到达站名去掉末尾「站」后的主体文本。</summary>
    public string ArriveStationBodyPlain => TrimTrailingStation(Source.ArriveStation);

    /// <summary>票面发站主体：2～5 个汉字时由布局字间距参数在字间插入细空白；否则沿用 Unicode 分字间空。</summary>
    public string DepartStationSpaced =>
        FormatStationNameForPreviewFace(Source.DepartStation, 0);

    public string ArriveStationSpaced =>
        FormatStationNameForPreviewFace(Source.ArriveStation, 0);

    /// <summary>
    ///     去掉末尾「站」后格式化为票面主体字符串：<paramref name="characterSpacingUnits" /> 在 2～5 个纯汉字时控制字间细空白（约千分之一 em 量级）；其它情况忽略该参数。
    /// </summary>
    public static string FormatStationNameForPreviewFace(string? rawStation, int characterSpacingUnits)
    {
        var body = TrimTrailingStation(rawStation);
        if (string.IsNullOrWhiteSpace(body)) return string.Empty;
        var t = body.Trim();
        if (IsOneToFiveAllHanStationBody(t))
        {
            var n = Math.Clamp(characterSpacingUnits / 12, 0, 36);
            if (n <= 0) return t;
            var sep = new string('\u200A', n);
            return string.Join(sep, t.ToCharArray());
        }

        return FormatTicketStationBodyForFace(t);
    }

    /// <summary>是否为 1～5 个中日韩统一表意文字（用于启用字间距/按字数左边距）。</summary>
    public static bool IsOneToFiveAllHanStationBody(string? body)
    {
        if (string.IsNullOrWhiteSpace(body)) return false;
        var t = body.Trim();
        if (t.Length is < 1 or > 5) return false;
        foreach (var ch in t)
            if (!IsCjkUnifiedIdeograph(ch))
                return false;
        return true;
    }

    /// <summary>是否为 2～5 个中日韩统一表意文字（用于启用字间距调节）。</summary>
    public static bool IsTwoToFiveAllHanStationBody(string? body)
    {
        if (string.IsNullOrWhiteSpace(body)) return false;
        var t = body.Trim();
        if (t.Length is < 2 or > 5) return false;
        foreach (var ch in t)
            if (!IsCjkUnifiedIdeograph(ch))
                return false;
        return true;
    }

    private static bool IsCjkUnifiedIdeograph(char c) =>
        c is >= '\u4E00' and <= '\u9FFF' or >= '\u3400' and <= '\u4DBF';

    private static string FormatTicketStationBodyForFace(string body)
    {
        if (string.IsNullOrWhiteSpace(body)) return string.Empty;
        var t = body.Trim();
        if (IsTwoToFiveAllHanStationBody(t)) return t;
        return InsertLegacyUnicodeTrackingBetweenChars(t);
    }

    private static string InsertLegacyUnicodeTrackingBetweenChars(string t)
    {
        if (t.Length <= 2) return t;
        var sp = t.Length >= 5 ? "\u2004" : "\u2006"; // 三全角空 / 六分空
        return string.Join(sp, t.ToCharArray());
    }

    public static string TrimTrailingStation(string? station)
    {
        if (string.IsNullOrWhiteSpace(station)) return string.Empty;
        var t = station.Trim();
        return t.EndsWith("站", StringComparison.Ordinal) && t.Length > 1 ? t[..^1] : t;
    }

    public bool DepartStationShowZhan =>
        Source.DepartStation != null && Source.DepartStation.TrimEnd().EndsWith("站", StringComparison.Ordinal);

    public bool ArriveStationShowZhan =>
        Source.ArriveStation != null && Source.ArriveStation.TrimEnd().EndsWith("站", StringComparison.Ordinal);

    public bool HasAdditionalInfo => !string.IsNullOrWhiteSpace(Source.AdditionalInfo);

    /// <summary>非卧铺时显示分段车厢/座位号块。</summary>
    public bool ShowCoachSeatFaceSegments => !IsSleeperBerth;

    public bool ShowCoachCheOnFace => CoachShowChe && ShowCoachSeatFaceSegments;

    /// <summary>加挂车厢时显示「加」字。</summary>
    public bool ShowCoachJiaOnFace => IsJiaGuaCoach && ShowCoachSeatFaceSegments;

    public bool ShowSeatHaoOnFace => SeatShowHao && ShowCoachSeatFaceSegments;

    /// <summary>箭头水平微调：站名越长略右移（简化版）</summary>
    public double ArrowOffsetAdjustPx
    {
        get
        {
            var len = (TrimTrailingStation(Source.DepartStation)?.Length ?? 0) +
                      (TrimTrailingStation(Source.ArriveStation)?.Length ?? 0);
            return Math.Min(40, Math.Max(0, (len - 6) * 3));
        }
    }

    public string CoachBodyWithoutChe
    {
        get
        {
            var c = Source.CoachNo?.Trim() ?? string.Empty;
            if (c.Length == 0) return string.Empty;
            c = c.Replace("加挂", "", StringComparison.Ordinal).Replace("加", "", StringComparison.Ordinal);
            return c.EndsWith("车", StringComparison.Ordinal) && c.Length > 1 ? c[..^1] : c;
        }
    }

    public bool CoachShowChe =>
        !string.IsNullOrWhiteSpace(Source.CoachNo) && Source.CoachNo.Trim().EndsWith("车", StringComparison.Ordinal);

    public bool IsJiaGuaCoach =>
        Source.CoachNo != null &&
        (Source.CoachNo.Contains("加挂", StringComparison.Ordinal) || Source.CoachNo.Contains("加", StringComparison.Ordinal));

    public string SeatBodyWithoutHao
    {
        get
        {
            var s = Source.SeatNo?.Trim() ?? string.Empty;
            if (s.Length == 0) return string.Empty;
            // 卧铺：去掉末尾 上/中/下 铺
            s = Regex.Replace(s, "[上中下]铺?$", "", RegexOptions.None);
            return s.EndsWith("号", StringComparison.Ordinal) && s.Length > 1 ? s[..^1] : s;
        }
    }

    public bool SeatShowHao => !string.IsNullOrWhiteSpace(Source.SeatNo?.Trim());

    public bool IsWuzuo =>
        Source.SeatNo != null && (Source.SeatNo.Contains("无座", StringComparison.Ordinal) ||
                                  (Source.SeatType?.Contains("无座", StringComparison.Ordinal) ?? false));

    public bool IsSleeperBerth =>
        (Source.SeatType?.Contains("硬卧", StringComparison.Ordinal) == true ||
         Source.SeatType?.Contains("软卧", StringComparison.Ordinal) == true) &&
        Source.SeatNo != null &&
        Regex.IsMatch(Source.SeatNo, "[上中下]");

    public string SleeperBerthNumberPart
    {
        get
        {
            var s = Source.SeatNo?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(s)) return string.Empty;
            return Regex.Replace(s, "[上中下]铺?", "", RegexOptions.None).TrimEnd('号');
        }
    }

    public string SleeperBerthSuffix
    {
        get
        {
            var s = Source.SeatNo?.Trim() ?? string.Empty;
            if (s.Contains('上')) return "上铺";
            if (s.Contains('中')) return "中铺";
            if (s.Contains('下')) return "下铺";
            return string.Empty;
        }
    }

    /// <summary>支付含支付宝/微信时，票面不再单独显示「网」标（文档业务规则）</summary>
    public bool SuppressNetworkTicketBadge
    {
        get
        {
            var p = Source.PaymentChannel ?? string.Empty;
            return p.Contains("支付宝", StringComparison.Ordinal) || p.Contains("微信", StringComparison.Ordinal);
        }
    }

    public IReadOnlyList<string> HintLines
    {
        get
        {
            var h = Source.Hint?.Trim();
            if (string.IsNullOrEmpty(h)) return Array.Empty<string>();
            return h.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        }
    }

    /// <summary>票种简字：学、孩、网、折/惠（不含支付支微银等）。</summary>
    public IReadOnlyList<string> TypeBadgeLetters(bool preferZheInsteadOfHui)
    {
        var list = new List<string>();
        void AddOne(string s)
        {
            if (!string.IsNullOrEmpty(s) && !list.Contains(s)) list.Add(s);
        }

        var tt = Source.TicketType ?? string.Empty;
        if (tt.Contains("学生", StringComparison.Ordinal)) AddOne("学");
        if (tt.Contains("儿童", StringComparison.Ordinal)) AddOne("孩");
        if (!SuppressNetworkTicketBadge && tt.Contains("网络", StringComparison.Ordinal)) AddOne("网");
        if (tt.Contains("优惠", StringComparison.Ordinal)) AddOne(preferZheInsteadOfHui ? "折" : "惠");
        return list;
    }

    /// <summary>支付渠道简字（支、微、银行等）。</summary>
    public IReadOnlyList<string> PaymentBadgeLetters()
    {
        var list = new List<string>();
        void AddOne(string s)
        {
            if (!string.IsNullOrEmpty(s) && !list.Contains(s)) list.Add(s);
        }

        var pc = Source.PaymentChannel ?? string.Empty;
        if (pc.Contains("支付宝", StringComparison.Ordinal)) AddOne("支");
        if (pc.Contains("微信", StringComparison.Ordinal)) AddOne("微");
        var bankPairs = new (string Key, string Ch)[]
        {
            ("农业银行", "农"), ("建设银行", "建"), ("工商银行", "工"), ("招商银行", "招"),
            ("邮储银行", "邮"), ("中国银行", "中"), ("交通银行", "交")
        };
        foreach (var (key, ch) in bankPairs)
            if (pc.Contains(key, StringComparison.Ordinal))
                AddOne(ch);

        return list;
    }

    /// <summary>票种/支付简字（票面小标，顺序：票种在前、支付在后）。</summary>
    public IReadOnlyList<string> TicketTypeBadgeLetters(bool preferZheInsteadOfHui)
    {
        var list = new List<string>();
        foreach (var s in TypeBadgeLetters(preferZheInsteadOfHui))
            list.Add(s);
        foreach (var s in PaymentBadgeLetters())
            if (!list.Contains(s)) list.Add(s);
        return list;
    }
}
