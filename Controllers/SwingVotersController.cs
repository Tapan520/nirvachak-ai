using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nirvachak_AI.Domain.Enums;
using Nirvachak_AI.Infrastructure.Data;

namespace Nirvachak_AI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class SwingVotersController : ApiBaseController
{
    private readonly AppDbContext _db;
    public SwingVotersController(AppDbContext db) => _db = db;

    public record SwingVoterDto(
        int Id,
        string VoterId,
        string Name,
        string? MobileNumber,
        int BoothNumber,
        string? WardNumber,
        string? PannaNumber,
        string CurrentSentiment,
        int FavourVisitCount,
        int TotalVisitCount,
        DateTime? LastVisitedAt,
        string? LastWorkerName);

    public record SwingVotersResponse(int Total, int Critical, int Floating, List<SwingVoterDto> Items);

    // GET /api/swingvoters?sentiment=Against|Floating&booth=1&ward=2
    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] string? sentiment = null,
        [FromQuery] int? booth = null,
        [FromQuery] string? ward = null)
    {
        var cId = GetConstituencyId();
        if (!cId.HasValue) return Ok(new SwingVotersResponse(0, 0, 0, new()));

        var q = _db.Voters.Where(v =>
            !v.IsDeleted &&
            v.ConstituencyId == cId.Value &&
            (v.Sentiment == VoterSentiment.Floating || v.Sentiment == VoterSentiment.Against));

        if (booth.HasValue) q = q.Where(v => v.BoothNumber == booth.Value);
        if (!string.IsNullOrWhiteSpace(ward)) q = q.Where(v => v.WardNumber == ward);
        if (sentiment == "Against") q = q.Where(v => v.Sentiment == VoterSentiment.Against);
        else if (sentiment == "Floating") q = q.Where(v => v.Sentiment == VoterSentiment.Floating);

        var candidateIds = await q.Select(v => v.Id).ToListAsync();
        if (candidateIds.Count == 0)
            return Ok(new SwingVotersResponse(0, 0, 0, new()));

        var visits = await _db.DoorToDoorVisits
            .Where(v => candidateIds.Contains(v.VoterId))
            .OrderByDescending(v => v.VisitedAt)
            .Select(v => new { v.VoterId, v.SentimentAfterVisit, v.VisitedAt, v.WorkerName })
            .ToListAsync();

        var trueSwingIds = visits
            .Where(v => v.SentimentAfterVisit == VoterSentiment.Favour)
            .Select(v => v.VoterId)
            .ToHashSet();

        if (trueSwingIds.Count == 0)
            return Ok(new SwingVotersResponse(0, 0, 0, new()));

        var voters = await q.Where(v => trueSwingIds.Contains(v.Id))
            .OrderBy(v => v.BoothNumber).ThenBy(v => v.Name)
            .Take(500)
            .ToListAsync();

        var items = voters.Select(v =>
        {
            var vv = visits.Where(x => x.VoterId == v.Id).ToList();
            var latest = vv.FirstOrDefault();
            var favourCount = vv.Count(x => x.SentimentAfterVisit == VoterSentiment.Favour);
            return new SwingVoterDto(
                v.Id, v.VoterId, v.Name, v.MobileNumber,
                v.BoothNumber, v.WardNumber, v.PannaNumber,
                v.Sentiment.ToString(),
                favourCount, vv.Count,
                latest?.VisitedAt, latest?.WorkerName);
        }).ToList();

        var total = items.Count;
        var critical = items.Count(i => i.CurrentSentiment == "Against");
        var floating = items.Count(i => i.CurrentSentiment == "Floating");

        return Ok(new SwingVotersResponse(total, critical, floating, items));
    }
}
