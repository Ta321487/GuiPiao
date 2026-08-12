using System;
using System.Windows;
using System.Windows.Controls;

namespace GuiPiao.View;

/// <summary>
///     卡片视图面板，支持自动换行或固定列数。
///     自动模式：按 CardWidth 估算每行列数后，把剩余宽度均分到卡片，避免行尾大块空白。
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
            new FrameworkPropertyMetadata(0,
                FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange));

    /// <summary>
    ///     卡片宽度（仅在自动模式下作为「期望宽度」用于估算列数）
    /// </summary>
    public static readonly DependencyProperty CardWidthProperty =
        DependencyProperty.Register(
            nameof(CardWidth),
            typeof(double),
            typeof(CardViewPanel),
            new FrameworkPropertyMetadata(280.0,
                FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange));

    /// <summary>
    ///     卡片间距
    /// </summary>
    public static readonly DependencyProperty CardSpacingProperty =
        DependencyProperty.Register(
            nameof(CardSpacing),
            typeof(double),
            typeof(CardViewPanel),
            new FrameworkPropertyMetadata(8.0,
                FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange));

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

        var availableWidth = availableSize.Width;
        var cardsPerRow = ResolveCardsPerRow(availableWidth);
        var cardWidth = ResolveCardWidth(availableWidth, cardsPerRow);

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
        var width = double.IsInfinity(availableWidth) || availableWidth <= 0
            ? cardsPerRow * cardWidth + (cardsPerRow - 1) * CardSpacing
            : availableWidth;
        return new Size(width, maxHeight);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (InternalChildren.Count == 0)
            return finalSize;

        var finalWidth = finalSize.Width;
        var cardsPerRow = ResolveCardsPerRow(finalWidth);
        var cardWidth = ResolveCardWidth(finalWidth, cardsPerRow);
        var currentX = 0.0;
        var currentY = 0.0;
        var rowHeight = 0.0;

        for (var i = 0; i < InternalChildren.Count; i++)
        {
            var child = InternalChildren[i];
            var childSize = child.DesiredSize;

            if (i % cardsPerRow == 0 && i > 0)
            {
                currentX = 0;
                currentY += rowHeight + CardSpacing;
                rowHeight = 0;
            }

            child.Arrange(new Rect(currentX, currentY, cardWidth, childSize.Height));
            currentX += cardWidth + CardSpacing;
            rowHeight = Math.Max(rowHeight, childSize.Height);
        }

        return finalSize;
    }

    /// <summary>
    ///     固定列数直接用设置值；自动模式按期望 CardWidth 估算能放下几列（至少 1）。
    /// </summary>
    private int ResolveCardsPerRow(double availableWidth)
    {
        if (CardsPerRow > 0)
            return CardsPerRow;

        if (double.IsInfinity(availableWidth) || availableWidth <= 0)
            return 1;

        var preferred = Math.Max(100.0, CardWidth);
        var count = (int)Math.Floor((availableWidth + CardSpacing) / (preferred + CardSpacing));
        return Math.Max(1, count);
    }

    /// <summary>
    ///     把当前行可用宽度均分给列，吃掉行尾空白。
    /// </summary>
    private double ResolveCardWidth(double availableWidth, int cardsPerRow)
    {
        cardsPerRow = Math.Max(1, cardsPerRow);
        if (double.IsInfinity(availableWidth) || availableWidth <= 0)
            return Math.Max(100.0, CardWidth);

        var totalSpacing = (cardsPerRow - 1) * CardSpacing;
        return Math.Max(100.0, (availableWidth - totalSpacing) / cardsPerRow);
    }
}
