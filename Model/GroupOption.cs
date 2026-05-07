namespace GuiPiao.Model
{
    /// <summary>
    /// 分组选项
    /// </summary>
    public enum GroupOption
    {
        /// <summary>
        /// 不分组
        /// </summary>
        None,

        /// <summary>
        /// 按日期(日)分组
        /// </summary>
        DateDay,

        /// <summary>
        /// 按日期(月)分组
        /// </summary>
        DateMonth,

        /// <summary>
        /// 按车次分组
        /// </summary>
        TrainNo,

        /// <summary>
        /// 按出发站分组
        /// </summary>
        Departure,

        /// <summary>
        /// 按到达站分组
        /// </summary>
        Arrival
    }
}
