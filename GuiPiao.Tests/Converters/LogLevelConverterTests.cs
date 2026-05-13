using System.Globalization;
using GuiPiao.Converters;
using GuiPiao.Model;
using Xunit;

namespace GuiPiao.Tests.Converters;

public class LogLevelConverterTests
{
    private readonly LogLevelConverter _sut = new();

    [Theory]
    [InlineData(LogLevel.WARN, "WARN", true)]
    [InlineData(LogLevel.INFO, "WARN", false)]
    public void Convert_参数级别与当前级别比较(LogLevel current, string param, bool expected)
    {
        var r = (bool)_sut.Convert(current, typeof(bool), param, CultureInfo.InvariantCulture)!;
        Assert.Equal(expected, r);
    }

    [Fact]
    public void Convert_非法参数返回false()
    {
        var r = (bool)_sut.Convert(LogLevel.INFO, typeof(bool), 123, CultureInfo.InvariantCulture)!;
        Assert.False(r);
    }

    [Fact]
    public void ConvertBack_勾选时返回对应级别()
    {
        var r = (LogLevel)_sut.ConvertBack(true, typeof(LogLevel), "ERROR", CultureInfo.InvariantCulture)!;
        Assert.Equal(LogLevel.ERROR, r);
    }

    [Fact]
    public void ConvertBack_未勾选时返回INFO()
    {
        var r = (LogLevel)_sut.ConvertBack(false, typeof(LogLevel), "WARN", CultureInfo.InvariantCulture)!;
        Assert.Equal(LogLevel.INFO, r);
    }
}
