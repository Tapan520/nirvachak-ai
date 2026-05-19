using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nirvachak_AI.Infrastructure.Data;
using Nirvachak_AI.Models.Api;

namespace Nirvachak_AI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class CampaignEventsController : ApiBaseController
{
    private readonly AppDbContext _db;

    public CampaignEventsController(AppDbContext db) => _db = db;

    [HttpGet]
    [ProducesResponseType(typeof(List<CampaignEventListItem>), 200)]
    public async Task<IActionResult> GetEvents([FromQuery] bool? upcoming)
    {
        var cId = GetConstituencyId();
        var query = _db.CampaignEvents.AsQueryable();
        if (cId.HasValue) query = query.Where(e => e.ConstituencyId == cId.Value);
        if (upcoming == true) query = query.Where(e => e.ScheduledAt >= DateTime.UtcNow && !e.IsCompleted);

        var items = await query
            .OrderByDescending(e => e.ScheduledAt)
            .Select(e => new CampaignEventListItem(
                e.Id, e.Title, e.EventType.ToString(), e.Location,
                e.ScheduledAt, e.ExpectedAttendance, e.ActualAttendance,
                e.OrganizedByName, e.IsCompleted, e.TargetWards, e.Description))
            .ToListAsync();

        return Ok(items);
    }

    /// <summary>Create a new campaign event</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResult), 200)]
    public async Task<IActionResult> Create([FromBody] CreateCampaignEventRequest req)
    {
        var cId = GetConstituencyId() ?? 1;
        var userName = GetUserFullName();

        if (!Enum.TryParse<Domain.Enums.CampaignEventType>(req.EventType, out var evtType))
            evtType = Domain.Enums.CampaignEventType.Other;

        _db.CampaignEvents.Add(new Domain.Entities.CampaignEvent
        {
            Title                = req.Title.Trim(),
            EventType            = evtType,
            Location             = req.Location.Trim(),
            ScheduledAt          = req.ScheduledAt,
            ExpectedAttendance   = req.ExpectedAttendance,
            Description          = req.Description,
            TargetWards          = req.TargetWards,
            TargetBoothNumbers   = req.TargetBoothNumbers,
            OrganizedByName      = userName,
            ConstituencyId       = cId
        });

        await _db.SaveChangesAsync();
        return Ok(new ApiResult(true, "Campaign event created successfully."));
    }
}
