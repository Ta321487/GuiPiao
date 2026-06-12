using System;
using System.Windows;
using System.Windows.Media;
using GuiPiao.Model;

namespace GuiPiao.View;

/// <summary>
///     票面布局工作台：为 Canvas 子元素标记可命中的布局块类型，便于在「票面上拖拽微调」时点击同步「编辑元素」下拉（须设在块根容器上，如票种/支付简字 Grid、二维码 Image）。
/// </summary>
public static class LayoutWorkbenchHit
{
    public static readonly DependencyProperty KindProperty = DependencyProperty.RegisterAttached(
        "Kind",
        typeof(object),
        typeof(LayoutWorkbenchHit),
        new PropertyMetadata(null));

    public static void SetKind(DependencyObject obj, object value) => obj.SetValue(KindProperty, value);

    public static TicketFaceLayoutElementKind? TryGetKind(DependencyObject obj) => CoerceKind(obj.GetValue(KindProperty));

    /// <summary>自命中视觉沿父链查找最近的 <see cref="KindProperty" />。</summary>
    public static TicketFaceLayoutElementKind? TryResolveKind(DependencyObject? leaf)
    {
        for (var o = leaf; o != null; o = VisualTreeHelper.GetParent(o))
        {
            var k = TryGetKind(o);
            if (k.HasValue) return k;
        }

        return null;
    }

    private static TicketFaceLayoutElementKind? CoerceKind(object? v)
    {
        if (v == null) return null;
        if (v is TicketFaceLayoutElementKind k) return k;
        if (v is string s && Enum.TryParse<TicketFaceLayoutElementKind>(s, out var parsed)) return parsed;
        return null;
    }
}
