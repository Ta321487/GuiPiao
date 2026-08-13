using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using GuiPiao.Model;

namespace GuiPiao.Services;

/// <summary>
///     从票面 OCR / 短信 / 订单详情截图文本抽取行程字段。
///     纸质票：发到站站 · 车次 · 日期时间「开」·「XX车XXX号」· ￥票价 · 席别 · 检票口。
///     订单详情：标签字段（车次/出发站/到达时间…）、箭头站对、常有到站时刻。
///     第三方短信（智行/携程/飞猪等）：签名识别 + 区间/时刻对/从…前往… 等常见句式。
/// </summary>
public class TicketTextExtractor
{
    /// <summary>票面/短信常见席别 → 表单下拉选项。</summary>
    private static readonly Dictionary<string, string> SeatTypeAliases = new(StringComparer.Ordinal)
    {
        ["商务座"] = "商务座",
        ["特等座"] = "特等座",
        ["一等座"] = "一等座",
        ["二等座"] = "二等座",
        ["软座"] = "软座",
        ["硬卧代硬座"] = "硬卧代硬座",
        ["新空调硬座"] = "新空调硬座",
        ["新空调硬卧"] = "新空调硬卧",
        ["新空调软卧"] = "新空调软卧",
        ["硬座"] = "新空调硬座",
        ["硬卧"] = "新空调硬卧",
        ["软卧"] = "新空调软卧",
        ["高级软卧"] = "新空调软卧",
        ["一等卧"] = "新空调软卧",
        ["二等卧"] = "新空调硬卧",
        ["动卧"] = "新空调软卧"
    };

    private static readonly string[] SeatTypeProbeOrder =
        SeatTypeAliases.Keys.OrderByDescending(k => k.Length).ToArray();

    private static readonly HashSet<string> NonStationNames = new(StringComparer.Ordinal)
    {
        "售票", "检票", "进站", "出站", "候车", "退票", "改签", "仅供报销使用",
        "出发", "到达", "乘车", "订单", "详情", "电子客票"
    };

