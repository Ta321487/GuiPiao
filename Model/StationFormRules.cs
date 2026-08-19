using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GuiPiao.Model;

/// <summary>
///     车站表单规则：站名/拼音必填且站名本库唯一；电报码选填，留空则按拼音规则生成本机码。
/// </summary>
public static class StationFormRules
{
    private static readonly HashSet<string> PinyinSyllables = BuildPinyinSyllables();

    public static string RemoveStationSuffix(string? stationName)
    {
        if (string.IsNullOrEmpty(stationName))
            return string.Empty;

        return stationName.EndsWith("站", StringComparison.Ordinal)
            ? stationName[..^1]
            : stationName;
    }

    public static string AddStationSuffix(string? stationName)
    {
        if (string.IsNullOrEmpty(stationName))
            return string.Empty;

        return stationName.EndsWith("站", StringComparison.Ordinal)
            ? stationName
            : $"{stationName}站";
    }

    public static bool HasStationSuffix(string? stationName) =>
        !string.IsNullOrWhiteSpace(stationName) &&
        stationName.TrimEnd().EndsWith("站", StringComparison.Ordinal);

    /// <summary>入库/查询用完整站名（补「站」）。</summary>
    public static string ToStoredName(string? nameBody)
    {
        var body = (nameBody ?? string.Empty).Trim();
        return AddStationSuffix(body);
    }

    /// <summary>去掉末尾「站」；空或仅空白返回空。</summary>
    public static string ToNameBody(string? storedName) => RemoveStationSuffix(storedName?.Trim());

    /// <summary>输入框/OCR/预览：去「站」；单字「站」保留。</summary>
    public static string ToInputName(string? storedName)
    {
        var t = (storedName ?? string.Empty).Trim();
        if (t.Length <= 1)
            return t;
        return RemoveStationSuffix(t);
    }

    public static string NormalizeCode(string? code) => (code ?? string.Empty).Trim().ToUpperInvariant();

    public static string NormalizePinyin(string? pinyin)
    {
        if (string.IsNullOrWhiteSpace(pinyin))
            return string.Empty;

        var sb = new StringBuilder(pinyin.Length);
        foreach (var ch in pinyin.Trim().ToLowerInvariant())
        {
            if (ch is >= 'a' and <= 'z')
                sb.Append(ch);
        }

        return sb.ToString();
    }

    public static List<string> GetEmptyRequiredFields(string? nameBody, string? pinyin)
    {
        var empty = new List<string>();
        if (string.IsNullOrWhiteSpace(nameBody))
            empty.Add("车站名称");
        if (string.IsNullOrWhiteSpace(NormalizePinyin(pinyin)))
            empty.Add("车站拼音");
        return empty;
    }

    /// <summary>
    ///     按拼音码常见规则（正取二、倒取一、不足补齐）生成本机三字母码；非官方电报码。
    /// </summary>
    public static string GenerateLocalCodeFromPinyin(string? pinyin, string? nameBody = null)
    {
        var normalized = NormalizePinyin(pinyin);
        if (string.IsNullOrEmpty(normalized))
            return "GP1";

        var syllables = SplitPinyinSyllables(normalized, ToNameBody(nameBody).Length);
        if (syllables.Count == 0)
            return PadOrTrimCode(normalized);

        var code = syllables.Count switch
        {
            >= 3 => $"{FirstUpper(syllables[0])}{FirstUpper(syllables[1])}{FirstUpper(syllables[^1])}",
            2 => $"{FirstUpper(syllables[0])}{FirstUpper(syllables[1])}{NthUpper(syllables[1], 1)}",
            _ => $"{FirstUpper(syllables[0])}{NthUpper(syllables[0], 1)}{NthUpper(syllables[0], 2)}"
        };

        return PadOrTrimCode(code);
    }

    public static string EnsureUniqueCode(string baseCode, IEnumerable<string> existingCodes)
    {
        var normalizedBase = PadOrTrimCode(NormalizeCode(baseCode));
        var taken = new HashSet<string>(existingCodes.Select(NormalizeCode), StringComparer.OrdinalIgnoreCase);
        if (!taken.Contains(normalizedBase))
            return normalizedBase;

        for (var i = 2; i < 1000; i++)
        {
            var candidate = i < 10
                ? PadOrTrimCode(normalizedBase[..Math.Min(2, normalizedBase.Length)] + i)
                : PadOrTrimCode(normalizedBase[..Math.Min(1, normalizedBase.Length)] + i);
            if (!taken.Contains(candidate))
                return candidate;
        }

        return normalizedBase + "X";
    }

    public static bool IsValidCodeFormat(string? code)
    {
        var normalized = NormalizeCode(code);
        if (normalized.Length is < 2 or > 5)
            return false;

        return normalized.All(ch => ch is >= 'A' and <= 'Z' or >= '0' and <= '9');
    }

