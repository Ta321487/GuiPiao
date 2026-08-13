namespace GuiPiao.Model;

public class StationInfo
{
    public int Id { get; set; }
    public string StationName { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public string StationCode { get; set; } = string.Empty;
    public string StationPinyin { get; set; } = string.Empty;
    public int StationLevel { get; set; }
    public string RailwayBureau { get; set; } = string.Empty;
    public string Longitude { get; set; } = string.Empty;
    public string Latitude { get; set; } = string.Empty;
}
