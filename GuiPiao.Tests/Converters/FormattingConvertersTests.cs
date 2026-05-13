using System.Globalization;
using GuiPiao.Converters;
using GuiPiao.Model;
using Xunit;

namespace GuiPiao.Tests.Converters;

public class DateFormatConverterTests
{
    private readonly DateFormatConverter _sut = new();

    [Theory]
    [InlineData("2026-05-14", "2026-05-14")]
    [InlineData("2026/05/14", "2026-05-14")]
    [InlineData("14/05/2026", "2026-05-14")]
    public void Convert_常见格式统一为yyyy_MM_dd(string input, string expected)
    {
        var r = (string)_sut.Convert(input, typeof(string), null!, CultureInfo.InvariantCulture)!;
        Assert.Equal(expected, r);
    }

    [Fact]
    public void Convert_空或非字符串原样返回()
    {
        Assert.Equal("", (string)_sut.Convert("", typeof(string), null!, CultureInfo.InvariantCulture)!);
        Assert.Equal(123, _sut.Convert(123, typeof(string), null!, CultureInfo.InvariantCulture));
    }
}

public class EnumToDescriptionConverterTests
{
    private readonly EnumToDescriptionConverter _sut = new();

    [Fact]
    public void Convert_LayoutType_使用字典名称()
    {
        var r = (string)_sut.Convert(LayoutType.TwoColumn, typeof(string), null!, CultureInfo.InvariantCulture)!;
        Assert.Equal("两列等宽", r);
    }

    [Fact]
    public void Convert_ChartType_使用字典名称()
    {
        var r = (string)_sut.Convert(ChartType.PieChart, typeof(string), null!, CultureInfo.InvariantCulture)!;
        Assert.Equal("饼图", r);
    }

    [Fact]
    public void Convert_null_返回空字符串()
    {
        var r = (string)_sut.Convert(null!, typeof(string), null!, CultureInfo.InvariantCulture)!;
        Assert.Equal("", r);
    }
}

public class StatisticTypeToLabelConverterTests
{
    private readonly StatisticTypeToLabelConverter _sut = new();

    [Theory]
    [InlineData(StatisticType.MonthlyTripStats, "时间粒度：")]
    [InlineData(StatisticType.TrainTypeRatio, "分类依据：")]
    [InlineData(StatisticType.StationTopRanking, "统计依据：")]
    [InlineData(StatisticType.AnnualTripSummary, "对比维度：")]
    [InlineData(StatisticType.TripTimeDistribution, "时段划分：")]
    public void Convert_各统计类型前缀(StatisticType type, string expected)
    {
        var r = (string)_sut.Convert(type, typeof(string), null!, CultureInfo.InvariantCulture)!;
        Assert.Equal(expected, r);
    }

    [Fact]
    public void Convert_非枚举返回默认前缀()
    {
        var r = (string)_sut.Convert("x", typeof(string), null!, CultureInfo.InvariantCulture)!;
        Assert.Equal("分类依据：", r);
    }
}
