using System;
using GuiPiao.Utils;
using Xunit;

namespace GuiPiao.Tests.Utils;

public class RideDateTimeTests
{
    [Theory]
    [InlineData("8:30", "08:30")]
    [InlineData("08:30", "08:30")]
    [InlineData("08:30:00", "08:30")]
    [InlineData("00:10", "00:10")]
    [InlineData("23:59", "23:59")]
    [InlineData("14:05(+1)", "14:05")]
    [InlineData("", "")]
    public void NormalizeTime_统一为HH_mm(string input, string expected)
    {
        Assert.Equal(expected, RideDateTime.NormalizeTime(input));
    }

    [Theory]
    [InlineData("2026-8-12", "2026-08-12")]
    [InlineData("2026/08/12", "2026-08-12")]
    [InlineData("2026-08-12", "2026-08-12")]
    public void NormalizeDate_统一为yyyy_MM_dd(string input, string expected)
    {
        Assert.Equal(expected, RideDateTime.NormalizeDate(input));
    }

    [Fact]
    public void TryParseTimeAsDateTime_供表单绑定()
    {
        Assert.True(RideDateTime.TryParseTimeAsDateTime("9:05", out var dt));
        Assert.Equal(9, dt.Hour);
        Assert.Equal(5, dt.Minute);
        Assert.Equal("09:05", RideDateTime.FormatTime(dt));
    }

    [Fact]
    public void FormatTime_小时分钟钳位()
    {
        Assert.Equal("23:59", RideDateTime.FormatTime(99, 99));
        Assert.Equal("00:00", RideDateTime.FormatTime(0, 0));
    }
}
