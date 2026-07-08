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
public class RapidResponseController : ApiBaseController
{
    private readonly AppDbContext _db;
    public RapidResponseController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? status)
    {
        var cId  = GetConstituencyId();
        var isSA = GetUserRole() == nameof(UserRole.SuperAdmin);

        var q = _db.RapidResponseItems.AsNoTracking().AsQueryable();
        if (!isSA && cId.HasValue) q = q.Where(r => r.ConstituencyId == cId.Value);
        else if (!isSA) return Ok(new List<RapidResponseListItem>());

        if (!string.IsNullOrEmpty(status) &&
            Enum.TryParse<RapidResponseStatus>(status, out var st))
            q = q.Where(r => r.Status == st);

        var items = await q.OrderByDescending(r => r.DetectedAt)
            .Select(r => new RapidResponseListItem(
                r.Id, r.Title, r.Description, r.Source,
                r.AffectedWards, r.AssignedToName,
                r.ResponseText, r.Status.ToString(), r.ThreatLevel.ToString(),
                r.DetectedAt, r.ResolvedAt))
            .ToListAsync();
        return Ok(items);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRapidResponseRequest req)
    {
        var cId = GetConstituencyId() ?? 1;
        if (!Enum.TryParse<RapidResponseThreat>(req.ThreatLevel, out var threat))
            threat = RapidResponseThreat.Medium;

        _db.RapidResponseItems.Add(new Domain.Entities.RapidResponseItem
        {
            Title          = req.Title.Trim(),
            Description    = req.Description.Trim(),
            Source         = req.Source,
            AffectedWards  = req.AffectedWards,
            ThreatLevel    = threat,
            ResponseText   = req.ResponseText,
            Status         = RapidResponseStatus.Detected,
            LoggedByUserId = GetUserId(),
            ConstituencyId = cId,
        });
        await _db.SaveChangesAsync();
        return Ok(new ApiResult(true, "Incident logged."));
    }

    [HttpPut("{id:int}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateRapidResponseRequest req)
    {
        var item = await _db.RapidResponseItems.FindAsync(id);
        if (item is null) return NotFound();
        if (Enum.TryParse<RapidResponseStatus>(req.Status, out var st)) item.Status = st;
        if (!string.IsNullOrEmpty(req.ResponseText)) item.ResponseText = req.ResponseText;
        if (item.Status == RapidResponseStatus.Resolved) item.ResolvedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(new ApiResult(true, "Updated."));
    }
}
