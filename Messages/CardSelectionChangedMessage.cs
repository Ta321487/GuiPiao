namespace GuiPiao.Messages;

/// <summary>
///     卡片选中状态变更消息
/// </summary>
public class CardSelectionChangedMessage
{
    public CardSelectionChangedMessage(bool hasSelectedItems, int selectedItemsCount)
    {
        HasSelectedItems = hasSelectedItems;
        SelectedItemsCount = selectedItemsCount;
    }

    public bool HasSelectedItems { get; }
    public int SelectedItemsCount { get; }
}