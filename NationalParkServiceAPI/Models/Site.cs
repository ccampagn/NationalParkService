namespace NationalParkServiceAPI.Models;

// One bookable site (a numbered pad or pitch) inside a campground.
public class Site
{
    public int SiteId { get; set; }
    public int CampgroundId { get; set; }
    public string SiteNumber { get; set; } = string.Empty;
    public string SiteType { get; set; } = string.Empty;
    public int MaxGuests { get; set; }
    public int NightlyRate { get; set; }
}
