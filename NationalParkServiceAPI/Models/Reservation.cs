namespace NationalParkServiceAPI.Models;

public class Reservation
{
    public int ReservationId { get; set; }
    public string ConfirmationCode { get; set; } = string.Empty;
    // Null for reservations made before user accounts existed; those aren't shown to anyone.
    public int? UserId { get; set; }
    public int CampgroundId { get; set; }
    // Null for reservations made before individual sites could be booked.
    public int? SiteId { get; set; }
    public DateOnly CheckIn { get; set; }
    public DateOnly CheckOut { get; set; }
    public int Guests { get; set; }
    public int NightlyRate { get; set; }
    public int ReservationFee { get; set; }
    public int Total { get; set; }
    public DateTime CreatedAt { get; set; }
}
