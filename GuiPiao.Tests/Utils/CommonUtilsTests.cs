using System;
using GuiPiao.Utils;
using Xunit;

namespace GuiPiao.Tests.Utils;

public class CommonUtilsTests
{
    [Fact]
    public void FormatDate_按约定格式()
    {
        var d = new DateTime(2026, 5, 14, 15, 30, 0);
        Assert.Equal("2026-05-14", CommonUtils.FormatDate(d));
    }

    [Fact]
    public void ParseDate_合法字符串可解析()
    {
        var d = CommonUtils.ParseDate("2026-05-14");
        Assert.Equal(2026, d.Year);
        Assert.Equal(5, d.Month);
        Assert.Equal(14, d.Day);
    }

    [Fact]
    public void ParseDate_非法格式返回最小值()
    {
        Assert.Equal(DateTime.MinValue, CommonUtils.ParseDate("not-a-date"));
    }

    [Fact]
    public void CalculateDaysDifference_按日历日差()
    {
        var a = new DateTime(2026, 5, 1);
        var b = new DateTime(2026, 5, 10);
        Assert.Equal(9, CommonUtils.CalculateDaysDifference(a, b));
    }

    [Theory]
    [InlineData("11010519491231002X", true)]
    [InlineData("11010519491231002x", true)]
    [InlineData("110105194912310021", true)]
    [InlineData("11010519491231001", false)]
    [InlineData("", false)]
    public void ValidateIdCard_长度与末位规则(string id, bool expected)
    {
        Assert.Equal(expected, CommonUtils.ValidateIdCard(id));
    }

    [Fact]
    public void HideSensitiveInfo_长度大于显示位时打码()
    {
        Assert.Equal("abcd*******", CommonUtils.HideSensitiveInfo("abcdefghijk", 4));
    }

    [Fact]
    public void HideSensitiveInfo_过短则原样返回()
    {
        Assert.Equal("abc", CommonUtils.HideSensitiveInfo("abc", 4));
    }
}
