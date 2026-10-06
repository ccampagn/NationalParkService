using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NationalParkServiceAPI.Data;
using NationalParkServiceAPI.Models;

namespace NationalParkServiceAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PassesController : ControllerBase
{
    private readonly NationalParkServiceDbContext _context;

    public PassesController(NationalParkServiceDbContext context)
    {
        _context = context;
    }

    // GET: api/passes
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Pass>>> GetPasses()
    {
        return await _context.Passes.AsNoTracking().Include(p => p.PassType).ToListAsync();
    }

    // GET: api/passes/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Pass>> GetPass(int id)
    {
        var pass = await _context.Passes.AsNoTracking()
            .Include(p => p.PassType)
            .FirstOrDefaultAsync(p => p.PassId == id);

        if (pass == null)
        {
            return NotFound();
        }

        return pass;
    }

    // POST: api/passes
    [HttpPost]
    public async Task<ActionResult<Pass>> CreatePass(PassRequest request)
    {
        var passTypeExists = await _context.PassTypes.AnyAsync(pt => pt.PassTypeId == request.PassTypeId);
        if (!passTypeExists)
        {
            return BadRequest($"PassType {request.PassTypeId} does not exist.");
        }

        var pass = new Pass
        {
            PassTypeId = request.PassTypeId,
            IssueDate = request.IssueDate,
            ExpirationDate = request.ExpirationDate,
            Active = request.Active
        };

        _context.Passes.Add(pass);
        await _context.SaveChangesAsync();

        await _context.Entry(pass).Reference(p => p.PassType).LoadAsync();

        return CreatedAtAction(nameof(GetPass), new { id = pass.PassId }, pass);
    }

    // PUT: api/passes/5
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdatePass(int id, PassRequest request)
    {
        var pass = await _context.Passes.FindAsync(id);
        if (pass == null)
        {
            return NotFound();
        }

        var passTypeExists = await _context.PassTypes.AnyAsync(pt => pt.PassTypeId== request.PassTypeId);
        if (!passTypeExists)
        {
            return BadRequest($"PassType {request.PassTypeId} does not exist.");
        }

        pass.PassTypeId = request.PassTypeId;
        pass.IssueDate = request.IssueDate;
        pass.ExpirationDate = request.ExpirationDate;
        pass.Active = request.Active;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: api/passes/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePass(int id)
    {
        var pass = await _context.Passes.FindAsync(id);
        if (pass == null)
        {
            return NotFound();
        }

        _context.Passes.Remove(pass);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
