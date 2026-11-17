using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NationalParkServiceAPI.Data;
using NationalParkServiceAPI.Models;

namespace NationalParkServiceAPI.Controllers;

[ApiController]
[Route("api/campgrounds/{campgroundId}/sites")]
public class SitesController : ControllerBase
{
    private readonly NationalParkServiceDbContext _context;

    public SitesController(NationalParkServiceDbContext context)
    {
        _context = context;
    }

    // GET: api/campgrounds/5/sites?checkIn=2026-11-04&checkOut=2026-11-07
    // With &except=NPS-ABC123, the signed-in user's own reservation with that code doesn't count as booking its site,
    // so a trip being changed can stay on (or extend) its current site.
    [HttpGet]
    public async Task<ActionResult<IEnumerable<SiteAvailability>>> GetSites(int campgroundId, DateOnly checkIn, DateOnly checkOut, string? except = null)
    {
        var campground = await _context.Campgrounds.FindAsync(campgroundId);
        if (campground == null)
        {
            return NotFound();
        }
        if (checkOut <= checkIn)
        {
            return BadRequest("Check-out must be after check-in.");
        }
        if (campground.FirstClosedNight(checkIn, checkOut) is { } closedNight)
        {
            return BadRequest(campground.ClosedMessage(closedNight));
        }

        var reservations = _context.Reservations.AsQueryable();
        if (except != null && User.FindFirstValue(ClaimTypes.NameIdentifier) is { } userId)
        {
            var id = int.Parse(userId);
            reservations = reservations.Where(r => !(r.ConfirmationCode == except && r.UserId == id));
        }
        var booked = reservations.Overlapping(checkIn, checkOut).Select(r => r.SiteId);

        return await _context.Sites
            .AsNoTracking()
            .Where(s => s.CampgroundId == campgroundId && !s.Retired)
            .OrderBy(s => s.Loop)
            .ThenBy(s => s.SiteNumber)
            .Select(s => new SiteAvailability(
                s.SiteId,
                s.SiteNumber,
                s.SiteType,
                s.Loop,
                s.MaxGuests,
                s.NightlyRate,
                !booked.Contains(s.SiteId)))
            .ToListAsync();
    }
}
