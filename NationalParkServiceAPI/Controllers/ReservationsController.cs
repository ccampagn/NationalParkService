using System.Data;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NationalParkServiceAPI.Data;
using NationalParkServiceAPI.Models;

namespace NationalParkServiceAPI.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class ReservationsController : ControllerBase
{
    public const int ReservationFee = 8;
    public const int MaxNights = 14;
    private const string CodeChars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    private readonly NationalParkServiceDbContext _context;
    private readonly TimeProvider _timeProvider;

    public ReservationsController(NationalParkServiceDbContext context, TimeProvider? timeProvider = null)
    {
        _context = context;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    // GET: api/reservations (the signed-in user's reservations)
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ReservationResponse>>> GetReservations()
    {
        return await QueryResponses(MyReservations()).ToListAsync();
    }

    // GET: api/reservations/NPS-ABC123
    [HttpGet("{code}")]
    public async Task<ActionResult<ReservationResponse>> GetReservation(string code)
    {
        var reservation = await QueryResponses(MyReservations().Where(r => r.ConfirmationCode == code))
            .FirstOrDefaultAsync();

        if (reservation == null)
        {
            return NotFound();
        }

        return reservation;
    }

    // POST: api/reservations
    [HttpPost]
    public async Task<ActionResult<ReservationResponse>> CreateReservation(ReservationRequest request)
    {
        var site = await _context.Sites.FindAsync(request.SiteId);
        if (site == null)
        {
            return BadRequest($"Site {request.SiteId} does not exist.");
        }

        var today = DateOnly.FromDateTime(_timeProvider.GetLocalNow().DateTime);
        var nights = request.CheckOut.DayNumber - request.CheckIn.DayNumber;

        if (request.CheckIn < today)
        {
            return BadRequest("Check-in can't be in the past.");
        }
        if (nights < 1)
        {
            return BadRequest("Check-out must be after check-in.");
        }
        if (nights > MaxNights)
        {
            return BadRequest($"Stays are limited to {MaxNights} nights.");
        }
        if (request.Guests < 1 || request.Guests > site.MaxGuests)
        {
            return BadRequest($"Site {site.SiteNumber} fits 1 to {site.MaxGuests} guests.");
        }

        var campground = await _context.Campgrounds.FindAsync(site.CampgroundId);
        if (campground!.FirstClosedNight(request.CheckIn, request.CheckOut) is { } closedNight)
        {
            return BadRequest(campground.ClosedMessage(closedNight));
        }

        // Serializable so two people can't both pass the availability check for the same site and nights.
        // The in-memory test database has no transactions, so skip it there.
        await using var transaction = _context.Database.IsRelational()
            ? await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable)
            : null;

        if (await _context.Reservations.Where(r => r.SiteId == site.SiteId).Overlapping(request.CheckIn, request.CheckOut).AnyAsync())
        {
            return Conflict($"Site {site.SiteNumber} is already booked for some of those nights.");
        }

        var reservation = new Reservation
        {
            ConfirmationCode = await NewConfirmationCode(),
            UserId = User.GetUserId(),
            CampgroundId = site.CampgroundId,
            SiteId = site.SiteId,
            CheckIn = request.CheckIn,
            CheckOut = request.CheckOut,
            Guests = request.Guests,
            NightlyRate = site.NightlyRate,
            ReservationFee = ReservationFee,
            Total = nights * site.NightlyRate + ReservationFee,
            CreatedAt = _timeProvider.GetUtcNow().UtcDateTime,
        };

        _context.Reservations.Add(reservation);
        await _context.SaveChangesAsync();
        if (transaction != null)
        {
            await transaction.CommitAsync();
        }

        var response = await QueryResponses(_context.Reservations.Where(r => r.ReservationId == reservation.ReservationId))
            .FirstAsync();

        return CreatedAtAction(nameof(GetReservation), new { code = response.Code }, response);
    }

    // DELETE: api/reservations/NPS-ABC123
    [HttpDelete("{code}")]
    public async Task<IActionResult> CancelReservation(string code)
    {
        var reservation = await MyReservations().FirstOrDefaultAsync(r => r.ConfirmationCode == code);
        if (reservation == null)
        {
            return NotFound();
        }

        _context.Reservations.Remove(reservation);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // Reservations belonging to the signed-in user. Other users' codes look the same as codes that don't exist.
    private IQueryable<Reservation> MyReservations()
    {
        var userId = User.GetUserId();
        return _context.Reservations.Where(r => r.UserId == userId);
    }

    // Joins reservations to their campground and park, newest first. Filter `reservations` before calling.
    private IQueryable<ReservationResponse> QueryResponses(IQueryable<Reservation> reservations)
    {
        return from r in reservations.AsNoTracking()
               join c in _context.Campgrounds on r.CampgroundId equals c.CampgroundId
               join p in _context.Parks on c.ParkId equals p.ParkId
               join s in _context.Sites on r.SiteId equals s.SiteId into siteMatch
               from s in siteMatch.DefaultIfEmpty()
               orderby r.CreatedAt descending, r.ReservationId descending
               select new ReservationResponse(
                   r.ConfirmationCode,
                   p.ParkId,
                   p.ParkName,
                   c.CampgroundId,
                   c.CampgroundName,
                   s == null ? null : s.SiteNumber,
                   s == null ? null : s.SiteType,
                   r.CheckIn,
                   r.CheckOut,
                   r.CheckOut.DayNumber - r.CheckIn.DayNumber,
                   r.Guests,
                   r.NightlyRate,
                   r.ReservationFee,
                   r.Total);
    }

    private async Task<string> NewConfirmationCode()
    {
        while (true)
        {
            var code = "NPS-" + RandomNumberGenerator.GetString(CodeChars, 6);
            if (!await _context.Reservations.AnyAsync(r => r.ConfirmationCode == code))
            {
                return code;
            }
        }
    }
}
