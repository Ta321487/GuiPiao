using GuiPiao.Model;

namespace GuiPiao.Messages
{
    /// <summary>
    /// 高级检索消息
    /// 用于在 SearchPanelViewModel 和 TripListViewModel 之间传递检索条件
    /// </summary>
    public class AdvancedSearchMessage
    {
        /// <summary>
        /// 检索条件
        /// </summary>
        public AdvancedSearchCriteria Criteria { get; }

        /// <summary>
        /// 是否为清空检索（重置为默认列表）
        /// </summary>
        public bool IsClear { get; }

        public AdvancedSearchMessage(AdvancedSearchCriteria criteria)
        {
            Criteria = criteria;
            IsClear = false;
        }

        public AdvancedSearchMessage()
        {
            Criteria = new AdvancedSearchCriteria();
            IsClear = true;
        }
    }
}
