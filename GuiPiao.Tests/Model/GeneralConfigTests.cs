using GuiPiao.Model;
using Xunit;

namespace GuiPiao.Tests.Model;

/// <summary>
///     常规设置中与主题、外观相关的默认值（与 GeneralConfig 定义保持一致，防止误改破坏体验）
/// </summary>
public class GeneralConfigTests
{
    [Fact]
    public void 新建配置_主题与强调色默认值()
    {
        var c = new GeneralConfig();
        Assert.Equal(ThemeMode.Light, c.ThemeMode);
        Assert.Equal(AccentColor.MicrosoftBlue, c.AccentColor);
        Assert.Equal("#0078D4", c.CustomColor);
        Assert.Equal(FontSizeOption.Medium, c.FontSize);
        Assert.Equal(RowHeightOption.Standard, c.RowHeight);
    }
}
