using GuiPiao.Model;

using Xunit;



namespace GuiPiao.Tests.Model;



public class StationFormRulesTests

{

    [Theory]

    [InlineData(null, "")]

    [InlineData("", "")]

    [InlineData("天津", "天津")]

    [InlineData("天津站", "天津")]

    [InlineData(" 天津站 ", "天津")]

    public void ToNameBody_去掉末尾站字(string? stored, string expected)

    {

        Assert.Equal(expected, StationFormRules.ToNameBody(stored));

    }



    [Theory]

    [InlineData(null, "")]

    [InlineData("", "")]

    [InlineData("天津", "天津站")]

    [InlineData("天津站", "天津站")]

    [InlineData(" 天津 ", "天津站")]

    public void ToStoredName_补全站字且不重复(string? body, string expected)

    {

        Assert.Equal(expected, StationFormRules.ToStoredName(body));

    }



    [Fact]

    public void GetEmptyRequiredFields_名称与拼音都空()

    {

        var fields = StationFormRules.GetEmptyRequiredFields("  ", "");

        Assert.Equal(new[] { "车站名称", "车站拼音" }, fields);

    }



    [Fact]

    public void GetEmptyRequiredFields_都有值则空列表()

    {

        Assert.Empty(StationFormRules.GetEmptyRequiredFields("天津", "tianjin"));

    }



    [Fact]

    public void NormalizeCode_去首尾空白并大写()

    {

        Assert.Equal("TJP", StationFormRules.NormalizeCode(" tjp "));

    }



    [Fact]

    public void NormalizePinyin_只保留小写字母()

    {

        Assert.Equal("hefei", StationFormRules.NormalizePinyin(" HeFei "));

    }



    [Theory]

    [InlineData("beijing", "北京", "BJI")]

    [InlineData("shanghai", "上海", "SHA")]

    [InlineData("shijiazhuang", "石家庄", "SJZ")]

    [InlineData("hefei", "合肥", "HFE")]

    public void GenerateLocalCodeFromPinyin_按拼音规则生成(string pinyin, string nameBody, string expected)

    {

        Assert.Equal(expected, StationFormRules.GenerateLocalCodeFromPinyin(pinyin, nameBody));

    }



    [Fact]

    public void EnsureUniqueCode_冲突时追加序号()

    {

        var code = StationFormRules.EnsureUniqueCode("HFE", new[] { "HFE", "HF2" });

        Assert.Equal("HF3", code);

    }



    [Theory]
    [InlineData("BJP", true)]
    [InlineData("A", false)]
    [InlineData("ABCDEF", false)]
    [InlineData("BJ1", true)]
    public void IsValidCodeFormat_校验长度与字符(string code, bool expected)
    {
        Assert.Equal(expected, StationFormRules.IsValidCodeFormat(code));
    }

    [Theory]
    [InlineData("大连站", true)]
    [InlineData("大连", false)]
    [InlineData(" 合肥站 ", true)]
    public void HasStationSuffix_识别末尾站字(string? name, bool expected)
    {
        Assert.Equal(expected, StationFormRules.HasStationSuffix(name));
    }

    [Theory]
    [InlineData("大连站", "大连")]
    [InlineData("站", "站")]
    [InlineData(null, "")]
    public void ToInputName_预览与OCR用(string? stored, string expected)
    {
        Assert.Equal(expected, StationFormRules.ToInputName(stored));
    }
}


