using System;

namespace GuiPiao.Utils;

/// <summary>
///     日历可选范围：火车票可回溯到电脑票普及前后，未来按行程预录留两年。
/// </summary>
public static class DatePickerBounds
{
    public static readonly DateTime Start = new(1990, 1, 1);

    public static DateTime End => DateTime.Today.AddYears(2);

    public static bool IsSelectable(DateTime date, DateTime start, DateTime end)
    {
        var d = date.Date;
        return d >= start.Date && d <= end.Date;
    }

    public static bool CanGoPreviousMonth(DateTime display, DateTime start) =>
        new DateTime(display.Year, display.Month, 1) > new DateTime(start.Year, start.Month, 1);

    public static bool CanGoNextMonth(DateTime display, DateTime end) =>
        new DateTime(display.Year, display.Month, 1) < new DateTime(end.Year, end.Month, 1);

    public static bool CanGoPreviousYear(int displayYear, DateTime start) => displayYear > start.Year;

    public static bool CanGoNextYear(int displayYear, DateTime end) => displayYear < end.Year;

    public static bool CanGoPreviousYearRange(int rangeStart, DateTime start) => rangeStart > start.Year;

    public static bool CanGoNextYearRange(int rangeStart, DateTime end) => rangeStart + 11 < end.Year;

    public static int ClampYearRangeStart(int rangeStart, DateTime start, DateTime end)
    {
        var minStart = start.Year;
        var maxStart = Math.Max(minStart, end.Year - 11);
        if (rangeStart < minStart) return minStart;
        if (rangeStart > maxStart) return maxStart;
        return rangeStart;
    }

    public static bool MonthOverlapsRange(int year, int month, DateTime start, DateTime end)
    {
        var monthStart = new DateTime(year, month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);
        return monthEnd.Date >= start.Date && monthStart.Date <= end.Date;
    }
}
