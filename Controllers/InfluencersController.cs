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
public class InfluencersController : ApiBaseController
{
    private readonly AppDbContext _db;
    public InfluencersController(AppDbContext db) => _db = db;

    /// <summary>List active influencers for the current constituency</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<InfluencerListItem>), 200)]
    public async Task<IActionResult> GetInfluencers([FromQuery] string? alignment)
    {
        var cId  = GetConstituencyId();
        var role = GetUserRole();
        var isSuperAdmin = role == nameof(UserRole.SuperAdmin);

        IQueryable<Domain.Entities.Influencer> q = _db.Influencers
            .Where(i => i.IsActive).AsNoTracking();

        if (!isSuperAdmin && cId.HasValue) q = q.Where(i => i.ConstituencyId == cId.Value);
        else if (!isSuperAdmin) return Ok(new List<InfluencerListItem>());

        if (!string.IsNullOrEmpty(alignment) &&
            Enum.TryParse<InfluencerAlignment>(alignment, out var a))
            q = q.Where(i => i.Alignment == a);

        var items = await q.OrderByDescending(i => i.EstimatedFollowers)
            .Select(i => new InfluencerListItem(
                i.Id, i.Name, i.MobileNumber, i.Category, i.Community,
                i.EstimatedFollowers, i.Ward, i.BoothNumber,
                i.Alignment.ToString(), i.Notes,
                i.LastMetAt, i.LastMeetingOutcome))
            .ToListAsync();

        return Ok(items);
    }

    /// <summary>Create a new influencer record</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResult), 200)]
    public async Task<IActionResult> Create([FromBody] CreateInfluencerRequest req)
    {
        var cId = GetConstituencyId() ?? 1;
        if (!Enum.TryParse<InfluencerAlignment>(req.Alignment, out var alignment))
            alignment = InfluencerAlignment.Unknown;

        _db.Influencers.Add(new Domain.Entities.Influencer
        {
            Name               = req.Name.Trim(),
            MobileNumber       = req.MobileNumber,
            Category           = req.Category,
            Community          = req.Community,
            EstimatedFollowers = req.EstimatedFollowers,
            Ward               = req.Ward,
            BoothNumber        = req.BoothNumber,
            Alignment          = alignment,
            Notes              = req.Notes,
            ConstituencyId     = cId,
            IsActive           = true,
        });

        await _db.SaveChangesAsync();
        return Ok(new ApiResult(true, "Influencer added successfully."));
    }

    /// <summary>Update influencer alignment and meeting outcome</summary>
    [HttpPut("{id:int}/meeting")]
    [ProducesResponseType(typeof(ApiResult), 200)]
    public async Task<IActionResult> UpdateMeeting(int id, [FromBody] UpdateInfluencerMeetingRequest req)
    {
        var influencer = await _db.Influencers.FindAsync(id);
        if (influencer is null) return NotFound();

        if (Enum.TryParse<InfluencerAlignment>(req.Alignment, out var alignment))
            influencer.Alignment = alignment;
        influencer.LastMetAt            = DateTime.UtcNow;
        influencer.LastMeetingOutcome   = req.OutcomeNotes;
        if (!string.IsNullOrEmpty(req.Notes)) influencer.Notes = req.Notes;

        await _db.SaveChangesAsync();
        return Ok(new ApiResult(true, "Updated successfully."));
    }
}
