using System;

namespace GuiPiao.Utils;

/// <summary>
///     到达时间展示（含跨天：+1 / +2）
/// </summary>
public static class ArriveTimeFormat
{
    public const int MaxDayOffset = 2;

    /// <summary>表单下拉文案（编辑时更直观）。</summary>
    public static readonly string[] DayOffsetLabels = { "当日", "次日", "第三天" };

    /// <summary>列表/导出等纯文本：04:52 / 04:52(+1) / 04:52(+2)。</summary>
    public static string Format(string? arriveTime, int dayOffset)
    {
        if (string.IsNullOrWhiteSpace(arriveTime))
            return string.Empty;

        var badge = FormatBadge(dayOffset);
        return string.IsNullOrEmpty(badge) ? arriveTime : $"{arriveTime}({badge})";
    }

    /// <summary>卡片右上角角标：空 / +1 / +2。</summary>
    public static string FormatBadge(int dayOffset)
    {
        var offset = NormalizeOffset(dayOffset);
        return offset > 0 ? $"+{offset}" : string.Empty;
    }

    public static int NormalizeOffset(int dayOffset)
    {
        if (dayOffset < 0) return 0;
        if (dayOffset > MaxDayOffset) return MaxDayOffset;
        return dayOffset;
    }

    public static string ToLabel(int dayOffset) =>
        DayOffsetLabels[NormalizeOffset(dayOffset)];

    public static int FromLabel(string? label)
    {
        if (string.IsNullOrWhiteSpace(label))
            return 0;
        for (var i = 0; i < DayOffsetLabels.Length; i++)
            if (string.Equals(DayOffsetLabels[i], label, StringComparison.Ordinal))
                return i;
        return 0;
    }
}
