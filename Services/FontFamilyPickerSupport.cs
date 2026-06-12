using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Media;

namespace GuiPiao.Services;

/// <summary>
///     票面工作台：系统字体列表与从 ttf/otf/ttc 解析 WPF <see cref="FontFamily.Source" />。
/// </summary>
public static class FontFamilyPickerSupport
{
    private static readonly Lazy<List<string>> SystemFontSourcesLazy = new(BuildSystemFontSources);

    public static IReadOnlyList<string> SystemFontFamilySources => SystemFontSourcesLazy.Value;

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
