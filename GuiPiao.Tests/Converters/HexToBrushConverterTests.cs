using System.Globalization;
using System.Windows.Media;
using GuiPiao.Converters;
using Xunit;

namespace GuiPiao.Tests.Converters;

public class HexToBrushConverterTests
{
    private readonly HexToBrushConverter _sut = new();

    [Fact]
    public void Convert_合法十六进制得到对应画刷()
    {
        var brush = (SolidColorBrush)_sut.Convert("#FF112233", typeof(Brush), null!, CultureInfo.InvariantCulture)!;
        Assert.Equal(Color.FromRgb(0x11, 0x22, 0x33), brush.Color);
    }

    [Fact]
    public void Convert_非法字符串回退灰色()
    {
        var brush = (SolidColorBrush)_sut.Convert("not-a-color", typeof(Brush), null!, CultureInfo.InvariantCulture)!;
        Assert.Equal(Colors.Gray, brush.Color);
    }

    [Fact]
    public void ConvertBack_画刷转字符串()
    {
        var c = Color.FromRgb(0xAB, 0xCD, 0xEF);
        var brush = new SolidColorBrush(c);
        var s = (string)_sut.ConvertBack(brush, typeof(string), null!, CultureInfo.InvariantCulture)!;
        Assert.Contains("ABCD", s.Replace("#", "").ToUpperInvariant());
    }
}
