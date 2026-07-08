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
public class PannaPramukhController : ApiBaseController
{
    private readonly AppDbContext _db;
    public PannaPramukhController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int? booth)
    {
        var cId  = GetConstituencyId();
        var isSA = GetUserRole() == nameof(UserRole.SuperAdmin);

        var q = _db.PannaPramukhs.AsNoTracking().AsQueryable();
        if (!isSA && cId.HasValue) q = q.Where(p => p.ConstituencyId == cId.Value);
        else if (!isSA) return Ok(new List<PannaPramukhItem>());
        if (booth.HasValue) q = q.Where(p => p.BoothNumber == booth.Value);

        var items = await q.Where(p => p.IsActive)
            .OrderBy(p => p.BoothNumber).ThenBy(p => p.PannaNumber)
            .Select(p => new PannaPramukhItem(
                p.Id, p.Name, p.Phone, p.BoothNumber, p.PannaNumber,
                p.TotalVotersAssigned, p.VotersContacted,
                p.TotalVotersAssigned > 0
                    ? Math.Round((double)p.VotersContacted / p.TotalVotersAssigned * 100, 1) : 0,
                p.IsActive, p.Notes))
            .ToListAsync();
        return Ok(items);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePannaPramukhRequest req)
    {
        var cId = GetConstituencyId() ?? 1;
        _db.PannaPramukhs.Add(new Domain.Entities.PannaPramukh
        {
            Name                 = req.Name.Trim(),
            Phone                = req.Phone.Trim(),
            Email                = req.Email,
            Address              = req.Address,
            BoothNumber          = req.BoothNumber,
            PannaNumber          = req.PannaNumber.Trim(),
            TotalVotersAssigned  = req.TotalVotersAssigned,
            Notes                = req.Notes,
            ConstituencyId       = cId,
            IsActive             = true,
        });
        await _db.SaveChangesAsync();
        return Ok(new ApiResult(true, "Panna Pramukh added."));
    }

    [HttpPut("{id:int}/contact")]
    public async Task<IActionResult> UpdateContact(int id, [FromBody] UpdatePannaContactRequest req)
    {
        var pp = await _db.PannaPramukhs.FindAsync(id);
        if (pp is null) return NotFound();
        pp.VotersContacted = req.VotersContacted;
        await _db.SaveChangesAsync();
        return Ok(new ApiResult(true, "Updated."));
    }
}
