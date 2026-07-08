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
public class CompetitorController : ApiBaseController
{
    private readonly AppDbContext _db;
    public CompetitorController(AppDbContext db) => _db = db;

    /// <summary>List competitor activities</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<CompetitorActivityItem>), 200)]
    public async Task<IActionResult> GetActivities(
        [FromQuery] string? competitor,
        [FromQuery] string? threat)
    {
        var cId  = GetConstituencyId();
        var role = GetUserRole();
        var isSuperAdmin = role == nameof(UserRole.SuperAdmin);

        IQueryable<Domain.Entities.CompetitorActivity> q = _db.CompetitorActivities.AsNoTracking();
        if (!isSuperAdmin && cId.HasValue) q = q.Where(a => a.ConstituencyId == cId.Value);
        else if (!isSuperAdmin) return Ok(new List<CompetitorActivityItem>());

        if (!string.IsNullOrEmpty(competitor)) q = q.Where(a => a.CompetitorName == competitor);
        if (!string.IsNullOrEmpty(threat) &&
            Enum.TryParse<CompetitorThreatLevel>(threat, out var t))
            q = q.Where(a => a.ThreatLevel == t);

        var items = await q.OrderByDescending(a => a.ActivityDate)
            .Take(100)
            .Select(a => new CompetitorActivityItem(
                a.Id, a.CompetitorName, a.PartyName,
                a.ActivityTitle, a.ActivityType.ToString(),
                a.Location, a.Ward, a.BoothNumber,
                a.ActivityDate, a.EstimatedCrowd,
                a.Notes, a.ThreatLevel.ToString()))
            .ToListAsync();

        return Ok(items);
    }

    /// <summary>Log a new competitor activity</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResult), 200)]
    public async Task<IActionResult> Create([FromBody] CreateCompetitorActivityRequest req)
    {
        var cId    = GetConstituencyId() ?? 1;
        var userId = GetUserId();

        if (!Enum.TryParse<CompetitorActivityType>(req.ActivityType, out var actType))
            actType = CompetitorActivityType.Other;
        if (!Enum.TryParse<CompetitorThreatLevel>(req.ThreatLevel, out var threat))
            threat = CompetitorThreatLevel.Medium;

        _db.CompetitorActivities.Add(new Domain.Entities.CompetitorActivity
        {
            CompetitorName  = req.CompetitorName.Trim(),
            PartyName       = req.PartyName,
            ActivityTitle   = req.ActivityTitle.Trim(),
            ActivityType    = actType,
            Location        = req.Location,
            Ward            = req.Ward,
            BoothNumber     = req.BoothNumber,
            ActivityDate    = req.ActivityDate,
            EstimatedCrowd  = req.EstimatedCrowd,
            Notes           = req.Notes,
            ThreatLevel     = threat,
            ConstituencyId  = cId,
            LoggedByUserId  = userId,
        });

        await _db.SaveChangesAsync();
        return Ok(new ApiResult(true, "Activity logged successfully."));
    }
}
