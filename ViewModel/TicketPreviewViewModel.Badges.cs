using System;
using System.Collections.Generic;
using System.Windows;
using GuiPiao.Model;

namespace GuiPiao.ViewModel;

public partial class TicketPreviewViewModel
{
    public static bool IsTypeBadgeLetterKind(TicketFaceLayoutElementKind kind) =>
        kind is TicketFaceLayoutElementKind.BadgeLetterXue
            or TicketFaceLayoutElementKind.BadgeLetterHai
            or TicketFaceLayoutElementKind.BadgeLetterWang
            or TicketFaceLayoutElementKind.BadgeLetterDiscount;

    public static bool IsTypeOrPaymentBadgeKind(TicketFaceLayoutElementKind kind) =>
        IsTypeBadgeLetterKind(kind)
        || kind is TicketFaceLayoutElementKind.BadgePaymentRow
            or TicketFaceLayoutElementKind.BadgeRow;

    /// <summary>
    ///     学/孩/网/折（惠）单枚占位宽：无框≈字宽，带框=圆/方框外径（边框相接）。
    /// </summary>
    public double TicketBadgeSlotWidth
    {
        get
        {
            var font = ActiveLayout?.BadgeFont ?? ActiveLayout?.BadgeLetterXueFont ?? 12;
            return ComputeBadgeSlotWidth(font, ShowFramedTicketBadges);
        }
    }

    /// <summary>与当前字号/边框无关的占位宽计算（字号切换前后对比用）。</summary>
    private static double ComputeBadgeSlotWidth(double font, bool framed)
    {
        if (font < 8) font = 8;
        // SemiBold 汉字视觉宽度常略大于 FontSize；带框还需留描边
        return framed
            ? Math.Max(16, Math.Ceiling(font * 1.35 + 4))
            : Math.Max(8, Math.Ceiling(font * 1.12));
    }

    public CornerRadius BadgeCornerRadius
    {
        get
        {
            if (!ShowFramedTicketBadges) return new CornerRadius(0);
            return IsRedTicket
                ? new CornerRadius(2)
                : new CornerRadius(TicketBadgeSlotWidth / 2.0);
        }
    }

    partial void OnShowFramedTicketBadgesChanged(bool value)
    {
        var font = ActiveLayout?.BadgeFont ?? ActiveLayout?.BadgeLetterXueFont ?? 12;
        // value 已是新值，旧框态为 !value
        var prevSlot = ComputeBadgeSlotWidth(font, !value);
        OnPropertyChanged(nameof(ActiveLayout));
        NotifyBadgeMetrics();
        ReflowBadgeGapsPreservingExtras(prevSlot);
    }

    private void NotifyBadgeMetrics()
    {
        OnPropertyChanged(nameof(TicketBadgeSlotWidth));
        OnPropertyChanged(nameof(BadgeCornerRadius));
    }

    /// <summary>共用字号写入布局（含兼容字段）。</summary>
    private void ApplySharedTypeBadgeFont(double font, bool reflowGaps = true)
    {
        var prevSlot = TicketBadgeSlotWidth;
        font = Math.Clamp(font, 6, 72);
        var L = ActiveLayout;
        L.BadgeFont = font;
        L.BadgeLetterXueFont = font;
        L.BadgeLetterHaiFont = font;
        L.BadgeLetterWangFont = font;
        L.BadgeLetterDiscountFont = font;
        L.BadgePaymentRowFont = font;
        NotifyBadgeMetrics();
        if (reflowGaps)
            ReflowBadgeGapsPreservingExtras(prevSlot);
    }

    private static void GetBadgeLetterPos(ObservableTicketFaceLayout L, TicketFaceLayoutElementKind kind,
        out double left, out double top)
    {
        switch (kind)
        {
            case TicketFaceLayoutElementKind.BadgeLetterHai:
                left = L.BadgeLetterHaiLeft;
                top = L.BadgeLetterHaiTop;
                return;
            case TicketFaceLayoutElementKind.BadgeLetterWang:
                left = L.BadgeLetterWangLeft;
                top = L.BadgeLetterWangTop;
                return;
            case TicketFaceLayoutElementKind.BadgeLetterDiscount:
                left = L.BadgeLetterDiscountLeft;
                top = L.BadgeLetterDiscountTop;
                return;
            default:
                left = L.BadgeLetterXueLeft;
                top = L.BadgeLetterXueTop;
                return;
        }
    }

