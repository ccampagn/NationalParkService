namespace NationalParkServiceAPI.Models;

// A reservation with its park and campground names, shaped for the "My trips" list.
public record ReservationResponse(
    string Code,
    int ParkId,
    string ParkName,
    int CampgroundId,
    string Campground,
    string? SiteNumber,
    string? SiteType,
    DateOnly CheckIn,
    DateOnly CheckOut,
    int Nights,
    int Guests,
    int NightlyRate,
    int ReservationFee,
    int Total);
