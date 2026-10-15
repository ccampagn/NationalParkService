namespace NationalParkServiceAPI.Models;

public class Park
{
    public int ParkId { get; set; }
    public string ParkName { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public string Scene { get; set; } = "peaks";
    public string Tagline { get; set; } = string.Empty;
    public decimal Rating { get; set; }
    public int Reviews { get; set; }
    public int BaseRate { get; set; }
    public int SitesLeft { get; set; }
    public string? Badge { get; set; }
    public List<string> Activities { get; set; } = [];
    public string? ImageUrl { get; set; }
    public string? ImageCredit { get; set; }
    public string? ImageSourceUrl { get; set; }
}