    private static void SetBadgeLetterPos(ObservableTicketFaceLayout L, TicketFaceLayoutElementKind kind,
        double left, double top)
    {
        switch (kind)
        {
            case TicketFaceLayoutElementKind.BadgeLetterHai:
                L.BadgeLetterHaiLeft = left;
                L.BadgeLetterHaiTop = top;
                break;
            case TicketFaceLayoutElementKind.BadgeLetterWang:
                L.BadgeLetterWangLeft = left;
                L.BadgeLetterWangTop = top;
                break;
            case TicketFaceLayoutElementKind.BadgeLetterDiscount:
                L.BadgeLetterDiscountLeft = left;
                L.BadgeLetterDiscountTop = top;
                break;
            default:
                L.BadgeLetterXueLeft = left;
                L.BadgeLetterXueTop = top;
                break;
        }
    }

    private static void NudgeBadgeLetterPos(ObservableTicketFaceLayout L, TicketFaceLayoutElementKind kind,
        double dx, double dy)
    {
        GetBadgeLetterPos(L, kind, out var left, out var top);
        SetBadgeLetterPos(L, kind, left + dx, top + dy);
    }

    /// <summary>成组编辑坐标：整行平移相同 Δ。</summary>
    private void ShiftTypeBadgeRow(double fromLeft, double fromTop, double toLeft, double toTop)
    {
        var dx = toLeft - fromLeft;
        var dy = toTop - fromTop;
        if (Math.Abs(dx) < 1e-9 && Math.Abs(dy) < 1e-9) return;
        var L = ActiveLayout;
        foreach (var kind in new[]
                 {
                     TicketFaceLayoutElementKind.BadgeLetterXue,
                     TicketFaceLayoutElementKind.BadgeLetterHai,
                     TicketFaceLayoutElementKind.BadgeLetterWang,
                     TicketFaceLayoutElementKind.BadgeLetterDiscount
                 })
            NudgeBadgeLetterPos(L, kind, dx, dy);
        L.BadgePaymentRowLeft += dx;
        L.BadgePaymentRowTop += dy;
        L.BadgeRowLeft += dx;
        L.BadgeRowTop += dy;
    }

    private void PushTypeBadgeLetterEditor(TicketFaceLayoutElementKind kind)
    {
        var L = ActiveLayout;
        GetBadgeLetterPos(L, kind, out var oldLeft, out var oldTop);
        var oldFont = L.BadgeFont > 0.01 ? L.BadgeFont : L.BadgeLetterXueFont;
        var fontChanged = Math.Abs(oldFont - EditorFontSize) > 0.01;

        // 成组：整行同 Δ；单枚：只改当前字。都不强制紧贴。
        if (WorkbenchMoveAsGroup)
            ShiftTypeBadgeRow(oldLeft, oldTop, EditorAnchorX, EditorAnchorY);
        else
            SetBadgeLetterPos(L, kind, EditorAnchorX, EditorAnchorY);

        L.BadgeFontFamily = NullIfEmpty(EditorFontFamily);
        // 字号变化时按旧占位宽重排（放大/缩小都收回/撑开）；纯挪位置不动间距
        ApplySharedTypeBadgeFont(EditorFontSize, reflowGaps: fontChanged);
    }

    private void PullTypeBadgeLetterEditor(TicketFaceLayoutElementKind kind)
    {
        var L = ActiveLayout;
        GetBadgeLetterPos(L, kind, out var left, out var top);
        EditorAnchorX = left;
        EditorAnchorY = top;
        EditorFontSize = L.BadgeFont > 0.01 ? L.BadgeFont : L.BadgeLetterXueFont;
        EditorFontFamily = L.BadgeFontFamily ?? string.Empty;
    }

