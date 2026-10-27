namespace NationalParkServiceAPI.Models;

public record ReservationRequest(int SiteId, DateOnly CheckIn, DateOnly CheckOut, int Guests);
