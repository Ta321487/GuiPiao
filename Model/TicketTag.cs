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
}
