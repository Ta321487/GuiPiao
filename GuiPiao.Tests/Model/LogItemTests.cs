using GuiPiao.Model;
using Xunit;

namespace GuiPiao.Tests.Model;

public class LogItemTests
{
    [Theory]
    [InlineData(LogLevel.ALL, "ALL")]
    [InlineData(LogLevel.INFO, "INFO")]
    [InlineData(LogLevel.WARN, "WARN")]
    [InlineData(LogLevel.ERROR, "ERROR")]
    [InlineData(LogLevel.FATAL, "FATAL")]
    public void LevelDisplay_与级别枚举一致(LogLevel level, string expected)
    {
        var item = new LogItem
        {
            Time = "12:00:00",
            Content = "x",
            Module = "Test",
            Level = level,
            CreatedAt = default
        };
        Assert.Equal(expected, item.LevelDisplay);
    }
}