    private (bool show, TicketFaceLayoutElementKind kind)[] GetBadgePackOrder()
    {
        var packAll = IsLayoutWorkbench;
        return
        [
            (packAll || ShowTicketBadgeXue, TicketFaceLayoutElementKind.BadgeLetterXue),
            (packAll || ShowTicketBadgeHai, TicketFaceLayoutElementKind.BadgeLetterHai),
            (packAll || ShowTicketBadgeWang, TicketFaceLayoutElementKind.BadgeLetterWang),
            (packAll || ShowTicketBadgeDiscount, TicketFaceLayoutElementKind.BadgeLetterDiscount)
        ];
    }

    /// <summary>
    ///     字号/边框导致占位宽变化后重排：新间距 = 新占位 + max(0, 旧间距 − 旧占位)。
    ///     紧贴行随放大/缩小对称伸缩；你多拉开的空隙会保留。成组平移不调用。
    /// </summary>
    public void ReflowBadgeGapsPreservingExtras(double previousSlot)
    {
        var L = ActiveLayout;
        if (L == null) return;
        if (previousSlot < 1) previousSlot = 1;

        NotifyBadgeMetrics();
        var newSlot = TicketBadgeSlotWidth;
        var ordered = GetBadgePackOrder();
        var packAll = IsLayoutWorkbench;

        var items = new List<(TicketFaceLayoutElementKind kind, double left, double top)>(4);
        foreach (var (show, kind) in ordered)
        {
            if (!show) continue;
            GetBadgeLetterPos(L, kind, out var left, out var top);
            items.Add((kind, left, top));
        }

        if (items.Count == 0)
        {
            L.BadgePaymentRowLeft = L.BadgeLetterXueLeft;
            L.BadgePaymentRowTop = L.BadgeLetterXueTop;
            return;
        }

        double originX;
        double originY;
        if (packAll)
        {
            originX = L.BadgeLetterXueLeft;
            originY = L.BadgeLetterXueTop;
        }
        else
        {
            originX = items[0].left;
            originY = items[0].top;
        }

        var paymentExtra = Math.Max(0, L.BadgePaymentRowLeft - (items[^1].left + previousSlot));

        var placedLeft = originX;
        SetBadgeLetterPos(L, items[0].kind, originX, items[0].top);
        for (var i = 1; i < items.Count; i++)
        {
            var oldGap = items[i].left - items[i - 1].left;
            var extra = Math.Max(0, oldGap - previousSlot);
            placedLeft += newSlot + extra;
            SetBadgeLetterPos(L, items[i].kind, placedLeft, items[i].top);
        }

        L.BadgePaymentRowLeft = placedLeft + newSlot + paymentExtra;
        L.BadgeRowLeft = originX;
        L.BadgeRowTop = originY;
    }

    /// <summary>
    ///     强制从左到右紧贴（显式对齐）。版式工作台排满四字；行程预览只排实际出现的简字。
    /// </summary>
    public void PackTypeBadgeLetters()
    {
        var L = ActiveLayout;
        if (L == null) return;

        NotifyBadgeMetrics();

        var slot = TicketBadgeSlotWidth;
        var ordered = GetBadgePackOrder();
        var packAll = IsLayoutWorkbench;

        double originX = L.BadgeLetterXueLeft;
        double originY = L.BadgeLetterXueTop;
        var any = false;
        foreach (var (show, kind) in ordered)
        {
            if (!show) continue;
            GetBadgeLetterPos(L, kind, out originX, out originY);
            any = true;
            break;
        }

        if (!any)
        {
            L.BadgePaymentRowLeft = L.BadgeLetterXueLeft;
            L.BadgePaymentRowTop = L.BadgeLetterXueTop;
            return;
        }

        if (packAll)
        {
            originX = L.BadgeLetterXueLeft;
            originY = L.BadgeLetterXueTop;
        }

        var x = originX;
        var y = originY;
        foreach (var (show, kind) in ordered)
        {
            if (!show) continue;
            SetBadgeLetterPos(L, kind, x, y);
            x += slot;
        }

        L.BadgePaymentRowLeft = x;
        L.BadgePaymentRowTop = y;
        L.BadgeRowLeft = originX;
        L.BadgeRowTop = originY;
    }
}
