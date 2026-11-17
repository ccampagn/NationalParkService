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
    // Fees follow Recreation.gov's overnight camping policy. The reservation fee is never refunded.
    public const int ReservationFee = 8;
    // Charged for moving to dates that share no nights with the current ones; extending or shortening is free.
    public const int ChangeFee = 10;
    // Kept from the refund when cancelling.
    public const int CancellationFee = 10;
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
        if (site.Retired)
        {
            return BadRequest($"Site {site.SiteNumber} can no longer be booked.");
        }

        if (await StayError(site, request.CheckIn, request.CheckOut) is { } stayError)
        {
            return BadRequest(stayError);
        }
        if (request.Guests < 1 || request.Guests > site.MaxGuests)
        {
            return BadRequest($"Site {site.SiteNumber} fits 1 to {site.MaxGuests} guests.");
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
            Total = (request.CheckOut.DayNumber - request.CheckIn.DayNumber) * site.NightlyRate + ReservationFee,
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

    // PUT: api/reservations/NPS-ABC123 (moves the stay to a new site in the same campground and/or new dates)
    [HttpPut("{code}")]
    public async Task<ActionResult<ReservationResponse>> ChangeReservation(string code, ReservationChangeRequest request)
    {
        var reservation = await MyReservations().FirstOrDefaultAsync(r => r.ConfirmationCode == code);
        if (reservation == null)
        {
            return NotFound();
        }

        var today = DateOnly.FromDateTime(_timeProvider.GetLocalNow().DateTime);
        if (reservation.CheckIn <= today)
        {
            return BadRequest("Trips can't be changed online from the check-in day on. Contact the campground.");
        }

        var site = await _context.Sites.FindAsync(request.SiteId);
        if (site == null)
        {
            return BadRequest($"Site {request.SiteId} does not exist.");
        }
        // A trip can stay on its own retired site, but not move to another one.
        if (site.Retired && site.SiteId != reservation.SiteId)
        {
            return BadRequest($"Site {site.SiteNumber} can no longer be booked.");
        }
        if (site.CampgroundId != reservation.CampgroundId)
        {
            return BadRequest("You can only move to another site in the same campground.");
        }
        if (await StayError(site, request.CheckIn, request.CheckOut) is { } stayError)
        {
            return BadRequest(stayError);
        }
        if (reservation.Guests > site.MaxGuests)
        {
            return BadRequest($"Site {site.SiteNumber} fits up to {site.MaxGuests} guests.");
        }

        // Serializable for the same reason as in CreateReservation.
        await using var transaction = _context.Database.IsRelational()
            ? await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable)
            : null;

        if (await _context.Reservations
                .Where(r => r.SiteId == site.SiteId && r.ReservationId != reservation.ReservationId)
                .Overlapping(request.CheckIn, request.CheckOut)
                .AnyAsync())
        {
            return Conflict($"Site {site.SiteNumber} is already booked for some of those nights.");
        }

        // Staying on the same site keeps the rate it was booked at; a new site is charged at its current rate.
        if (site.SiteId != reservation.SiteId)
        {
            reservation.SiteId = site.SiteId;
            reservation.NightlyRate = site.NightlyRate;
        }
        if (!(request.CheckIn < reservation.CheckOut && reservation.CheckIn < request.CheckOut))
        {
            reservation.ChangeFees += ChangeFee;
        }
        reservation.CheckIn = request.CheckIn;
        reservation.CheckOut = request.CheckOut;
        reservation.Total = (request.CheckOut.DayNumber - request.CheckIn.DayNumber) * reservation.NightlyRate
            + reservation.ReservationFee + reservation.ChangeFees;

        await _context.SaveChangesAsync();
        if (transaction != null)
        {
            await transaction.CommitAsync();
        }

        return await QueryResponses(_context.Reservations.Where(r => r.ReservationId == reservation.ReservationId))
            .FirstAsync();
    }

    // DELETE: api/reservations/NPS-ABC123
    [HttpDelete("{code}")]
    public async Task<ActionResult<CancellationResponse>> CancelReservation(string code)
    {
        var reservation = await MyReservations().FirstOrDefaultAsync(r => r.ConfirmationCode == code);
        if (reservation == null)
        {
            return NotFound();
        }

        var today = DateOnly.FromDateTime(_timeProvider.GetLocalNow().DateTime);
        if (reservation.CheckIn < today)
        {
            return BadRequest("This trip has already started, so it can't be cancelled online. Contact the campground.");
        }

        var refund = CancellationRefund(reservation, today);
        _context.Reservations.Remove(reservation);
        await _context.SaveChangesAsync();

        return new CancellationResponse(code, refund);
    }

    // The reservation and change fees are never refunded, and the cancellation fee is kept. Cancelling the day before or
    // the day of arrival also forfeits the first night, or everything for a one-night stay.
    public static int CancellationRefund(Reservation reservation, DateOnly today)
    {
        var late = reservation.CheckIn <= today.AddDays(1);
        var nights = reservation.CheckOut.DayNumber - reservation.CheckIn.DayNumber;
        if (late && nights == 1)
        {
            return 0;
        }

        var refund = reservation.Total - reservation.ReservationFee - reservation.ChangeFees - CancellationFee
            - (late ? reservation.NightlyRate : 0);
        return Math.Max(refund, 0);
    }

    // Why a stay at `site` from checkIn to checkOut isn't allowed, or null if it is. Doesn't check other bookings.
    private async Task<string?> StayError(Site site, DateOnly checkIn, DateOnly checkOut)
    {
        var today = DateOnly.FromDateTime(_timeProvider.GetLocalNow().DateTime);
        var nights = checkOut.DayNumber - checkIn.DayNumber;

        if (checkIn < today)
        {
            return "Check-in can't be in the past.";
        }
        if (nights < 1)
        {
            return "Check-out must be after check-in.";
        }
        if (nights > MaxNights)
        {
            return $"Stays are limited to {MaxNights} nights.";
        }

        var campground = await _context.Campgrounds.FindAsync(site.CampgroundId);
        return campground!.FirstClosedNight(checkIn, checkOut) is { } closedNight
            ? campground.ClosedMessage(closedNight)
            : null;
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
                   r.SiteId,
                   s == null ? null : s.SiteNumber,
                   s == null ? null : s.SiteType,
                   r.CheckIn,
                   r.CheckOut,
                   r.CheckOut.DayNumber - r.CheckIn.DayNumber,
                   r.Guests,
                   r.NightlyRate,
                   r.ReservationFee,
                   r.ChangeFees,
                   r.Total,
                   // Stored without a kind; mark it UTC so it serializes with a 'Z'.
                   DateTime.SpecifyKind(r.CreatedAt, DateTimeKind.Utc));
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
