using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nirvachak_AI.Domain.Entities;
using Nirvachak_AI.Infrastructure.Data;

namespace Nirvachak_AI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin,SuperAdmin,CampaignManager")]
public class WardsController : ApiBaseController
{
    private readonly AppDbContext _db;
    public WardsController(AppDbContext db) => _db = db;

    public record WardDto(int Id, string WardNumber, string WardName, string? Description);
    public record WardRequest(string WardNumber, string WardName, string? Description);

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var cId = GetConstituencyId();
        if (!cId.HasValue) return Ok(new List<WardDto>());
        var wards = await _db.Wards
            .Where(w => w.ConstituencyId == cId.Value)
            .OrderBy(w => w.WardNumber)
            .Select(w => new WardDto(w.Id, w.WardNumber, w.WardName, w.Description))
            .ToListAsync();
        return Ok(wards);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] WardRequest req)
    {
        var cId = GetConstituencyId();
        if (!cId.HasValue) return BadRequest(new { error = "constituency not resolved" });
        if (string.IsNullOrWhiteSpace(req.WardName) || string.IsNullOrWhiteSpace(req.WardNumber))
            return BadRequest(new { error = "Ward number and name are required" });

        var w = new Ward
        {
            WardNumber = req.WardNumber.Trim(),
            WardName = req.WardName.Trim(),
            Description = req.Description?.Trim(),
            ConstituencyId = cId.Value
        };
        _db.Wards.Add(w);
        await _db.SaveChangesAsync();
        return Ok(new { id = w.Id });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] WardRequest req)
    {
        var cId = GetConstituencyId();
        var w = await _db.Wards.FindAsync(id);
        if (w == null) return NotFound();
        if (cId.HasValue && w.ConstituencyId != cId.Value) return Forbid();

        w.WardNumber = req.WardNumber.Trim();
        w.WardName = req.WardName.Trim();
        w.Description = req.Description?.Trim();
        await _db.SaveChangesAsync();
        return Ok();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var cId = GetConstituencyId();
        var w = await _db.Wards.FindAsync(id);
        if (w == null) return NotFound();
        if (cId.HasValue && w.ConstituencyId != cId.Value) return Forbid();

        _db.Wards.Remove(w);
        await _db.SaveChangesAsync();
        return Ok();
    }
}
