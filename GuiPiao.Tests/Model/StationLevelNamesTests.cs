using GuiPiao.Model;
using Xunit;

namespace GuiPiao.Tests.Model;

public class StationLevelNamesTests
{
    [Theory]
    [InlineData(0, StationLevel.Unspecified, "未分级")]
    [InlineData(1, StationLevel.Special, "特等站")]
    [InlineData(2, StationLevel.First, "一等站")]
    [InlineData(4, StationLevel.Second, "二等站")]
    [InlineData(8, StationLevel.Third, "三等站")]
    [InlineData(16, StationLevel.Fourth, "四等站")]
    [InlineData(32, StationLevel.Fifth, "五等站")]
    public void FromStoredValue_已知值映射显示名(int stored, StationLevel expected, string display)
    {
        Assert.Equal(expected, StationLevelNames.FromStoredValue(stored));
        Assert.Equal(display, StationLevelNames.GetDisplayName(stored));
    }

    [Fact]
    public void FromStoredValue_未知值回退未分级()
    {
        Assert.Equal(StationLevel.Unspecified, StationLevelNames.FromStoredValue(3));
        Assert.Equal("未分级", StationLevelNames.GetDisplayName(99));
    }
}
