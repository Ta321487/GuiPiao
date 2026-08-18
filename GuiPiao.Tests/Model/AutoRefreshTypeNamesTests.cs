using GuiPiao.Model;
using Xunit;

namespace GuiPiao.Tests.Model;

public class AutoRefreshTypeNamesTests
{
    [Fact]
    public void Normalize_Weekly当作关闭()
    {
        Assert.Equal(AutoRefreshType.Off, AutoRefreshTypeNames.Normalize(AutoRefreshType.Weekly));
        Assert.Equal(AutoRefreshType.Off, AutoRefreshTypeNames.Normalize(AutoRefreshType.Off));
        Assert.Equal(AutoRefreshType.OnStartup, AutoRefreshTypeNames.Normalize(AutoRefreshType.OnStartup));
    }

    [Fact]
    public void Names_不含周日定时()
    {
        Assert.False(AutoRefreshTypeNames.Names.ContainsKey(AutoRefreshType.Weekly));
        Assert.Equal("关闭", AutoRefreshTypeNames.Names[AutoRefreshType.Off]);
        Assert.Equal("加载仪表盘时刷新", AutoRefreshTypeNames.Names[AutoRefreshType.OnStartup]);
    }
}
