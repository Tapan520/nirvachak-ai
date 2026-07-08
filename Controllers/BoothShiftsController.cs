using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nirvachak_AI.Domain.Enums;
using Nirvachak_AI.Infrastructure.Data;
using Nirvachak_AI.Models.Api;

namespace Nirvachak_AI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class BoothShiftsController : ApiBaseController
{
    private readonly AppDbContext _db;
    public BoothShiftsController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetShifts([FromQuery] int? booth)
    {
        var cId = GetConstituencyId();
        var isSA = GetUserRole() == nameof(UserRole.SuperAdmin);

        var q = _db.BoothShiftAssignments
            .Include(b => b.Volunteer)
            .AsNoTracking()
            .AsQueryable();

        if (!isSA && cId.HasValue) q = q.Where(b => b.ConstituencyId == cId.Value);
        else if (!isSA) return Ok(new List<BoothShiftItem>());
        if (booth.HasValue) q = q.Where(b => b.BoothNumber == booth.Value);

        var items = await q.OrderBy(b => b.ShiftStart)
            .Select(b => new BoothShiftItem(
                b.Id, b.VolunteerId, b.Volunteer!.Name, b.Volunteer.Phone,
                b.BoothNumber, b.ShiftStart, b.ShiftEnd,
                b.Role.ToString(), b.IsConfirmed, b.Notes))
            .ToListAsync();
        return Ok(items);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateBoothShiftRequest req)
    {
        var cId = GetConstituencyId() ?? 1;
        if (!Enum.TryParse<ShiftRole>(req.Role, out var role)) role = ShiftRole.BoothAgent;

        _db.BoothShiftAssignments.Add(new Domain.Entities.BoothShiftAssignment
        {
            VolunteerId    = req.VolunteerId,
            BoothNumber    = req.BoothNumber,
            ShiftStart     = req.ShiftStart,
            ShiftEnd       = req.ShiftEnd,
            Role           = role,
            Notes          = req.Notes,
            ConstituencyId = cId,
        });
        await _db.SaveChangesAsync();
        return Ok(new ApiResult(true, "Shift assigned."));
    }

    [HttpPut("{id:int}/confirm")]
    public async Task<IActionResult> Confirm(int id)
    {
        var shift = await _db.BoothShiftAssignments.FindAsync(id);
        if (shift is null) return NotFound();
        shift.IsConfirmed = true;
        await _db.SaveChangesAsync();
        return Ok(new ApiResult(true, "Shift confirmed."));
    }
}
