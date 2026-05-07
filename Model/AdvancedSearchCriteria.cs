namespace GuiPiao.Model;

/// <summary>
///     高级检索条件
/// </summary>
public class AdvancedSearchCriteria
{
    /// <summary>
    ///     出发站
    /// </summary>
    public string? DepartStation { get; set; }

    /// <summary>
    ///     到达站
    /// </summary>
    public string? ArriveStation { get; set; }

    /// <summary>
    ///     车次（完整车次号）
    /// </summary>
    public string? TrainNo { get; set; }

    /// <summary>
    ///     车次前缀（如 G/D/C/Z/T/K 等）
    /// </summary>
    public string? TrainNoPrefix { get; set; }

    /// <summary>
    ///     车次数字部分
    /// </summary>
    public string? TrainNoNumber { get; set; }

    /// <summary>
    ///     日期范围（今年/去年/最近3个月/最近6个月）
    /// </summary>
    public string? DateRange { get; set; }

    /// <summary>
    ///     自定义开始日期（yyyy-MM-dd格式）
    /// </summary>
    public string? StartDate { get; set; }

    /// <summary>
    ///     自定义结束日期（yyyy-MM-dd格式）
    /// </summary>
    public string? EndDate { get; set; }

    /// <summary>
    ///     状态（全部/未出行/已完成）
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    ///     标签ID
    /// </summary>
    public int? TagId { get; set; }

    /// <summary>
    ///     标签名称
    /// </summary>
    public string? TagName { get; set; }

    /// <summary>
    ///     检查是否有任何检索条件
    /// </summary>
    public bool HasAnyCriteria =>
        !string.IsNullOrWhiteSpace(DepartStation) ||
        !string.IsNullOrWhiteSpace(ArriveStation) ||
        !string.IsNullOrWhiteSpace(TrainNo) ||
        !string.IsNullOrWhiteSpace(TrainNoPrefix) ||
        !string.IsNullOrWhiteSpace(TrainNoNumber) ||
        !string.IsNullOrWhiteSpace(DateRange) ||
        !string.IsNullOrWhiteSpace(Status) ||
        !string.IsNullOrWhiteSpace(StartDate) ||
        !string.IsNullOrWhiteSpace(EndDate) ||
        TagId.HasValue;
}