    public static IReadOnlyList<string> SplitPinyinSyllables(string normalizedPinyin, int expectedCount = 0)
    {
        if (string.IsNullOrEmpty(normalizedPinyin))
            return Array.Empty<string>();

        var all = SplitAllSyllables(normalizedPinyin);
        if (expectedCount <= 0 || all.Count == expectedCount)
            return all;

        if (expectedCount > 0 && normalizedPinyin.Length >= expectedCount)
        {
            var proportional = SplitByLength(normalizedPinyin, expectedCount);
            if (proportional.Count == expectedCount)
                return proportional;
        }

        return all;
    }

    private static List<string> SplitAllSyllables(string pinyin)
    {
        var result = new List<string>();
        var i = 0;
        while (i < pinyin.Length)
        {
            var matched = false;
            for (var len = Math.Min(6, pinyin.Length - i); len >= 1; len--)
            {
                var part = pinyin.Substring(i, len);
                if (!PinyinSyllables.Contains(part))
                    continue;

                result.Add(part);
                i += len;
                matched = true;
                break;
            }

            if (!matched)
            {
                result.Add(pinyin[i].ToString());
                i++;
            }
        }

        return result;
    }

    private static List<string> SplitByLength(string pinyin, int parts)
    {
        var result = new List<string>(parts);
        var baseLen = pinyin.Length / parts;
        var remainder = pinyin.Length % parts;
        var index = 0;
        for (var part = 0; part < parts; part++)
        {
            var len = baseLen + (part < remainder ? 1 : 0);
            if (len <= 0)
                break;
            result.Add(pinyin.Substring(index, len));
            index += len;
        }

        return result;
    }

    private static char FirstUpper(string syllable) =>
        syllable.Length > 0 ? char.ToUpperInvariant(syllable[0]) : 'X';

    private static char NthUpper(string syllable, int index) =>
        index < syllable.Length ? char.ToUpperInvariant(syllable[index]) : 'X';

    private static string PadOrTrimCode(string code)
    {
        var letters = new string(code.Where(ch => ch is >= 'A' and <= 'Z' or >= 'a' and <= 'z' or >= '0' and <= '9')
            .ToArray()).ToUpperInvariant();
        if (letters.Length >= 3)
            return letters[..3];
        if (letters.Length == 2)
            return letters + "X";
        if (letters.Length == 1)
            return letters + "XX";
        return "GPX";
    }

    private static HashSet<string> BuildPinyinSyllables()
    {
        var raw = """
            a ai an ang ao ba bai ban bang bao bei ben beng bi bian biao bie bin bing bo bu
            ca cai can cang cao ce cen ceng cha chai chan chang chao che chen cheng chi chong chou chu chuai chuan chuang chui chun chuo
            ci cong cou cu cuan cui cun cuo da dai dan dang dao de dei den deng di dia dian diao die ding diu dong dou du duan dui dun duo
            e ei en eng er fa fan fang fei fen feng fo fou fu ga gai gan gang gao ge gei gen geng gong gou gu gua guai guan guang gui gun guo
            ha hai han hang hao he hei hen heng hong hou hu hua huai huan huang hui hun huo ji jia jian jiang jiao jie jin jing jiong jiu ju juan jue jun
            ka kai kan kang kao ke ken keng kong kou ku kua kuai kuan kuang kui kun kuo la lai lan lang lao le lei leng li lia lian liang liao lie lin ling liu long lou lu luan lue lun luo
            lv ma mai man mang mao me mei men meng mi mian miao mie min ming miu mo mou mu
            na nai nan nang nao ne nei nen neng ni nian niang niao nie nin ning niu nong nou nu nuan nue nun nuo
            nv o ou pa pai pan pang pao pei pen peng pi pian piao pie pin ping po pou pu
            qi qia qian qiang qiao qie qin qing qiong qiu qu quan que qun ran rang rao re ren reng ri rong rou ru ruan rui run ruo
            sa sai san sang sao se sen seng sha shai shan shang shao she shen sheng shi shou shu shua shuai shuan shuang shui shun shuo
            si song sou su suan sui sun suo ta tai tan tang tao te teng ti tian tiao tie ting tong tou tu tuan tui tun tuo
            wa wai wan wang wei wen weng wo wu xi xia xian xiang xiao xie xin xing xiong xiu xu xuan xue xun
            ya yan yang yao ye yi yin ying yo yong you yu yuan yue yun za zai zan zang zao ze zei zen zeng zha zhai zhan zhang zhao zhe zhen zheng zhi zhong zhou zhu zhua zhuai zhuan zhuang zhui zhun zhuo
            zi zong zou zu zuan zui zun zuo
            """;
        return raw.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
            .ToHashSet(StringComparer.Ordinal);
    }
}
