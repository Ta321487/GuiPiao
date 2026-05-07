namespace GuiPiao.Model;

public class TicketTag
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Color { get; set; }
    public string TextColor { get; set; }
    public int SortOrder { get; set; }
    public bool IsDefault { get; set; }
    public string CreatedAt { get; set; }
}