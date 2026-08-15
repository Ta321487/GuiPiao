namespace GuiPiao.Model;

public class TicketTag
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public string TextColor { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsDefault { get; set; }
    public string CreatedAt { get; set; } = string.Empty;

    /// <summary>跨端稳定 ID（UUID）。</summary>
    public string SyncId { get; set; } = string.Empty;

    /// <summary>最后修改时间（UTC ISO-8601）。</summary>
    public string UpdatedAt { get; set; } = string.Empty;

    /// <summary>软删时间；空表示未删除。</summary>
    public string? DeletedAt { get; set; }
}