    // —— 车次：票面多为 K1020；短信/订单多为 K1020次 / 车次：G1234 ——
    private static readonly Regex TrainNoLabeledRegex = new(
        @"车次\s*[:：]?\s*([GDCKTZSYL]?\d{1,4})\s*次?",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex TrainNoWithCiRegex = new(
        @"([GDCKTZSYL]\d{1,4}|\d{1,4})\s*次",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex TrainNoBareRegex = new(
        @"(?<![A-Za-z0-9])([GDCKTZSYL]\d{1,4})(?![A-Za-z0-9])",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // —— 发到站：票面「九江站」；短信「北京南-上海虹桥」；订单「北京南 → 上海虹桥」——
    private static readonly Regex StationPairDashRegex = new(
        @"([\u4e00-\u9fa5]{2,15}?)\s*[-—–~～到至→➝➜＞>]\s*([\u4e00-\u9fa5]{2,15}?)" +
        @"(?=\s*(?:\d{1,2}\s*[:：]\s*\d{2}|开|检票|[GDCKTZSYL]?\d|车|号|座|卧|票价|[￥¥]|$))",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex StationDepartLabeledRegex = new(
        @"(?:出发站|发站)\s*[:：]?\s*([\u4e00-\u9fa5]{2,15})",
        RegexOptions.Compiled);

    private static readonly Regex StationArriveLabeledRegex = new(
        @"(?:到达站|到站|终点站)\s*[:：]?\s*([\u4e00-\u9fa5]{2,15})",
        RegexOptions.Compiled);

    /// <summary>携程类：「从广州前往深圳」。</summary>
    private static readonly Regex StationFromToRegex = new(
        @"从\s*([\u4e00-\u9fa5]{2,15})\s*(?:前往|到|至)\s*([\u4e00-\u9fa5]{2,15})",
        RegexOptions.Compiled);

    /// <summary>飞猪/OTA：「出发地为北京南…目的地为上海虹桥」。</summary>
    private static readonly Regex StationOriginDestRegex = new(
        @"出发地\s*[:：为]?\s*([\u4e00-\u9fa5]{2,15}).{0,40}?目的地\s*[:：为]?\s*([\u4e00-\u9fa5]{2,15})",
        RegexOptions.Compiled);

    /// <summary>智行类：「区间：北京南→上海虹桥」。</summary>
    private static readonly Regex StationQuJianRegex = new(
        @"区间\s*[:：]?\s*([\u4e00-\u9fa5]{2,15})\s*[-—–~～到至→➝➜]\s*([\u4e00-\u9fa5]{2,15})",
        RegexOptions.Compiled);

    /// <summary>智行紧凑：「您订购的3月15日G1234次北京南到上海虹桥」。</summary>
    private static readonly Regex ZhiXingDingGouRegex = new(
        @"您订购的\s*(\d{1,2})\s*月\s*(\d{1,2})\s*日\s*([GDCKTZSYL]\d{1,4})\s*次\s*" +
        @"([\u4e00-\u9fa5]{2,15})\s*到\s*([\u4e00-\u9fa5]{2,15})",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex StationWithZhanRegex = new(
        @"([\u4e00-\u9fa5]{2,15})站",
        RegexOptions.Compiled);

    /// <summary>铁路客服旧短信：「09:00武汉站发车」。</summary>
    private static readonly Regex TimeStationFaCheRegex = new(
        @"(\d{1,2})\s*[:：]\s*(\d{2})\s*([\u4e00-\u9fa5]{2,15})站发车",
        RegexOptions.Compiled);

    // —— 票面日期行：2024年01月25日18:55开 ——
    private static readonly Regex PaperDateTimeKaiRegex = new(
        @"(?:(\d{4})\s*年)?\s*(\d{1,2})\s*月\s*(\d{1,2})\s*日\s*(\d{1,2})\s*[:：]\s*(\d{2})\s*开",
        RegexOptions.Compiled);

    /// <summary>订单详情常见：日期与时刻同行，无「开」字。</summary>
    private static readonly Regex OrderDateTimeRegex = new(
        @"(?:(\d{4})\s*年)?\s*(\d{1,2})\s*月\s*(\d{1,2})\s*日\s+(\d{1,2})\s*[:：]\s*(\d{2})",
        RegexOptions.Compiled);

    private static readonly Regex IsoDateTimeRegex = new(
        @"(\d{4})\s*[-/.]\s*(\d{1,2})\s*[-/.]\s*(\d{1,2})\s+(\d{1,2})\s*[:：]\s*(\d{2})",
        RegexOptions.Compiled);

    private static readonly Regex DateYmdRegex = new(
        @"(?:(\d{4})\s*年)?\s*(\d{1,2})\s*月\s*(\d{1,2})\s*日",
        RegexOptions.Compiled);

    private static readonly Regex DateIsoRegex = new(
        @"(\d{4})\s*[-/.]\s*(\d{1,2})\s*[-/.]\s*(\d{1,2})",
        RegexOptions.Compiled);

    private static readonly Regex DepartDateLabeledRegex = new(
        @"(?:乘车日期|出发日期|开车日期)\s*[:：]?\s*(?:(\d{4})\s*[年\-/.])?\s*(\d{1,2})\s*[月\-/.]\s*(\d{1,2})",
        RegexOptions.Compiled);

    private static readonly Regex DepartTimeLabeledRegex = new(
        @"(?:出发时间|开车时间)\s*[:：]?\s*(?:次日\s*)?(\d{1,2})\s*[:：]\s*(\d{2})",
        RegexOptions.Compiled);

    private static readonly Regex ArriveTimeLabeledRegex = new(
        @"(?:到达时间|到站时间)\s*[:：]?\s*(?:次日\s*)?(\d{1,2})\s*[:：]\s*(\d{2})",
        RegexOptions.Compiled);

    private static readonly Regex TimeKaiRegex = new(
        @"(\d{1,2})\s*[:：]\s*(\d{2})\s*开",
        RegexOptions.Compiled);

    private static readonly Regex TimeDaoRegex = new(
        @"(\d{1,2})\s*[:：]\s*(\d{2})\s*到(?!达站|站)",
        RegexOptions.Compiled);

    private static readonly Regex TimeBareRegex = new(
        @"(\d{1,2})\s*[:：]\s*(\d{2})",
        RegexOptions.Compiled);

    /// <summary>智行等 OTA：「08:15-12:48」出发-到达。</summary>
    private static readonly Regex TimeRangeRegex = new(
        @"(\d{1,2})\s*[:：]\s*(\d{2})\s*[-—–~～]\s*(\d{1,2})\s*[:：]\s*(\d{2})",
        RegexOptions.Compiled);

    /// <summary>
    ///     国铁票面标准：「加?XX车XXX号」或「XX车XXA」(动车) 或「XX车XXX号上/中/下铺」。
    /// </summary>
    private static readonly Regex PaperCoachSeatRegex = new(
        @"(?<jia>加)?\s*(?<coach>\d{1,2})\s*车\s*(?:" +
        @"(?<noseat>无座|不对号入座)|" +
        @"(?<seatnum>\d{1,3})\s*号\s*(?<berth>上铺|中铺|下铺)?|" +
        @"(?<seatletter>\d{1,2}[A-Fa-f])\s*号?" +
        @")",
        RegexOptions.Compiled);

    /// <summary>铁路客服旧短信：「编号1F，12号车」或「02B号车」（座位在前/贴在号车前）。</summary>
    private static readonly Regex ClassicSeatThenCoachRegex = new(
        @"(?:编号\s*)?(?<seat>\d{1,2}[A-Fa-f])\s*[,，]?\s*(?<coach>\d{1,2})\s*号车",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex ClassicCoachHaoCheRegex = new(
        @"(?<coach>\d{1,2})\s*号车",
        RegexOptions.Compiled);

    /// <summary>OCR 漏掉车厢数字时：仅剩「车104号」→ 只填座位。</summary>
    private static readonly Regex OcrCheSeatOnlyRegex = new(
        @"(?<![0-9])车\s*(?<seatnum>\d{1,3})\s*号(?:(?<berth>上铺|中铺|下铺))?",
        RegexOptions.Compiled);

    private static readonly Regex OrderNoPlatformRegex = new(
        @"(?:订单号|订单编号|订单)\s*[:：]?\s*([A-Za-z0-9]{6,24})",
        RegexOptions.Compiled);

    private static readonly Regex StandaloneNoSeatRegex = new(
        @"不对号入座|(?<![A-Za-z0-9\u4e00-\u9fa5])无座(?![A-Za-z0-9\u4e00-\u9fa5])",
        RegexOptions.Compiled);

    private static readonly Regex SeatTypeLabeledRegex = new(
        @"席别\s*[:：]?\s*([\u4e00-\u9fa5]{2,10})",
        RegexOptions.Compiled);

    /// <summary>优先「票价」；其次 ￥；避免订单页保险费/服务费等其它「xx元」。</summary>
    private static readonly Regex MoneyTicketPriceRegex = new(
        @"票价\s*[:：]?\s*[￥¥]?\s*(\d+(?:\.\d{1,2})?)\s*元?",
        RegexOptions.Compiled);

    private static readonly Regex MoneyYenRegex = new(
        @"[￥¥]\s*(\d+(?:\.\d{1,2})?)\s*元?",
        RegexOptions.Compiled);

    private static readonly Regex MoneyYuanLooseRegex = new(
        @"(\d+(?:\.\d{1,2})?)\s*元",
        RegexOptions.Compiled);

    private static readonly Regex CheckInRegex = new(
        @"检票(?:口|闸机|位置)?\s*[:：]?\s*([A-Za-z0-9\-]+)",
        RegexOptions.Compiled);

    private static readonly Regex TicketNoRegex = new(
        @"(?:取票号|票号|电子客票号)\s*[:：]?\s*([A-Za-z0-9]{6,20})",
        RegexOptions.Compiled);

    public TicketImportDraft Extract(string rawText)
    {
        var draft = new TicketImportDraft
        {
            RawText = rawText?.Trim() ?? string.Empty,
            SourceHint = GuessSource(rawText)
        };

        if (string.IsNullOrWhiteSpace(rawText))
        {
            draft.FieldsNeedingReview.Add("全文为空");
            return draft;
        }

        var text = Normalize(rawText);

        // 智行紧凑句一次带出日期/车次/站
        TryApplyZhiXingDingGou(text, draft);

        draft.TrainNo ??= TryExtractTrainNo(text);
        TryExtractStations(text, draft);
        TryExtractDateTime(text, draft);
        TryExtractArriveTime(text, draft);
        TryExtractCoachSeat(text, draft);
        TryExtractSeatType(text, draft);
        draft.MoneyText = TryExtractMoney(text);
        draft.CheckInLocation = MatchGroup(CheckInRegex, text, 1);
        draft.TicketNumber = MatchGroup(TicketNoRegex, text, 1)
                             ?? MatchGroup(OrderNoPlatformRegex, text, 1);

        MarkMissing(draft);
        return draft;
    }

    public static string? MapSeatTypeToFormOption(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        return SeatTypeAliases.TryGetValue(raw.Trim(), out var mapped) ? mapped : null;
    }

    private static string Normalize(string text)
    {
        var s = StripHtmlIfPresent(text)
            .Replace('\u3000', ' ')
            .Replace("，", " ")
            .Replace(",", " ")
            .Replace("：", ":");

        s = Regex.Replace(s, @"\s+", " ");
        // 「04 车 019 号」→「04车019号」
        s = Regex.Replace(s, @"(\d{1,2})\s*车\s*", "$1车");
        s = Regex.Replace(s, @"车\s*(\d{1,3})\s*号", "车$1号");
        s = Regex.Replace(s, @"(\d{1,3})\s*号", "$1号");
        s = Regex.Replace(s, @"(\d{1,2})\s*:\s*(\d{2})\s*开", "$1:$2开");
        s = Regex.Replace(s, @"(\d{1,2})\s*:\s*(\d{2})\s*到", "$1:$2到");
        return s;
    }

    /// <summary>购票邮件 HTML：去标签后按纯文本规则抽。</summary>
    private static string StripHtmlIfPresent(string text)
    {
        if (string.IsNullOrEmpty(text) || text.IndexOf('<') < 0)
            return text;

        var s = Regex.Replace(text, @"<(script|style)[^>]*>[\s\S]*?</\1>", " ",
            RegexOptions.IgnoreCase);
        s = Regex.Replace(s, @"<(br|BR)\s*/?>", "\n");
        s = Regex.Replace(s, @"</(p|div|tr|li|h\d)\s*>", "\n", RegexOptions.IgnoreCase);
        s = Regex.Replace(s, @"<[^>]+>", " ");
        s = WebUtility.HtmlDecode(s);
        return s;
    }

    private static string GuessSource(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "未知";

        if (ContainsAny(text, "【智行】", "智行火车票", "zhih.co"))
            return "智行";
        if (ContainsAny(text, "【携程", "携程旅行", "携程网", "携程火车票"))
            return "携程";
        if (ContainsAny(text, "【去哪儿", "去哪儿网", "去哪儿旅行"))
            return "去哪儿";
        if (ContainsAny(text, "【飞猪", "飞猪旅行"))
            return "飞猪";
        if (ContainsAny(text, "【同程", "同程旅行", "同程艺龙"))
            return "同程";
        if (ContainsAny(text, "【美团", "美团火车票"))
            return "美团";
        if (ContainsAny(text, "高铁管家", "【高铁管家"))
            return "高铁管家";

        if (ContainsAny(text, "行程分享", "【行程分享", "分享行程", "邀请你查看行程"))
            return "分享卡";
        if (ContainsAny(text, "本人车票", "未出行", "已支付车票"))
            return "本人车票";
        if (text.Contains('<') &&
            (ContainsAny(text, "<html", "<body", "<table", "<br", "&nbsp;") ||
             ContainsAny(text, "购票成功", "出票成功", "电子客票")))
            return "邮件";

        if (text.Contains("订单详情", StringComparison.Ordinal) ||
            text.Contains("电子客票", StringComparison.Ordinal) ||
            text.Contains("乘车人", StringComparison.Ordinal) ||
            (text.Contains("出发站", StringComparison.Ordinal) &&
             text.Contains("到达站", StringComparison.Ordinal)))
            return "订单";
        if (ContainsAny(text, "12306", "铁路12306", "【铁路客服】", "铁路客服"))
            return "短信";
        if (Regex.IsMatch(text, @"[\u4e00-\u9fa5]{2,}站") &&
            (text.Contains('开') || Regex.IsMatch(text, @"\d{1,2}车\d{1,3}号") || text.Contains('￥') ||
             text.Contains('¥')))
            return "票面";
        return "文本";
    }

    private static bool ContainsAny(string text, params string[] needles)
    {
        foreach (var n in needles)
        {
            if (text.Contains(n, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static void TryApplyZhiXingDingGou(string text, TicketImportDraft draft)
    {
        var m = ZhiXingDingGouRegex.Match(text);
        if (!m.Success) return;

        draft.DepartDate ??= BuildDate(null, m.Groups[1].Value, m.Groups[2].Value);
        draft.TrainNo ??= m.Groups[3].Value.ToUpperInvariant();
        var d = StripStationSuffix(m.Groups[4].Value);
        var a = StripStationSuffix(m.Groups[5].Value);
        if (IsPlausibleStation(d)) draft.DepartStation ??= d;
        if (IsPlausibleStation(a)) draft.ArriveStation ??= a;
    }

    private static string? TryExtractTrainNo(string text)
    {
        var labeled = MatchGroup(TrainNoLabeledRegex, text, 1);
        if (!string.IsNullOrWhiteSpace(labeled))
            return labeled.ToUpperInvariant();

        var withCi = MatchGroup(TrainNoWithCiRegex, text, 1);
        if (!string.IsNullOrWhiteSpace(withCi))
            return withCi.ToUpperInvariant();

        var bare = MatchGroup(TrainNoBareRegex, text, 1);
        return string.IsNullOrWhiteSpace(bare) ? null : bare.ToUpperInvariant();
    }

    private static void TryExtractStations(string text, TicketImportDraft draft)
    {
        if (!string.IsNullOrWhiteSpace(draft.DepartStation) &&
            !string.IsNullOrWhiteSpace(draft.ArriveStation))
            return;

        // 订单标签优先（截图 OCR 常保留「出发站/到达站」）
        var depLabeled = MatchGroup(StationDepartLabeledRegex, text, 1);
        var arrLabeled = MatchGroup(StationArriveLabeledRegex, text, 1);
        if (TryAssignStationPair(draft, depLabeled, arrLabeled))
            return;

        if (TryAssignStationPairFromMatch(draft, StationQuJianRegex.Match(text)))
            return;
        if (TryAssignStationPairFromMatch(draft, StationFromToRegex.Match(text)))
            return;
        if (TryAssignStationPairFromMatch(draft, StationOriginDestRegex.Match(text)))
            return;

        var dash = StationPairDashRegex.Match(text);
        if (TryAssignStationPairFromMatch(draft, dash))
            return;

        // 铁路客服：「09:00武汉站发车」→ 只补发站
        var faChe = TimeStationFaCheRegex.Match(text);
        if (faChe.Success)
        {
            var d = StripStationSuffix(faChe.Groups[3].Value);
            if (IsPlausibleStation(d))
                draft.DepartStation ??= d;
        }

        // 票面：按出现顺序取前两个「xx站」（跳过非站名）
        var stations = new List<string>();
        foreach (Match m in StationWithZhanRegex.Matches(text))
        {
            var name = StripStationSuffix(m.Groups[1].Value);
            if (!IsPlausibleStation(name)) continue;
            if (stations.Count == 0 || !string.Equals(stations[^1], name, StringComparison.Ordinal))
                stations.Add(name);
            if (stations.Count >= 2) break;
        }

        if (stations.Count >= 2)
        {
            draft.DepartStation ??= stations[0];
            draft.ArriveStation ??= stations[1];
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(depLabeled))
            {
                var d = StripStationSuffix(depLabeled);
                if (IsPlausibleStation(d)) draft.DepartStation ??= d;
            }

            if (!string.IsNullOrWhiteSpace(arrLabeled))
            {
                var a = StripStationSuffix(arrLabeled);
                if (IsPlausibleStation(a)) draft.ArriveStation ??= a;
            }
        }
    }

    private static bool TryAssignStationPairFromMatch(TicketImportDraft draft, Match m)
    {
        if (!m.Success) return false;
        return TryAssignStationPair(draft, m.Groups[1].Value, m.Groups[2].Value);
    }

    private static bool TryAssignStationPair(TicketImportDraft draft, string? depRaw, string? arrRaw)
    {
        if (string.IsNullOrWhiteSpace(depRaw) || string.IsNullOrWhiteSpace(arrRaw))
            return false;
        var d = StripStationSuffix(depRaw);
        var a = StripStationSuffix(arrRaw);
        if (!IsPlausibleStation(d) || !IsPlausibleStation(a))
            return false;
        draft.DepartStation ??= d;
        draft.ArriveStation ??= a;
        return true;
    }

    private static bool IsPlausibleStation(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length < 2) return false;
        if (NonStationNames.Contains(name)) return false;
        if (name.EndsWith("口", StringComparison.Ordinal)) return false;
        return true;
    }

    private static string StripStationSuffix(string name)
    {
        var s = name.Trim();
        if (s.EndsWith("站", StringComparison.Ordinal) && s.Length > 1)
            s = s[..^1];
        return s;
    }

    private static void TryExtractDateTime(string text, TicketImportDraft draft)
    {
        // 优先整行：日期+时间+「开」（票面日期行）
        var paper = PaperDateTimeKaiRegex.Match(text);
        if (paper.Success)
        {
            draft.DepartDate ??= BuildDate(
                paper.Groups[1].Success ? paper.Groups[1].Value : null,
                paper.Groups[2].Value,
                paper.Groups[3].Value);
            draft.DepartTime ??= FormatTime(paper.Groups[4].Value, paper.Groups[5].Value);
            return;
        }

        // 订单：乘车日期标签
        var dateLabeled = DepartDateLabeledRegex.Match(text);
        if (dateLabeled.Success)
        {
            draft.DepartDate ??= BuildDate(
                dateLabeled.Groups[1].Success ? dateLabeled.Groups[1].Value : null,
                dateLabeled.Groups[2].Value,
                dateLabeled.Groups[3].Value);
        }

        var timeLabeled = DepartTimeLabeledRegex.Match(text);
        if (timeLabeled.Success)
            draft.DepartTime ??= FormatTime(timeLabeled.Groups[1].Value, timeLabeled.Groups[2].Value);

        // 智行等：08:15-12:48
        var range = TimeRangeRegex.Match(text);
        if (range.Success)
        {
            draft.DepartTime ??= FormatTime(range.Groups[1].Value, range.Groups[2].Value);
            draft.ArriveTime ??= FormatTime(range.Groups[3].Value, range.Groups[4].Value);
        }

        // 铁路客服：09:00武汉站发车
        var faChe = TimeStationFaCheRegex.Match(text);
        if (faChe.Success)
            draft.DepartTime ??= FormatTime(faChe.Groups[1].Value, faChe.Groups[2].Value);

        if (!string.IsNullOrWhiteSpace(draft.DepartDate) && !string.IsNullOrWhiteSpace(draft.DepartTime))
            return;

        // 订单常见：2026年3月15日 08:00（无「开」）
        var order = OrderDateTimeRegex.Match(text);
        if (order.Success)
        {
            draft.DepartDate ??= BuildDate(
                order.Groups[1].Success ? order.Groups[1].Value : null,
                order.Groups[2].Value,
                order.Groups[3].Value);
            draft.DepartTime ??= FormatTime(order.Groups[4].Value, order.Groups[5].Value);
            if (!string.IsNullOrWhiteSpace(draft.DepartDate) && !string.IsNullOrWhiteSpace(draft.DepartTime))
                return;
        }

        var iso = IsoDateTimeRegex.Match(text);
        if (iso.Success)
        {
            draft.DepartDate ??= BuildDate(iso.Groups[1].Value, iso.Groups[2].Value, iso.Groups[3].Value);
            draft.DepartTime ??= FormatTime(iso.Groups[4].Value, iso.Groups[5].Value);
            if (!string.IsNullOrWhiteSpace(draft.DepartDate) && !string.IsNullOrWhiteSpace(draft.DepartTime))
                return;
        }

        draft.DepartDate ??= TryExtractDate(text);
        draft.DepartTime ??= TryExtractDepartTime(text);
    }

    private static void TryExtractArriveTime(string text, TicketImportDraft draft)
    {
        if (!string.IsNullOrWhiteSpace(draft.ArriveTime))
            return;

        var labeled = ArriveTimeLabeledRegex.Match(text);
        if (labeled.Success)
        {
            draft.ArriveTime = FormatTime(labeled.Groups[1].Value, labeled.Groups[2].Value);
            return;
        }

        var range = TimeRangeRegex.Match(text);
        if (range.Success)
        {
            draft.ArriveTime = FormatTime(range.Groups[3].Value, range.Groups[4].Value);
            return;
        }

        var dao = TimeDaoRegex.Match(text);
        if (dao.Success)
            draft.ArriveTime = FormatTime(dao.Groups[1].Value, dao.Groups[2].Value);
    }

    private static string? TryExtractDate(string text)
    {
        var m = DateYmdRegex.Match(text);
        if (m.Success)
        {
            return BuildDate(
                m.Groups[1].Success ? m.Groups[1].Value : null,
                m.Groups[2].Value,
                m.Groups[3].Value);
        }

        var iso = DateIsoRegex.Match(text);
        if (!iso.Success) return null;
        return BuildDate(iso.Groups[1].Value, iso.Groups[2].Value, iso.Groups[3].Value);
    }

    private static string? BuildDate(string? yearText, string monthText, string dayText)
    {
        var year = yearText != null
            ? int.Parse(yearText, CultureInfo.InvariantCulture)
            : DateTime.Now.Year;
        var month = int.Parse(monthText, CultureInfo.InvariantCulture);
        var day = int.Parse(dayText, CultureInfo.InvariantCulture);
        try
        {
            var dt = new DateTime(year, month, day);
            if (yearText == null && dt < DateTime.Today.AddDays(-30))
                dt = dt.AddYears(1);
            return dt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }
        catch
        {
            return null;
        }
    }

    private static string? TryExtractDepartTime(string text)
    {
        var kai = TimeKaiRegex.Match(text);
        if (kai.Success)
            return FormatTime(kai.Groups[1].Value, kai.Groups[2].Value);

        // 票面时间旁常紧跟「开」；若无「开」则取日期后的第一个时刻，避免误吃别的时间
        var dateMatch = DateYmdRegex.Match(text);
        if (!dateMatch.Success)
            dateMatch = DateIsoRegex.Match(text);
        var searchFrom = dateMatch.Success ? dateMatch.Index + dateMatch.Length : 0;
        var slice = searchFrom < text.Length ? text[searchFrom..] : text;
        var m = TimeBareRegex.Match(slice);
        return m.Success ? FormatTime(m.Groups[1].Value, m.Groups[2].Value) : null;
    }

    private static string FormatTime(string h, string min)
    {
        var hour = int.Parse(h, CultureInfo.InvariantCulture);
        var minute = int.Parse(min, CultureInfo.InvariantCulture);
        return $"{hour:D2}:{minute:D2}";
    }

    private static void TryExtractCoachSeat(string text, TicketImportDraft draft)
    {
        // 1) 标准票面 XX车XXX号 / XX车12A（订单「座位 05车12A号」同样适用）
        var paper = PaperCoachSeatRegex.Match(text);
        if (paper.Success)
        {
            draft.IsJiaChe = paper.Groups["jia"].Success;
            draft.CoachNo = paper.Groups["coach"].Value;

            if (paper.Groups["noseat"].Success)
            {
                draft.IsNoSeat = true;
                draft.SeatNo = null;
            }
            else if (paper.Groups["seatletter"].Success)
            {
                draft.SeatNo = paper.Groups["seatletter"].Value.ToUpperInvariant();
            }
            else if (paper.Groups["seatnum"].Success)
            {
                draft.SeatNo = ComposeSeatNo(paper.Groups["seatnum"].Value, paper.Groups["berth"]);
            }

            if (draft.IsNoSeat || StandaloneNoSeatRegex.IsMatch(text))
            {
                if (StandaloneNoSeatRegex.IsMatch(text)) draft.IsNoSeat = true;
                if (draft.IsNoSeat) draft.SeatNo = null;
            }

            return;
        }

        // 2) 铁路客服旧短信：「编号1F，12号车」
        var classic = ClassicSeatThenCoachRegex.Match(text);
        if (classic.Success)
        {
            draft.CoachNo = classic.Groups["coach"].Value;
            draft.SeatNo = classic.Groups["seat"].Value.ToUpperInvariant();
            return;
        }

        var haoChe = ClassicCoachHaoCheRegex.Match(text);
        if (haoChe.Success)
            draft.CoachNo = haoChe.Groups["coach"].Value;

        // 3) OCR 缺车厢数字：「开车104号」/「车104号」
        var ocrSeat = OcrCheSeatOnlyRegex.Match(text);
        if (ocrSeat.Success)
            draft.SeatNo ??= ComposeSeatNo(ocrSeat.Groups["seatnum"].Value, ocrSeat.Groups["berth"]);

        if (StandaloneNoSeatRegex.IsMatch(text))
        {
            draft.IsNoSeat = true;
            draft.SeatNo = null;
        }
    }

    private static string ComposeSeatNo(string num, Group berthGroup)
    {
        if (!berthGroup.Success || string.IsNullOrWhiteSpace(berthGroup.Value))
            return num;
        var berth = berthGroup.Value;
        if (berth.Length == 1 && char.IsLetter(berth[0]))
            return num + char.ToUpperInvariant(berth[0]);
        return num + berth;
    }

    private static void TryExtractSeatType(string text, TicketImportDraft draft)
    {
        var labeled = MatchGroup(SeatTypeLabeledRegex, text, 1);
        if (!string.IsNullOrWhiteSpace(labeled))
        {
            // 「席别 二等座」后可能粘到其它字，截到已知席别最长前缀
            foreach (var key in SeatTypeProbeOrder)
            {
                if (!labeled.StartsWith(key, StringComparison.Ordinal)) continue;
                ApplySeatType(draft, key);
                return;
            }
        }

        foreach (var key in SeatTypeProbeOrder)
        {
            if (!text.Contains(key, StringComparison.Ordinal)) continue;
            ApplySeatType(draft, key);
            return;
        }
    }

    private static void ApplySeatType(TicketImportDraft draft, string key)
    {
        var mapped = MapSeatTypeToFormOption(key);
        if (mapped != null) draft.SeatType = mapped;
        else draft.UnmappedSeatTypeRaw = key;
    }

    private static string? TryExtractMoney(string text)
    {
        var ticket = MoneyTicketPriceRegex.Match(text);
        if (ticket.Success)
            return ticket.Groups[1].Value.Trim();

        var yen = MoneyYenRegex.Match(text);
        if (yen.Success)
            return yen.Groups[1].Value.Trim();

        // 订单页常有多项「xx元」，无「票价/￥」时宁可不填，避免吃到服务费
        if (text.Contains("订单", StringComparison.Ordinal) ||
            text.Contains("保险", StringComparison.Ordinal) ||
            text.Contains("服务费", StringComparison.Ordinal))
            return null;

        var loose = MoneyYuanLooseRegex.Match(text);
        return loose.Success ? loose.Groups[1].Value.Trim() : null;
    }

    private static string? MatchGroup(Regex regex, string text, int group)
    {
        var m = regex.Match(text);
        return m.Success ? m.Groups[group].Value.Trim() : null;
    }

    private static void MarkMissing(TicketImportDraft draft)
    {
        void Need(string label, bool missing)
        {
            if (missing) draft.FieldsNeedingReview.Add(label);
        }

        Need("车次", string.IsNullOrWhiteSpace(draft.TrainNo));
        Need("出发站", string.IsNullOrWhiteSpace(draft.DepartStation));
        Need("到达站", string.IsNullOrWhiteSpace(draft.ArriveStation));
        Need("出发日期", string.IsNullOrWhiteSpace(draft.DepartDate));
        Need("出发时间", string.IsNullOrWhiteSpace(draft.DepartTime));
        Need("车厢", string.IsNullOrWhiteSpace(draft.CoachNo));
        Need("座位", !draft.IsNoSeat && string.IsNullOrWhiteSpace(draft.SeatNo));
        Need("席别", string.IsNullOrWhiteSpace(draft.SeatType));
        Need("票价", string.IsNullOrWhiteSpace(draft.MoneyText));

        if (!string.IsNullOrWhiteSpace(draft.UnmappedSeatTypeRaw) &&
            string.IsNullOrWhiteSpace(draft.SeatType))
            draft.FieldsNeedingReview.Add($"席别原文「{draft.UnmappedSeatTypeRaw}」无法映射");
    }
}
