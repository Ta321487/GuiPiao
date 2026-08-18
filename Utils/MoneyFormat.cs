using System;
using System.Globalization;

namespace GuiPiao.Utils;

/// <summary>
///     产品金额展示：符号一律「￥」。入库与输入框仍是纯数字。
/// </summary>
public static class MoneyFormat
{
    public const char Symbol = '￥';
    public const string SymbolText = "￥";

    public static string Display(decimal money) =>
        $"{Symbol}{money.ToString("0.00", CultureInfo.InvariantCulture)}";

    public static string Display(double money) => Display((decimal)money);

    public static string DisplayFromRaw(string? raw)
    {
        return TryParse(raw, out var money) ? Display(money) : string.Empty;
    }

    public static string Strip(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return string.Empty;

        return raw.Trim()
            .Replace("¥", "", StringComparison.Ordinal)
            .Replace("￥", "", StringComparison.Ordinal)
            .Replace(",", "", StringComparison.Ordinal)
            .Trim();
    }

    public static bool TryParse(string? raw, out decimal money)
    {
        var s = Strip(raw);
        if (string.IsNullOrEmpty(s))
        {
            money = 0m;
            return false;
        }

        return decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out money)
               || decimal.TryParse(s, NumberStyles.Number, CultureInfo.CurrentCulture, out money);
    }

    /// <summary>统计指标名是否金额类（花费 / 金额 / 成本 / 票价）。</summary>
    public static bool LooksLikeMoneyCaption(string? caption)
    {
        if (string.IsNullOrEmpty(caption))
            return false;
        return caption.Contains("花费") || caption.Contains("金额")
               || caption.Contains("成本") || caption.Contains("票价");
    }

    public static string FormatStatisticValue(string? caption, double value)
    {
        return LooksLikeMoneyCaption(caption)
            ? Display(value)
            : value.ToString("0.##", CultureInfo.InvariantCulture);
    }
}
