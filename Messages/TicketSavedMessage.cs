namespace GuiPiao.Messages
{
    /// <summary>
    /// 车票保存成功消息
    /// </summary>
    public class TicketSavedMessage
    {
        /// <summary>
        /// 车票ID
        /// </summary>
        public int? TicketId { get; set; }

        /// <summary>
        /// 是否为编辑模式（true=编辑，false=新增）
        /// </summary>
        public bool IsEditMode { get; set; }

        /// <summary>
        /// 车次号
        /// </summary>
        public string? TrainNo { get; set; }
    }
}
