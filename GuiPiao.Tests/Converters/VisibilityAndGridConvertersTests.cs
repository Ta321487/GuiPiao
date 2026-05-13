using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using GuiPiao.Converters;
using Xunit;

namespace GuiPiao.Tests.Converters;

public class BoolToStatusConverterTests
{
    private readonly BoolToStatusConverter _sut = new();

    [Theory]
    [InlineData(true, "✓")]
    [InlineData(false, "✗")]
    public void Convert_布尔转符号(bool input, string expected)
    {
        var r = (string)_sut.Convert(input, typeof(string), null!, CultureInfo.InvariantCulture)!;
        Assert.Equal(expected, r);
    }

    [Fact]
    public void Convert_非布尔为叉()
    {
        var r = (string)_sut.Convert("x", typeof(string), null!, CultureInfo.InvariantCulture)!;
        Assert.Equal("✗", r);
    }
}

public class CountToVisibilityConverterTests
{
    private readonly CountToVisibilityConverter _sut = new();

    [Theory]
    [InlineData(0, Visibility.Visible)]
    [InlineData(1, Visibility.Collapsed)]
    public void Convert_零可见非零折叠(int count, Visibility expected)
    {
        var r = (Visibility)_sut.Convert(count, typeof(Visibility), null!, CultureInfo.InvariantCulture)!;
        Assert.Equal(expected, r);
    }

    [Fact]
    public void Convert_非int为折叠()
    {
        var r = (Visibility)_sut.Convert("0", typeof(Visibility), null!, CultureInfo.InvariantCulture)!;
        Assert.Equal(Visibility.Collapsed, r);
    }
}

public class CollectionContainsConverterTests
{
    private readonly CollectionContainsConverter _sut = new();

    [Fact]
    public void Convert_集合包含参数字符串时true()
    {
        var list = new List<string> { "a", "b", "c" };
        var r = (bool)_sut.Convert(list, typeof(bool), "b", CultureInfo.InvariantCulture)!;
        Assert.True(r);
    }

    [Fact]
    public void Convert_不包含时false()
    {
        var r = (bool)_sut.Convert(new[] { 1, 2 }, typeof(bool), "3", CultureInfo.InvariantCulture)!;
        Assert.False(r);
    }
}

public class BoolToGridLengthConverterTests
{
    private readonly BoolToGridLengthConverter _sut = new();

    [Fact]
    public void Convert_true为Auto_false为0()
    {
        Assert.Equal(GridLength.Auto, _sut.Convert(true, typeof(GridLength), null!, CultureInfo.InvariantCulture));
        var hidden = (GridLength)_sut.Convert(false, typeof(GridLength), null!, CultureInfo.InvariantCulture)!;
        Assert.Equal(0, hidden.Value);
    }

    [Theory]
    [InlineData(1.0, true)]
    [InlineData(0.0, false)]
    public void ConvertBack_GridLength反映是否可见(double value, bool expected)
    {
        var gl = new GridLength(value);
        var r = (bool)_sut.ConvertBack(gl, typeof(bool), null!, CultureInfo.InvariantCulture)!;
        Assert.Equal(expected, r);
    }
}

public class InverseBooleanToVisibilityConverterTests
{
    private readonly InverseBooleanToVisibilityConverter _sut = new();

    [Theory]
    [InlineData(true, Visibility.Collapsed)]
    [InlineData(false, Visibility.Visible)]
    public void Convert_与布尔可见性相反(bool b, Visibility v)
    {
        Assert.Equal(v, _sut.Convert(b, typeof(Visibility), null!, CultureInfo.InvariantCulture));
    }

    [Fact]
    public void ConvertBack_Visible对应false()
    {
        var r = (bool)_sut.ConvertBack(Visibility.Visible, typeof(bool), null!, CultureInfo.InvariantCulture)!;
        Assert.False(r);
    }
}

public class StringNotEmptyToVisibilityConverterTests
{
    private readonly StringNotEmptyToVisibilityConverter _sut = new();

    [Theory]
    [InlineData("", Visibility.Collapsed)]
    [InlineData("x", Visibility.Visible)]
    public void Convert_空折叠非空可见(string s, Visibility v)
    {
        Assert.Equal(v, _sut.Convert(s, typeof(Visibility), null!, CultureInfo.InvariantCulture));
    }
}

public class BooleanToOpacityConverterTests
{
    private readonly BooleanToOpacityConverter _sut = new();

    [Theory]
    [InlineData(true, 0.5)]
    [InlineData(false, 1.0)]
    public void Convert_布尔对应透明度(bool b, double opacity)
    {
        var r = (double)_sut.Convert(b, typeof(double), null!, CultureInfo.InvariantCulture)!;
        Assert.Equal(opacity, r);
    }

    [Fact]
    public void Convert_非布尔为1()
    {
        var r = (double)_sut.Convert("x", typeof(double), null!, CultureInfo.InvariantCulture)!;
        Assert.Equal(1.0, r);
    }
}

public class IntComparisonToVisibilityConverterTests
{
    private readonly IntComparisonToVisibilityConverter _sut = new();
    private readonly InverseIntComparisonToVisibilityConverter _inverse = new();

    [Theory]
    [InlineData(3, "3", Visibility.Visible)]
    [InlineData(2, "3", Visibility.Collapsed)]
    public void Convert_等于参数则可见(int value, string param, Visibility expected)
    {
        Assert.Equal(expected, _sut.Convert(value, typeof(Visibility), param, CultureInfo.InvariantCulture));
    }

    [Theory]
    [InlineData(3, "3", Visibility.Collapsed)]
    [InlineData(2, "3", Visibility.Visible)]
    public void InverseConvert_等于参数则折叠(int value, string param, Visibility expected)
    {
        Assert.Equal(expected, _inverse.Convert(value, typeof(Visibility), param, CultureInfo.InvariantCulture));
    }

    [Fact]
    public void InverseConvert_非法参数为Visible()
    {
        var r = (Visibility)_inverse.Convert(1, typeof(Visibility), "bad", CultureInfo.InvariantCulture)!;
        Assert.Equal(Visibility.Visible, r);
    }
}
