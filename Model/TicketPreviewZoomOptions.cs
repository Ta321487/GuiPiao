using System.Collections.Generic;

namespace GuiPiao.Model;

/// <summary>
///     票面预览缩放下拉项（设置页「默认缩放」与预览窗口「缩放」共用，与 <see cref="ViewModel.TicketPreviewViewModel" /> 的 +/- 档位一致）。
/// </summary>
public sealed class TicketPreviewZoomListItem
{
    public required string Display { get; init; }
    public required string Tag { get; init; }
}

public static class TicketPreviewZoomOptions
{
    /// <summary>下拉展示与写入配置的 Tag（FitWindow 或整数字符串）。</summary>
    public static IReadOnlyList<TicketPreviewZoomListItem> ComboItems { get; } =
        new TicketPreviewZoomListItem[]
        {
            new() { Display = "适应窗口", Tag = "FitWindow" },
            new() { Display = "50%", Tag = "50" },
            new() { Display = "75%", Tag = "75" },
            new() { Display = "100%", Tag = "100" },
            new() { Display = "125%", Tag = "125" },
            new() { Display = "150%", Tag = "150" },
            new() { Display = "200%", Tag = "200" },
            new() { Display = "300%", Tag = "300" },
            new() { Display = "400%", Tag = "400" }
        };
}
