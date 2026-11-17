namespace NationalParkServiceAPI.Models;

// New site and dates for an existing reservation. The site must be in the same campground; the guests stay the same.
public record ReservationChangeRequest(int SiteId, DateOnly CheckIn, DateOnly CheckOut);
