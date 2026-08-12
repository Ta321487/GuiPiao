using System;
using System.Globalization;

namespace GuiPiao.Utils;

/// <summary>
///     行程日期/时间的唯一规范：
///     日期 = yyyy-MM-dd；时间 = HH:mm（24 小时、两位补零、无秒）。
/// </summary>
public static class RideDateTime
{
    public const string DateFormat = "yyyy-MM-dd";
    public const string TimeFormat = "HH:mm";

    private static readonly string[] DateFormats =
    {
        "yyyy-MM-dd",
        "yyyy/MM/dd",
        "yyyy-M-d",
        "yyyy/M/d",
        "dd/MM/yyyy",
        "dd-MM-yyyy",
        "d/M/yyyy",
        "d-M-yyyy",
        "MM/dd/yyyy",
        "M/d/yyyy",
        "yyyy.MM.dd",
        "yyyy.M.d",
        "yyyy年MM月dd日",
        "yyyy年M月d日"
    };

    private static readonly string[] TimeFormats =
    {
        "HH:mm",
        "H:mm",
        "HH:mm:ss",
        "H:mm:ss",
        "HH:m",
        "H:m"
    };

    private static readonly string[] DateTimeFormats =
    {
        "yyyy-MM-dd HH:mm",
        "yyyy-MM-dd HH:mm:ss",
        "yyyy/MM/dd HH:mm",
        "yyyy/MM/dd HH:mm:ss"
    };

    public static string FormatDate(DateTime date) =>
        date.ToString(DateFormat, CultureInfo.InvariantCulture);

    public static string FormatTime(DateTime dateTime) =>
        dateTime.ToString(TimeFormat, CultureInfo.InvariantCulture);

    public static string FormatTime(TimeSpan time) =>
        $"{time.Hours:D2}:{time.Minutes:D2}";

    public static string FormatTime(int hour, int minute) =>
        $"{Math.Clamp(hour, 0, 23):D2}:{Math.Clamp(minute, 0, 59):D2}";

    /// <summary>归一化为 yyyy-MM-dd；无法解析时返回去空白后的原串。</summary>
    public static string NormalizeDate(string? dateStr)
    {
        if (string.IsNullOrWhiteSpace(dateStr))
            return string.Empty;

        return TryParseDate(dateStr, out var date) ? FormatDate(date) : dateStr.Trim();
    }

    /// <summary>归一化为 HH:mm；空串保持空；无法解析时返回去空白后的原串。</summary>
    public static string NormalizeTime(string? timeStr)
    {
        if (string.IsNullOrWhiteSpace(timeStr))
            return string.Empty;

        return TryParseTime(timeStr, out var time) ? FormatTime(time) : timeStr.Trim();
    }

    public static bool TryParseDate(string? dateStr, out DateTime date)
    {
        date = default;
        if (string.IsNullOrWhiteSpace(dateStr))
            return false;

        var s = dateStr.Trim();
        if (DateTime.TryParseExact(s, DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
            return true;

        return DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out date) ||
               DateTime.TryParse(s, CultureInfo.CurrentCulture, DateTimeStyles.None, out date);
    }

    public static bool TryParseTime(string? timeStr, out TimeSpan time)
    {
        time = default;
        if (string.IsNullOrWhiteSpace(timeStr))
            return false;

        var s = timeStr.Trim();

        // 去掉跨天展示后缀：04:52(+1) / 次日 04:52
        var paren = s.IndexOf('(', StringComparison.Ordinal);
        if (paren > 0)
            s = s[..paren].Trim();
        if (s.StartsWith("次日", StringComparison.Ordinal) || s.StartsWith("第三天", StringComparison.Ordinal))
        {
            var space = s.LastIndexOf(' ');
            if (space >= 0 && space < s.Length - 1)
                s = s[(space + 1)..].Trim();
        }

        if (TimeSpan.TryParseExact(s, new[] { @"hh\:mm", @"h\:mm", @"hh\:mm\:ss", @"h\:mm\:ss" },
                CultureInfo.InvariantCulture, out time))
        {
            time = new TimeSpan(time.Hours, time.Minutes, 0);
            return true;
        }

        if (DateTime.TryParseExact(s, TimeFormats, CultureInfo.InvariantCulture, DateTimeStyles.NoCurrentDateDefault,
                out var asTime))
        {
            time = new TimeSpan(asTime.Hour, asTime.Minute, 0);
            return true;
        }

        if (DateTime.TryParseExact(s, DateTimeFormats, CultureInfo.InvariantCulture, DateTimeStyles.None,
                out var asDateTime))
        {
            time = new TimeSpan(asDateTime.Hour, asDateTime.Minute, 0);
            return true;
        }

        if (DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out asDateTime) ||
            DateTime.TryParse(s, CultureInfo.CurrentCulture, DateTimeStyles.None, out asDateTime))
        {
            time = new TimeSpan(asDateTime.Hour, asDateTime.Minute, 0);
            return true;
        }

        // 末段像时间： "2026-08-12 8:30"
        var lastSpace = s.LastIndexOf(' ');
        if (lastSpace > 0 && lastSpace < s.Length - 1)
            return TryParseTime(s[(lastSpace + 1)..], out time);

        return false;
    }

    /// <summary>解析为带虚拟日期的 DateTime，供表单 TimePicker 绑定。</summary>
    public static bool TryParseTimeAsDateTime(string? timeStr, out DateTime dateTime)
    {
        dateTime = default;
        if (!TryParseTime(timeStr, out var time))
            return false;

        dateTime = new DateTime(2000, 1, 1, time.Hours, time.Minutes, 0);
        return true;
    }
}
