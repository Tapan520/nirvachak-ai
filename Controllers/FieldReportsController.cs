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
public class FieldReportsController : ApiBaseController
{
    private readonly AppDbContext _db;
    public FieldReportsController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetReports()
    {
        var cId  = GetConstituencyId();
        var uid  = GetUserId();
        var role = GetUserRole();
        var isSA = role == nameof(UserRole.SuperAdmin);
        var isAdmin = role == nameof(UserRole.Admin) || role == nameof(UserRole.CampaignManager);

        var q = _db.FieldReports.AsNoTracking().AsQueryable();
        if (!isSA && cId.HasValue) q = q.Where(r => r.ConstituencyId == cId.Value);
        else if (!isSA) return Ok(new List<FieldReportItem>());
        if (!isSA && !isAdmin) q = q.Where(r => r.WorkerUserId == uid);

        var items = await q.OrderByDescending(r => r.ReportDate)
            .Take(60)
            .Select(r => new FieldReportItem(
                r.Id, r.WorkerName, r.ReportDate,
                r.ContactsMade, r.FavourContacts, r.FloatingContacts, r.AgainstContacts,
                r.IssuesLogged, r.Highlights, r.Challenges,
                r.PlannedForTomorrow, r.Status.ToString()))
            .ToListAsync();
        return Ok(items);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateFieldReportRequest req)
    {
        var cId = GetConstituencyId() ?? 1;
        _db.FieldReports.Add(new Domain.Entities.FieldReport
        {
            WorkerUserId       = GetUserId(),
            WorkerName         = GetUserFullName(),
            ReportDate         = DateTime.UtcNow.Date,
            ContactsMade       = req.ContactsMade,
            FavourContacts     = req.FavourContacts,
            FloatingContacts   = req.FloatingContacts,
            AgainstContacts    = req.AgainstContacts,
            IssuesLogged       = req.IssuesLogged,
            Highlights         = req.Highlights,
            Challenges         = req.Challenges,
            PlannedForTomorrow = req.PlannedForTomorrow,
            Status             = FieldReportStatus.Submitted,
            ConstituencyId     = cId,
        });
        await _db.SaveChangesAsync();
        return Ok(new ApiResult(true, "Report submitted."));
    }
}
