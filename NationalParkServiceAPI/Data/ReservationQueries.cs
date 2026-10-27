using NationalParkServiceAPI.Models;

namespace NationalParkServiceAPI.Data;

public static class ReservationQueries
{
    // Reservations that share at least one night with [checkIn, checkOut). Checking out the morning
    // someone else checks in is not an overlap.
    public static IQueryable<Reservation> Overlapping(this IQueryable<Reservation> reservations, DateOnly checkIn, DateOnly checkOut) =>
        reservations.Where(r => r.CheckIn < checkOut && checkIn < r.CheckOut);
}
