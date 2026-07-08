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
public class BroadcastController : ApiBaseController
{
    private readonly AppDbContext _db;
    public BroadcastController(AppDbContext db) => _db = db;

    [HttpGet("templates")]
    public async Task<IActionResult> GetTemplates()
    {
        var cId = GetConstituencyId();
        var isSA = GetUserRole() == nameof(UserRole.SuperAdmin);
        var q = _db.MessageTemplates.AsNoTracking().AsQueryable();
        if (!isSA && cId.HasValue) q = q.Where(t => t.ConstituencyId == cId.Value);
        else if (!isSA) return Ok(new List<MessageTemplateItem>());

        var items = await q.OrderByDescending(t => t.CreatedAt)
            .Select(t => new MessageTemplateItem(
                t.Id, t.Title, t.Body, t.Language, t.Category.ToString(), t.CreatedAt))
            .ToListAsync();
        return Ok(items);
    }

    [HttpPost("templates")]
    public async Task<IActionResult> CreateTemplate([FromBody] CreateMessageTemplateRequest req)
    {
        var cId = GetConstituencyId() ?? 1;
        if (!Enum.TryParse<MessageCategory>(req.Category, out var cat)) cat = MessageCategory.VoterOutreach;
        _db.MessageTemplates.Add(new Domain.Entities.MessageTemplate
        {
            Title = req.Title.Trim(), Body = req.Body.Trim(),
            Language = req.Language, Category = cat,
            ConstituencyId = cId, CreatedByUserId = GetUserId(),
        });
        await _db.SaveChangesAsync();
        return Ok(new ApiResult(true, "Template saved."));
    }

    [HttpGet]
    public async Task<IActionResult> GetBroadcasts()
    {
        var cId = GetConstituencyId();
        var isSA = GetUserRole() == nameof(UserRole.SuperAdmin);
        var q = _db.MessageBroadcasts.Include(b => b.Template).AsNoTracking().AsQueryable();
        if (!isSA && cId.HasValue) q = q.Where(b => b.ConstituencyId == cId.Value);
        else if (!isSA) return Ok(new List<BroadcastItem>());

        var items = await q.OrderByDescending(b => b.CreatedAt)
            .Select(b => new BroadcastItem(
                b.Id, b.TemplateId, b.Template!.Title,
                b.TargetDescription, b.TotalTargeted, b.SentCount,
                b.Status.ToString(), b.ScheduledAt, b.SentAt,
                b.CreatedByName ?? "Unknown", b.CreatedAt))
            .ToListAsync();
        return Ok(items);
    }

    [HttpPost]
    public async Task<IActionResult> CreateBroadcast([FromBody] CreateBroadcastRequest req)
    {
        var cId = GetConstituencyId() ?? 1;
        _db.MessageBroadcasts.Add(new Domain.Entities.MessageBroadcast
        {
            TemplateId         = req.TemplateId,
            TargetDescription  = req.TargetDescription,
            ScheduledAt        = req.ScheduledAt,
            Status             = BroadcastStatus.Draft,
            ConstituencyId     = cId,
            CreatedByUserId    = GetUserId(),
            CreatedByName      = GetUserFullName(),
        });
        await _db.SaveChangesAsync();
        return Ok(new ApiResult(true, "Broadcast created."));
    }
}
