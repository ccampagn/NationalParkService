namespace NationalParkServiceAPI.Models;

// A site and whether it's free for every night of the requested stay.
public record SiteAvailability(
    int SiteId,
    string SiteNumber,
    string SiteType,
    int MaxGuests,
    int NightlyRate,
    bool Available);
