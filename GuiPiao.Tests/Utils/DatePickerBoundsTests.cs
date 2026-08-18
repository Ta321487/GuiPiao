using System;
using GuiPiao.Utils;
using Xunit;

namespace GuiPiao.Tests.Utils;

public class DatePickerBoundsTests
{
    private static readonly DateTime Start = new(1990, 1, 1);
    private static readonly DateTime End = new(2028, 8, 18);

    [Fact]
    public void IsSelectable_范围内可选()
    {
        Assert.True(DatePickerBounds.IsSelectable(new DateTime(2026, 8, 18), Start, End));
        Assert.True(DatePickerBounds.IsSelectable(Start, Start, End));
        Assert.True(DatePickerBounds.IsSelectable(End, Start, End));
        Assert.False(DatePickerBounds.IsSelectable(new DateTime(1989, 12, 31), Start, End));
        Assert.False(DatePickerBounds.IsSelectable(new DateTime(2028, 8, 19), Start, End));
    }

    [Fact]
    public void 翻月在边界停住()
    {
        Assert.False(DatePickerBounds.CanGoPreviousMonth(Start, Start));
        Assert.True(DatePickerBounds.CanGoPreviousMonth(new DateTime(1990, 2, 1), Start));
        Assert.False(DatePickerBounds.CanGoNextMonth(new DateTime(2028, 8, 1), End));
        Assert.True(DatePickerBounds.CanGoNextMonth(new DateTime(2028, 7, 1), End));
    }

    [Fact]
    public void 年份页不超出范围()
    {
        Assert.Equal(1990, DatePickerBounds.ClampYearRangeStart(1980, Start, End));
        Assert.Equal(2017, DatePickerBounds.ClampYearRangeStart(2030, Start, End));
        Assert.False(DatePickerBounds.CanGoPreviousYearRange(1990, Start));
        Assert.True(DatePickerBounds.CanGoPreviousYearRange(2002, Start));
        Assert.False(DatePickerBounds.CanGoNextYearRange(2017, End));
        Assert.True(DatePickerBounds.CanGoNextYearRange(2016, End));
    }
}
