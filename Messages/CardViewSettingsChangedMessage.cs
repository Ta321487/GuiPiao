namespace GuiPiao.Messages;

public class CardViewSettingsChangedMessage(
    int cardsPerRow,
    int cardWidth,
    int cardSpacing,
    int cardCornerRadius,
    bool cardShowViewAction,
    bool cardShowEditAction,
    bool cardShowRescheduleAction,
    bool cardShowRefundAction,
    bool cardShowDeleteAction,
    bool cardEnableMultiSelect,
    bool cardBatchShowView,
    bool cardBatchShowEdit,
    bool cardBatchShowReschedule,
    bool cardBatchShowRefund,
    bool cardBatchShowDelete,
    string cardStatusPosition,
    bool cardHoverHighlight,
    bool cardShowShadow,
    bool cardHoverScale)
{
    public int CardsPerRow { get; } = cardsPerRow;
    public int CardWidth { get; } = cardWidth;
    public int CardSpacing { get; } = cardSpacing;
    public int CardCornerRadius { get; } = cardCornerRadius;
    public bool CardShowViewAction { get; } = cardShowViewAction;
    public bool CardShowEditAction { get; } = cardShowEditAction;
    public bool CardShowRescheduleAction { get; } = cardShowRescheduleAction;
    public bool CardShowRefundAction { get; } = cardShowRefundAction;
    public bool CardShowDeleteAction { get; } = cardShowDeleteAction;
    public bool CardEnableMultiSelect { get; } = cardEnableMultiSelect;
    public bool CardBatchShowView { get; } = cardBatchShowView;
    public bool CardBatchShowEdit { get; } = cardBatchShowEdit;
    public bool CardBatchShowReschedule { get; } = cardBatchShowReschedule;
    public bool CardBatchShowRefund { get; } = cardBatchShowRefund;
    public bool CardBatchShowDelete { get; } = cardBatchShowDelete;
    public string CardStatusPosition { get; } = cardStatusPosition;
    public bool CardHoverHighlight { get; } = cardHoverHighlight;
    public bool CardShowShadow { get; } = cardShowShadow;
    public bool CardHoverScale { get; } = cardHoverScale;
}
