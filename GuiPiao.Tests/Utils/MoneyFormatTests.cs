using GuiPiao.Utils;
using Xunit;

namespace GuiPiao.Tests.Utils;

public class MoneyFormatTests
{
    [Fact]
    public void Display_前缀全角人民币符号()
    {
        Assert.Equal("￥553.00", MoneyFormat.Display(553m));
        Assert.Equal("￥0.50", MoneyFormat.Display(0.5m));
    }

    [Theory]
    [InlineData("¥553.00", 553)]
    [InlineData("￥553.5", 553.5)]
    [InlineData("1,234.50", 1234.50)]
    public void TryParse_去掉符号与千分位(string raw, double expected)
    {
        Assert.True(MoneyFormat.TryParse(raw, out var money));
        Assert.Equal((decimal)expected, money);
    }

    [Fact]
    public void FormatStatisticValue_花费类加符号()
    {
        Assert.Equal("￥10.00", MoneyFormat.FormatStatisticValue("总花费", 10));
        Assert.Equal("￥10.00", MoneyFormat.FormatStatisticValue("出行花费占比", 10));
        Assert.Equal("￥10.00", MoneyFormat.FormatStatisticValue("平均花费", 10));
        Assert.Equal("3", MoneyFormat.FormatStatisticValue("出行次数", 3));
        Assert.Equal("3", MoneyFormat.FormatStatisticValue("车票数量占比", 3));
    }
}
