namespace NationalParkServiceAPI.Models;

// A park and how many of its sites are in season and free for every night of the requested stay.
public record ParkAvailability(int ParkId, int SitesLeft);
