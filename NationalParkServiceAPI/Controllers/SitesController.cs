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
    [HttpGet]
    public async Task<ActionResult<IEnumerable<SiteAvailability>>> GetSites(int campgroundId, DateOnly checkIn, DateOnly checkOut)
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

        var booked = _context.Reservations.Overlapping(checkIn, checkOut).Select(r => r.SiteId);

        return await _context.Sites
            .AsNoTracking()
            .Where(s => s.CampgroundId == campgroundId)
            .OrderBy(s => s.SiteNumber)
            .Select(s => new SiteAvailability(
                s.SiteId,
                s.SiteNumber,
                s.SiteType,
                s.MaxGuests,
                s.NightlyRate,
                !booked.Contains(s.SiteId)))
            .ToListAsync();
    }
}
