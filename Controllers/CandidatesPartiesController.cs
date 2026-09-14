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
public class CandidatesPartiesController : ApiBaseController
{
    private readonly AppDbContext _db;
    public CandidatesPartiesController(AppDbContext db) => _db = db;

    public record CandidateDto(int Id, string Name, string? PartyAffiliation, string? PhotoUrl, string? Notes, int DisplayOrder, bool IsActive);
    public record PartyDto(int Id, string Name, string? Symbol, string? Notes, bool IsActive);
    public record CatalogResponse(List<CandidateDto> Candidates, List<PartyDto> Parties);

    public record AddCandidateRequest(string Name, string? PartyAffiliation, string? Notes, int DisplayOrder);
    public record AddPartyRequest(string Name, string? Symbol, string? Notes);

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var cId = GetConstituencyId();
        if (!cId.HasValue) return Ok(new CatalogResponse(new(), new()));

        var candidates = await _db.SurveyCandidates
            .Where(c => c.ConstituencyId == cId.Value)
            .OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name)
            .Select(c => new CandidateDto(c.Id, c.Name, c.PartyAffiliation, c.PhotoUrl, c.Notes, c.DisplayOrder, c.IsActive))
            .ToListAsync();

        var parties = await _db.SurveyParties
            .Where(p => p.ConstituencyId == cId.Value)
            .OrderBy(p => p.Name)
            .Select(p => new PartyDto(p.Id, p.Name, p.Symbol, p.Notes, p.IsActive))
            .ToListAsync();

        return Ok(new CatalogResponse(candidates, parties));
    }

    [HttpPost("candidates")]
    public async Task<IActionResult> AddCandidate([FromBody] AddCandidateRequest req)
    {
        var cId = GetConstituencyId();
        if (!cId.HasValue) return BadRequest(new { error = "constituency not resolved" });
        if (string.IsNullOrWhiteSpace(req.Name)) return BadRequest(new { error = "Name required" });

        var c = new SurveyCandidate
        {
            Name = req.Name.Trim(),
            PartyAffiliation = req.PartyAffiliation?.Trim(),
            Notes = req.Notes?.Trim(),
            DisplayOrder = req.DisplayOrder,
            ConstituencyId = cId.Value,
            IsActive = true
        };
        _db.SurveyCandidates.Add(c);
        await _db.SaveChangesAsync();
        return Ok(new { id = c.Id });
    }

    [HttpPost("candidates/{id:int}/toggle")]
    public async Task<IActionResult> ToggleCandidate(int id)
    {
        var c = await FindCandidate(id);
        if (c == null) return NotFound();
        c.IsActive = !c.IsActive;
        await _db.SaveChangesAsync();
        return Ok(new { isActive = c.IsActive });
    }

    [HttpPost("candidates/{id:int}/order")]
    public async Task<IActionResult> SetOrder(int id, [FromBody] int order)
    {
        var c = await FindCandidate(id);
        if (c == null) return NotFound();
        c.DisplayOrder = order;
        await _db.SaveChangesAsync();
        return Ok();
    }

    [HttpDelete("candidates/{id:int}")]
    public async Task<IActionResult> DeleteCandidate(int id)
    {
        var c = await FindCandidate(id);
        if (c == null) return NotFound();
        _db.SurveyCandidates.Remove(c);
        await _db.SaveChangesAsync();
        return Ok();
    }

    [HttpPost("parties")]
    public async Task<IActionResult> AddParty([FromBody] AddPartyRequest req)
    {
        var cId = GetConstituencyId();
        if (!cId.HasValue) return BadRequest(new { error = "constituency not resolved" });
        if (string.IsNullOrWhiteSpace(req.Name)) return BadRequest(new { error = "Name required" });

        var p = new SurveyParty
        {
            Name = req.Name.Trim(),
            Symbol = req.Symbol?.Trim(),
            Notes = req.Notes?.Trim(),
            ConstituencyId = cId.Value,
            IsActive = true
        };
        _db.SurveyParties.Add(p);
        await _db.SaveChangesAsync();
        return Ok(new { id = p.Id });
    }

    [HttpPost("parties/{id:int}/toggle")]
    public async Task<IActionResult> ToggleParty(int id)
    {
        var p = await FindParty(id);
        if (p == null) return NotFound();
        p.IsActive = !p.IsActive;
        await _db.SaveChangesAsync();
        return Ok(new { isActive = p.IsActive });
    }

    [HttpDelete("parties/{id:int}")]
    public async Task<IActionResult> DeleteParty(int id)
    {
        var p = await FindParty(id);
        if (p == null) return NotFound();
        _db.SurveyParties.Remove(p);
        await _db.SaveChangesAsync();
        return Ok();
    }

    private async Task<SurveyCandidate?> FindCandidate(int id)
    {
        var cId = GetConstituencyId();
        var c = await _db.SurveyCandidates.FindAsync(id);
        if (c == null) return null;
        if (cId.HasValue && c.ConstituencyId != cId.Value) return null;
        return c;
    }
    private async Task<SurveyParty?> FindParty(int id)
    {
        var cId = GetConstituencyId();
        var p = await _db.SurveyParties.FindAsync(id);
        if (p == null) return null;
        if (cId.HasValue && p.ConstituencyId != cId.Value) return null;
        return p;
    }
}
