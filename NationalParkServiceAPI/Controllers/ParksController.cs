using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NationalParkServiceAPI.Data;
using NationalParkServiceAPI.Models;

namespace NationalParkServiceAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ParksController : ControllerBase
{
    private readonly NationalParkServiceDbContext _context;

    public ParksController(NationalParkServiceDbContext context)
    {
        _context = context;
    }

    // GET: api/parks
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Park>>> GetParks()
    {
        return await _context.Parks
            .AsNoTracking()
            .Include(p => p.Campgrounds.OrderBy(c => c.CampgroundName))
            .OrderBy(p => p.ParkName)
            .ToListAsync();
    }

    // GET: api/parks/available?checkIn=2026-11-04&checkOut=2026-11-07
    // Parks with at least one site that's in season and free every night of the stay, and how many such sites each has.
    [HttpGet("available")]
    public async Task<ActionResult<IEnumerable<ParkAvailability>>> GetAvailableParks(DateOnly checkIn, DateOnly checkOut)
    {
        var nights = checkOut.DayNumber - checkIn.DayNumber;
        if (nights < 1)
        {
            return BadRequest("Check-out must be after check-in.");
        }
        if (nights > ReservationsController.MaxNights)
        {
            return BadRequest($"Stays are limited to {ReservationsController.MaxNights} nights.");
        }

        // Seasons are checked in memory; there are only a couple hundred campgrounds.
        var openCampgroundIds = (await _context.Campgrounds.AsNoTracking().ToListAsync())
            .Where(c => c.FirstClosedNight(checkIn, checkOut) == null)
            .Select(c => c.CampgroundId)
            .ToList();
        var overlapping = _context.Reservations.Overlapping(checkIn, checkOut);

        return await (from s in _context.Sites
                      where openCampgroundIds.Contains(s.CampgroundId)
                            && !overlapping.Any(r => r.SiteId == s.SiteId)
                      join c in _context.Campgrounds on s.CampgroundId equals c.CampgroundId
                      group s by c.ParkId into park
                      orderby park.Key
                      select new ParkAvailability(park.Key, park.Count()))
            .ToListAsync();
    }
}
