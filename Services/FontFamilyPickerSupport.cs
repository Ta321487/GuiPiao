using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Media;

namespace GuiPiao.Services;

/// <summary>
///     票面工作台：推荐字体、系统字体列表与从 ttf/otf/ttc 解析 WPF <see cref="FontFamily.Source" />。
/// </summary>
public static class FontFamilyPickerSupport
{
    private static readonly Lazy<List<string>> SystemFontSourcesLazy = new(BuildSystemFontSources);
    private static readonly Lazy<List<string>> RecommendedLazy = new(BuildRecommendedInstalled);

    /// <summary>偏好顺序：票面常用中文，其次常见西文（仅保留本机已安装的）。</summary>
    private static readonly string[] RecommendedCandidates =
    [
        "微软雅黑", "Microsoft YaHei UI", "Microsoft YaHei",
        "宋体", "SimSun", "NSimSun",
        "黑体", "SimHei",
        "楷体", "KaiTi",
        "仿宋", "FangSong",
        "等线", "DengXian",
        "思源黑体", "Source Han Sans SC", "Noto Sans CJK SC",
        "Arial", "Times New Roman", "Segoe UI"
    ];

    /// <summary>下拉「继承/默认」项的 Source 哨兵（WPF ComboBox 的 SelectedValue 无法稳定选中空串）。</summary>
    public const string InheritSourceSentinel = "$inherit$";

    public static bool IsInheritSource(string? source) =>
        string.IsNullOrWhiteSpace(source) ||
        string.Equals(source.Trim(), InheritSourceSentinel, StringComparison.Ordinal);

    /// <summary>布局存储值 → 下拉 SelectedValue（空 → 哨兵）。</summary>
    public static string ToComboSource(string? layoutSource) =>
        string.IsNullOrWhiteSpace(layoutSource) ? InheritSourceSentinel : layoutSource.Trim();

    /// <summary>下拉 SelectedValue → 布局存储值（哨兵 → 空）。</summary>
    public static string FromComboSource(string? comboSource) =>
        IsInheritSource(comboSource) ? string.Empty : comboSource!.Trim();

    public static IReadOnlyList<string> SystemFontFamilySources => SystemFontSourcesLazy.Value;

    /// <summary>本机已安装的推荐字体（短列表，供下拉「推荐」分组）。</summary>
    public static IReadOnlyList<string> RecommendedInstalledSources => RecommendedLazy.Value;

    private static List<string> BuildSystemFontSources()
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var f in Fonts.SystemFontFamilies)
        {
            try
            {
                var s = f.Source?.Trim();
                if (!string.IsNullOrEmpty(s)) set.Add(s);
            }
            catch
            {
                // 个别系统字体族解析失败时跳过
            }
        }

        return set.OrderBy(s => s, StringComparer.CurrentCultureIgnoreCase).ToList();
    }

    private static List<string> BuildRecommendedInstalled()
    {
        var sys = SystemFontFamilySources;
        var list = new List<string>();
        foreach (var cand in RecommendedCandidates)
        {
            var hit = sys.FirstOrDefault(s => string.Equals(s, cand, StringComparison.OrdinalIgnoreCase));
            if (hit == null) continue;
            if (list.Any(x => string.Equals(x, hit, StringComparison.OrdinalIgnoreCase))) continue;
            list.Add(hit);
        }

        return list;
    }

    /// <summary>若与系统/推荐列表大小写不同，规范成列表中的写法。</summary>
    public static string CanonicalizeSource(string? source)
    {
        if (IsInheritSource(source)) return string.Empty;
        var cur = source!.Trim();
        foreach (var s in RecommendedInstalledSources)
            if (string.Equals(s, cur, StringComparison.OrdinalIgnoreCase))
                return s;
        foreach (var s in SystemFontFamilySources)
            if (string.Equals(s, cur, StringComparison.OrdinalIgnoreCase))
                return s;
        return cur;
    }

    public static bool IsInRecommended(string? source) =>
        !string.IsNullOrWhiteSpace(source) &&
        RecommendedInstalledSources.Any(s =>
            string.Equals(s, source.Trim(), StringComparison.OrdinalIgnoreCase));

    /// <summary>下拉显示名：file URI 取 # 后族名。</summary>
    public static string ShortDisplayName(string? source)
    {
        var s = (source ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(s)) return "默认";
        var i = s.LastIndexOf('#');
        if (i >= 0 && i < s.Length - 1)
            return s[(i + 1)..].Trim();
        return s;
    }

    /// <summary>
    ///     从字体文件得到可写入布局 JSON、并可被 <see cref="FontFamily" /> 解析的 Source 字符串。
    /// </summary>
    public static bool TryResolveFamilySourceFromFontFile(string fontFilePath, out string? familySource)
    {
        familySource = null;
        if (string.IsNullOrWhiteSpace(fontFilePath)) return false;
        var full = Path.GetFullPath(fontFilePath);
        if (!File.Exists(full)) return false;

        try
        {
            var fileUri = new Uri(full, UriKind.Absolute);
            var glyph = new GlyphTypeface(fileUri);
            var enUs = CultureInfo.GetCultureInfo("en-us");
            if (!glyph.FamilyNames.TryGetValue(enUs, out var familyName) || string.IsNullOrWhiteSpace(familyName))
                familyName = glyph.FamilyNames.Values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));
            if (string.IsNullOrWhiteSpace(familyName)) return false;

            var dir = Path.GetDirectoryName(full);
            if (string.IsNullOrEmpty(dir)) return false;
            var baseUri = new Uri(dir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) +
                                  Path.DirectorySeparatorChar, UriKind.Absolute);
            var ff = new FontFamily(baseUri, "./#" + familyName);
            familySource = ff.Source;
            return !string.IsNullOrWhiteSpace(familySource);
        }
        catch
        {
            return false;
        }
    }
}
