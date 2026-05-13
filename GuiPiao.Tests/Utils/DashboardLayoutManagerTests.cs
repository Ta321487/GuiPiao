using System.Collections.Generic;
using GuiPiao.Model;
using GuiPiao.Utils;
using Xunit;

namespace GuiPiao.Tests.Utils;

public class DashboardLayoutManagerTests
{
    private static List<DashboardCard> MakeCards(int count)
    {
        var list = new List<DashboardCard>();
        for (var i = 0; i < count; i++) list.Add(new DashboardCard());
        return list;
    }

    [Fact]
    public void ApplyLayout_两列_行列按索引铺开()
    {
        var cards = MakeCards(4);
        DashboardLayoutManager.ApplyLayout(cards, LayoutType.TwoColumn);
        Assert.Equal(0, cards[0].GridRow);
        Assert.Equal(0, cards[0].GridColumn);
        Assert.Equal(0, cards[1].GridRow);
        Assert.Equal(1, cards[1].GridColumn);
        Assert.Equal(1, cards[2].GridRow);
        Assert.Equal(0, cards[2].GridColumn);
        Assert.Equal(1, cards[3].GridRow);
        Assert.Equal(1, cards[3].GridColumn);
    }

    [Fact]
    public void ApplyLayout_三列_每行三个()
    {
        var cards = MakeCards(5);
        DashboardLayoutManager.ApplyLayout(cards, LayoutType.ThreeColumn);
        Assert.Equal(0, cards[0].GridRow);
        Assert.Equal(2, cards[2].GridColumn);
        Assert.Equal(1, cards[3].GridRow);
        Assert.Equal(0, cards[3].GridColumn);
    }

    [Fact]
    public void GetRequiredRows_与卡片数量一致()
    {
        var cards = MakeCards(5);
        Assert.Equal(3, DashboardLayoutManager.GetRequiredRows(cards, LayoutType.TwoColumn));
        Assert.Equal(2, DashboardLayoutManager.GetRequiredRows(cards, LayoutType.ThreeColumn));
    }

    [Fact]
    public void GetRequiredRows_空列表返回1()
    {
        Assert.Equal(1, DashboardLayoutManager.GetRequiredRows(new List<DashboardCard>(), LayoutType.TwoColumn));
    }

    [Theory]
    [InlineData(LayoutType.ThreeColumn, 3)]
    [InlineData(LayoutType.TwoColumn, 2)]
    public void GetRequiredColumns(LayoutType layout, int cols)
    {
        Assert.Equal(cols, DashboardLayoutManager.GetRequiredColumns(layout));
    }
}
