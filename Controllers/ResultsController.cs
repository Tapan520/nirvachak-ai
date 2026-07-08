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
public class ResultsController : ApiBaseController
{
    private readonly AppDbContext _db;
    public ResultsController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetResults([FromQuery] int? round)
    {
        var cId  = GetConstituencyId();
        var isSA = GetUserRole() == nameof(UserRole.SuperAdmin);

        var q = _db.ElectionResults.AsNoTracking().AsQueryable();
        if (!isSA && cId.HasValue) q = q.Where(r => r.ConstituencyId == cId.Value);
        else if (!isSA) return Ok(new ElectionResultSummary(0, 0, 0, null, null, false, 0, new()));
        if (round.HasValue) q = q.Where(r => r.RoundNumber == round.Value);

        var results = await q.OrderBy(r => r.RoundNumber).ThenBy(r => r.BoothNumber).ToListAsync();
        var items = results.Select(r => new ElectionResultItem(
            r.Id, r.BoothNumber, r.RoundNumber,
            r.CandidateVotes, r.Competitor1Votes, r.Competitor1Name,
            r.Competitor2Votes, r.Competitor2Name,
            r.TotalVotesCast, r.IsFinal, r.EnteredAt)).ToList();

        var totalCand  = results.Sum(r => r.CandidateVotes);
        var totalComp1 = results.Sum(r => r.Competitor1Votes ?? 0);
        var totalComp2 = results.Sum(r => r.Competitor2Votes ?? 0);
        var comp1Name  = results.FirstOrDefault(r => r.Competitor1Name != null)?.Competitor1Name;
        var comp2Name  = results.FirstOrDefault(r => r.Competitor2Name != null)?.Competitor2Name;
        var maxComp    = Math.Max(totalComp1, totalComp2);
        var isLeading  = totalCand > maxComp;
        var margin     = Math.Abs(totalCand - maxComp);

        return Ok(new ElectionResultSummary(
            totalCand, totalComp1, totalComp2, comp1Name, comp2Name,
            isLeading, margin, items));
    }

    [HttpPost]
    public async Task<IActionResult> AddResult([FromBody] CreateElectionResultRequest req)
    {
        var cId  = GetConstituencyId() ?? 1;
        _db.ElectionResults.Add(new Domain.Entities.ElectionResult
        {
            BoothNumber       = req.BoothNumber,
            RoundNumber       = req.RoundNumber,
            CandidateVotes    = req.CandidateVotes,
            Competitor1Votes  = req.Competitor1Votes,
            Competitor1Name   = req.Competitor1Name,
            Competitor2Votes  = req.Competitor2Votes,
            Competitor2Name   = req.Competitor2Name,
            TotalVotesCast    = req.TotalVotesCast,
            IsFinal           = req.IsFinal,
            ConstituencyId    = cId,
            EnteredByUserId   = GetUserId(),
        });
        await _db.SaveChangesAsync();
        return Ok(new ApiResult(true, "Result saved."));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> Delete(int id)
    {
        var r = await _db.ElectionResults.FindAsync(id);
        if (r is null) return NotFound();
        _db.ElectionResults.Remove(r);
        await _db.SaveChangesAsync();
        return Ok(new ApiResult(true, "Deleted."));
    }
}
