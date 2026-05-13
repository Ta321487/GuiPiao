using System;
using System.Windows;
using System.Windows.Controls;

namespace GuiPiao.View;

/// <summary>
///     卡片视图面板，支持自动换行或固定列数
/// </summary>
public class CardViewPanel : Panel
{
    /// <summary>
    ///     每行卡片数，0表示自动（根据设置的CardWidth和CardSpacing自适应）
    /// </summary>
    public static readonly DependencyProperty CardsPerRowProperty =
        DependencyProperty.Register(
            nameof(CardsPerRow),
            typeof(int),
            typeof(CardViewPanel),
            new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsMeasure));

    /// <summary>
    ///     卡片宽度（仅在自动模式下使用）
    /// </summary>
    public static readonly DependencyProperty CardWidthProperty =
        DependencyProperty.Register(
            nameof(CardWidth),
            typeof(double),
            typeof(CardViewPanel),
            new FrameworkPropertyMetadata(280.0, FrameworkPropertyMetadataOptions.AffectsMeasure));

    /// <summary>
    ///     卡片间距
    /// </summary>
    public static readonly DependencyProperty CardSpacingProperty =
        DependencyProperty.Register(
            nameof(CardSpacing),
            typeof(double),
            typeof(CardViewPanel),
            new FrameworkPropertyMetadata(8.0, FrameworkPropertyMetadataOptions.AffectsMeasure));

    public int CardsPerRow
    {
        get => (int)GetValue(CardsPerRowProperty);
        set => SetValue(CardsPerRowProperty, value);
    }

    public double CardWidth
    {
        get => (double)GetValue(CardWidthProperty);
        set => SetValue(CardWidthProperty, value);
    }

    public double CardSpacing
    {
        get => (double)GetValue(CardSpacingProperty);
        set => SetValue(CardSpacingProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        if (InternalChildren.Count == 0)
            return new Size(0, 0);

        var cardsPerRow = CardsPerRow;
        var availableWidth = availableSize.Width;

        // 如果设置了固定列数，且可用宽度有效
        if (cardsPerRow > 0 && !double.IsInfinity(availableWidth) && availableWidth > 0)
        {
            // 固定列数模式：自动计算卡片宽度
            // 公式：(总宽度 - (列数-1) * 间距) / 列数
            var totalSpacing = (cardsPerRow - 1) * CardSpacing;
            var cardWidth = Math.Max(100, (availableWidth - totalSpacing) / cardsPerRow);

            var maxHeight = 0.0;
            var currentRowHeight = 0.0;

            for (var i = 0; i < InternalChildren.Count; i++)
            {
                var child = InternalChildren[i];
                child.Measure(new Size(cardWidth, double.PositiveInfinity));

                if (i % cardsPerRow == 0 && i > 0)
                {
                    maxHeight += currentRowHeight + CardSpacing;
                    currentRowHeight = 0;
                }

                currentRowHeight = Math.Max(currentRowHeight, child.DesiredSize.Height);
            }

            maxHeight += currentRowHeight;
            return new Size(availableWidth, maxHeight);
        }

        // 自动换行模式（使用设置的CardWidth和CardSpacing）
        var currentX = 0.0;
        var currentY = 0.0;
        var rowHeight = 0.0;
        var totalWidth = 0.0;
        var totalHeight = 0.0;
        var itemWidth = CardWidth + CardSpacing;

        foreach (UIElement child in InternalChildren)
        {
            // 强制使用设置的CardWidth作为测量宽度
            child.Measure(new Size(CardWidth, double.PositiveInfinity));

            // 检查是否需要换行
            if (currentX + CardWidth > availableWidth && currentX > 0)
            {
                currentX = 0;
                currentY += rowHeight + CardSpacing;
                rowHeight = 0;
            }

            currentX += itemWidth;
            // 使用实际测量高度，但宽度使用设置的CardWidth
            rowHeight = Math.Max(rowHeight, child.DesiredSize.Height);
            totalWidth = Math.Max(totalWidth, currentX);
        }

        totalHeight = currentY + rowHeight;
        return new Size(Math.Min(totalWidth, availableWidth), totalHeight);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (InternalChildren.Count == 0)
            return finalSize;

        var cardsPerRow = CardsPerRow;
        var finalWidth = finalSize.Width;

        if (cardsPerRow > 0 && !double.IsInfinity(finalWidth) && finalWidth > 0)
        {
            // 固定列数模式
            var totalSpacing = (cardsPerRow - 1) * CardSpacing;
            var cardWidth = Math.Max(100, (finalWidth - totalSpacing) / cardsPerRow);
            var currentX = 0.0;
            var currentY = 0.0;
            var rowHeight = 0.0;

            for (var i = 0; i < InternalChildren.Count; i++)
            {
                var child = InternalChildren[i];
                var childSize = child.DesiredSize;

                if (i % cardsPerRow == 0 && i > 0)
                {
                    // 换行
                    currentX = 0;
                    currentY += rowHeight + CardSpacing;
                    rowHeight = 0;
                }

                child.Arrange(new Rect(currentX, currentY, cardWidth, childSize.Height));
                currentX += cardWidth + CardSpacing;
                rowHeight = Math.Max(rowHeight, childSize.Height);
            }
        }
        else
        {
            // 自动换行模式
            var currentX = 0.0;
            var currentY = 0.0;
            var rowHeight = 0.0;
            var itemWidth = CardWidth + CardSpacing;

            foreach (UIElement child in InternalChildren)
            {
                var childSize = child.DesiredSize;

                // 检查是否需要换行
                if (currentX + CardWidth > finalWidth && currentX > 0)
                {
                    currentX = 0;
                    currentY += rowHeight + CardSpacing;
                    rowHeight = 0;
                }

                child.Arrange(new Rect(currentX, currentY, CardWidth, childSize.Height));
                currentX += itemWidth;
                rowHeight = Math.Max(rowHeight, childSize.Height);
            }
        }

        return finalSize;
    }
}