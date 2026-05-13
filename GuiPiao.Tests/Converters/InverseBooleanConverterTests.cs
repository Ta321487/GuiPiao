using System.Globalization;
using GuiPiao.Converters;
using Xunit;

namespace GuiPiao.Tests.Converters;

public class InverseBooleanConverterTests
{
    private readonly InverseBooleanConverter _sut = new();

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void Convert_inverts_bool(bool input, bool expected)
    {
        var result = _sut.Convert(input, typeof(bool), null!, CultureInfo.InvariantCulture);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void ConvertBack_inverts_bool(bool input, bool expected)
    {
        var result = _sut.ConvertBack(input, typeof(bool), null!, CultureInfo.InvariantCulture);
        Assert.Equal(expected, result);
    }
}
