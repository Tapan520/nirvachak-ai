using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nirvachak_AI.Domain.Entities;
using Nirvachak_AI.Domain.Enums;
using Nirvachak_AI.Infrastructure.Data;

namespace Nirvachak_AI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class BoothChecklistController : ApiBaseController
{
    private readonly AppDbContext _db;
    public BoothChecklistController(AppDbContext db) => _db = db;

    public record ChecklistItemDto(
        int BoothNumber,
        string BoothName,
        string? Address,
        string? WardNumber,
        string? AssignedAgentName,
        string? AssignedAgentPhone,
        bool AgentPresent,
        bool BannerDisplayed,
        bool VoterListPrinted,
        bool TransportArranged,
        bool PhoneCharged,
        bool BoothClean,
        string? Notes,
        string? SubmittedByName,
        DateTime? SubmittedAt,
        DateTime? UpdatedAt,
        bool IsReady);

    public record ChecklistResponse(int Total, int Ready, List<ChecklistItemDto> Items);

    public record SaveChecklistRequest(
        int BoothNumber,
        bool AgentPresent,
        bool BannerDisplayed,
        bool VoterListPrinted,
        bool TransportArranged,
        bool PhoneCharged,
        bool BoothClean,
        string? Notes);

    // GET /api/boothchecklist
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var cId = GetConstituencyId();
        if (!cId.HasValue) return Ok(new ChecklistResponse(0, 0, new()));

        var role = GetUserRole();
        var currentUserId = GetUserId();

        var boothQuery = _db.Booths.Where(b => b.ConstituencyId == cId.Value);

        if (role == nameof(UserRole.FieldWorker) ||
            role == nameof(UserRole.BoothAgent)  ||
            role == nameof(UserRole.VoterManager))
        {
            var me = await _db.Users
                .Where(u => u.Id == currentUserId)
                .Select(u => u.AssignedBoothNumbers)
                .FirstOrDefaultAsync();
            var assigned = (me ?? "")
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => int.TryParse(s.Trim(), out var n) ? (int?)n : null)
                .Where(n => n.HasValue).Select(n => n!.Value).ToHashSet();
            if (assigned.Count > 0)
                boothQuery = boothQuery.Where(b => assigned.Contains(b.BoothNumber));
        }

        var booths = await boothQuery.OrderBy(b => b.BoothNumber).ToListAsync();
        if (booths.Count == 0) return Ok(new ChecklistResponse(0, 0, new()));

        var checklists = await _db.BoothChecklists
            .Where(c => c.ConstituencyId == cId.Value)
            .ToListAsync();
        var byBooth = checklists.ToDictionary(c => c.BoothNumber);

        var items = booths.Select(b =>
        {
            byBooth.TryGetValue(b.BoothNumber, out var c);
            var isReady = c != null &&
                c.AgentPresent && c.BannerDisplayed && c.VoterListPrinted &&
                c.TransportArranged && c.PhoneCharged && c.BoothClean;
            return new ChecklistItemDto(
                b.BoothNumber, b.BoothName, b.Address, b.WardNumber,
                b.AssignedAgentName, b.AssignedAgentPhone,
                c?.AgentPresent ?? false,
                c?.BannerDisplayed ?? false,
                c?.VoterListPrinted ?? false,
                c?.TransportArranged ?? false,
                c?.PhoneCharged ?? false,
                c?.BoothClean ?? false,
                c?.Notes,
                c?.SubmittedByName,
                c?.SubmittedAt,
                c?.UpdatedAt,
                isReady);
        }).ToList();

        return Ok(new ChecklistResponse(items.Count, items.Count(i => i.IsReady), items));
    }

    // POST /api/boothchecklist
    [HttpPost]
    public async Task<IActionResult> Save([FromBody] SaveChecklistRequest req)
    {
        var cId = GetConstituencyId();
        if (!cId.HasValue) return BadRequest(new { error = "constituency not resolved" });

        var existing = await _db.BoothChecklists
            .FirstOrDefaultAsync(c => c.ConstituencyId == cId.Value && c.BoothNumber == req.BoothNumber);

        if (existing == null)
        {
            _db.BoothChecklists.Add(new BoothChecklist
            {
                BoothNumber       = req.BoothNumber,
                ConstituencyId    = cId.Value,
                AgentPresent      = req.AgentPresent,
                BannerDisplayed   = req.BannerDisplayed,
                VoterListPrinted  = req.VoterListPrinted,
                TransportArranged = req.TransportArranged,
                PhoneCharged      = req.PhoneCharged,
                BoothClean        = req.BoothClean,
                Notes             = req.Notes,
                SubmittedByUserId = GetUserId(),
                SubmittedByName   = GetUserFullName(),
                SubmittedAt       = DateTime.UtcNow
            });
        }
        else
        {
            existing.AgentPresent      = req.AgentPresent;
            existing.BannerDisplayed   = req.BannerDisplayed;
            existing.VoterListPrinted  = req.VoterListPrinted;
            existing.TransportArranged = req.TransportArranged;
            existing.PhoneCharged      = req.PhoneCharged;
            existing.BoothClean        = req.BoothClean;
            existing.Notes             = req.Notes;
            existing.SubmittedByUserId = GetUserId();
            existing.SubmittedByName   = GetUserFullName();
            existing.UpdatedAt         = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        return Ok(new { saved = true });
    }
}
