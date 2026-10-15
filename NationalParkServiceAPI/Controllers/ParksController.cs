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
        return await _context.Parks.AsNoTracking().OrderBy(p => p.ParkName).ToListAsync();
    }
}
