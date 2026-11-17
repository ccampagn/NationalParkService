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
    // The loop or area the site is in, e.g. "Loop A", from Recreation.gov. Null for sites not imported from there.
    public string? Loop { get; set; }
    // Recreation.gov's CampsiteID, for sites imported by ImportRecreationGovSites.sql.
    public int? RecreationGovId { get; set; }
    // Placeholder sites replaced by real ones but kept because reservations point at them. Can't be booked.
    public bool Retired { get; set; }
}
