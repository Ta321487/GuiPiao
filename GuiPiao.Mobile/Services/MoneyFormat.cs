using System.Globalization;

namespace GuiPiao.Mobile.Services;

/// <summary>与 PC <c>GuiPiao.Utils.MoneyFormat</c> 对齐：展示用 ￥，输入仍为数字。</summary>
public static class MoneyFormat
{
    public const string SymbolText = "￥";

    public static string Display(decimal money) =>
        $"{SymbolText}{money.ToString("0.00", CultureInfo.InvariantCulture)}";
}
