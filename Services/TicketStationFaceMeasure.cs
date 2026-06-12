using System;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using GuiPiao.Model;

namespace GuiPiao.Services;

/// <summary>
///     811 票面站名主体宽度测量（用于旧布局「站」字绝对坐标 → 相对间距迁移）。
/// </summary>
public static class TicketStationFaceMeasure
{
    private const string LegacyMigrationReferenceBody = "上海虹桥";

    public static double MeasurePreviewBodyWidth(
        string? rawStation,
        int characterSpacingUnits,
        double fontSizePx,
        string? nameFontFamily,
        string? stationFontFamily,
        string? layoutDefaultFont)
    {
        var text = TicketPreviewDraft.FormatStationNameForPreviewFace(rawStation, characterSpacingUnits);
        return MeasureFormattedStationText(text, fontSizePx, nameFontFamily, stationFontFamily, layoutDefaultFont);
    }

    public static double MeasureFormattedStationText(
        string text,
        double fontSizePx,
        string? nameFontFamily,
        string? stationFontFamily,
        string? layoutDefaultFont)
    {
        if (string.IsNullOrEmpty(text) || fontSizePx <= 0.01) return 0;

        var familySource = FirstNonEmpty(nameFontFamily, stationFontFamily, layoutDefaultFont) ?? "Microsoft YaHei UI";
        FontFamily family;
        try
        {
            family = new FontFamily(familySource);
        }
        catch
        {
            family = new FontFamily("Microsoft YaHei UI");
        }

        var typeface = new Typeface(family, FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);
        var formatted = new FormattedText(
            text,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            typeface,
            fontSizePx,
            Brushes.Black,
            1.0);

        return formatted.WidthIncludingTrailingWhitespace;
    }

    /// <summary>旧 JSON 以 Canvas 绝对坐标存「站」字时，按 4 字参考站名估算主体宽度并反推间距。</summary>
    public static double EstimateLegacyStationBodyWidth(int characterSpacingUnits, double fontSizePx) =>
        MeasurePreviewBodyWidth(LegacyMigrationReferenceBody, characterSpacingUnits, fontSizePx, null, null, null);

    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var v in values)
            if (!string.IsNullOrWhiteSpace(v))
                return v.Trim();
        return null;
    }
}
