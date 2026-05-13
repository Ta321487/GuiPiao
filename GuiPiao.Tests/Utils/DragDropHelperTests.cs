using System.Collections.ObjectModel;
using GuiPiao.Utils;
using Xunit;

namespace GuiPiao.Tests.Utils;

public class DragDropHelperTests
{
    private class SortableItem : ISortable
    {
        public string Name { get; set; } = "";
        public int SortOrder { get; set; }
    }

    [Fact]
    public void MoveItem_合法索引时移动元素()
    {
        var c = new ObservableCollection<string> { "a", "b", "c" };
        DragDropHelper.MoveItem(c, 0, 2);
        Assert.Equal(new[] { "b", "c", "a" }, c);
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, -1)]
    [InlineData(0, 3)]
    [InlineData(0, 0)]
    public void MoveItem_非法索引或相同索引不改变集合(int from, int to)
    {
        var c = new ObservableCollection<int> { 1, 2, 3 };
        DragDropHelper.MoveItem(c, from, to);
        Assert.Equal(new[] { 1, 2, 3 }, c);
    }

    [Fact]
    public void UpdateSortOrder_按集合顺序写入SortOrder()
    {
        var c = new ObservableCollection<SortableItem>
        {
            new() { Name = "x", SortOrder = 99 },
            new() { Name = "y", SortOrder = 98 }
        };
        DragDropHelper.UpdateSortOrder(c);
        Assert.Equal(0, c[0].SortOrder);
        Assert.Equal(1, c[1].SortOrder);
    }
}
