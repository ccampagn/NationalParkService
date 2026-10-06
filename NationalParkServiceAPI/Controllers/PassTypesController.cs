using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NationalParkServiceAPI.Data;
using NationalParkServiceAPI.Models;

namespace NationalParkServiceAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PassTypesController : ControllerBase
{
    private readonly NationalParkServiceDbContext _context;

    public PassTypesController(NationalParkServiceDbContext context)
    {
        _context = context;
    }

    // GET: api/passtypes
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PassType>>> GetPassTypes()
    {
        return await _context.PassTypes.AsNoTracking().ToListAsync();
    }
}
