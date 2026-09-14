using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nirvachak_AI.Infrastructure.Data;

namespace Nirvachak_AI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin,SuperAdmin,CampaignManager,Candidate")]
public class PreferenceAnalyticsController : ApiBaseController
{
    private readonly AppDbContext _db;
    public PreferenceAnalyticsController(AppDbContext db) => _db = db;

    public record PreferenceRowDto(int Id, string Name, string? SubText, int Count, double Percent);
    public record CrossTabRowDto(string Group, int Total, List<(string Candidate, int Count)> TopCandidates);
    public record BoothPreferenceDto(int BoothNumber, int TotalResponses, string TopCandidate, int TopCount, double TopPct);

    public record PreferenceResponse(
        int TotalResponses,
        List<PreferenceRowDto> Candidates,
        int CandidateNoPreference,
        List<PreferenceRowDto> Parties,
        int PartyNoPreference,
        List<BoothPreferenceDto> ByBooth,
        string? TicketRecommendation,
        string? TicketRecommendationReason);

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var cId = GetConstituencyId();
        if (!cId.HasValue)
            return Ok(new PreferenceResponse(0, new(), 0, new(), 0, new(), null, null));

        var profiles = await _db.VoterProfiles
            .Where(p => p.Voter != null && p.Voter.ConstituencyId == cId.Value)
            .Select(p => new { p.VoterId, p.PreferredCandidateId, p.PreferredPartyId })
            .ToListAsync();

        var total = profiles.Count;
        if (total == 0)
            return Ok(new PreferenceResponse(0, new(), 0, new(), 0, new(), null, null));

        var candidates = await _db.SurveyCandidates
            .Where(c => c.ConstituencyId == cId.Value)
            .OrderBy(c => c.Name).ToListAsync();
        var parties = await _db.SurveyParties
            .Where(p => p.ConstituencyId == cId.Value)
            .OrderBy(p => p.Name).ToListAsync();

        var candidateRows = candidates.Select(c => new PreferenceRowDto(
                c.Id, c.Name, c.PartyAffiliation,
                profiles.Count(p => p.PreferredCandidateId == c.Id),
                total > 0 ? Math.Round(profiles.Count(p => p.PreferredCandidateId == c.Id) * 100.0 / total, 1) : 0))
            .OrderByDescending(r => r.Count).ToList();
        var partyRows = parties.Select(p => new PreferenceRowDto(
                p.Id, p.Name, p.Symbol,
                profiles.Count(vp => vp.PreferredPartyId == p.Id),
                total > 0 ? Math.Round(profiles.Count(vp => vp.PreferredPartyId == p.Id) * 100.0 / total, 1) : 0))
            .OrderByDescending(r => r.Count).ToList();

        var voterIds = profiles.Select(p => p.VoterId).ToList();
        var voterBooths = await _db.Voters
            .Where(v => voterIds.Contains(v.Id))
            .Select(v => new { v.Id, v.BoothNumber })
            .ToListAsync();
        var boothMap = voterBooths.ToDictionary(v => v.Id, v => v.BoothNumber);

        var byBooth = profiles
            .Where(p => p.PreferredCandidateId != null && boothMap.ContainsKey(p.VoterId))
            .GroupBy(p => boothMap[p.VoterId])
            .Select(g =>
            {
                var topCandId = g.GroupBy(p => p.PreferredCandidateId)
                                 .OrderByDescending(x => x.Count()).First().Key;
                var topCand = candidates.FirstOrDefault(c => c.Id == topCandId);
                var topCount = g.Count(p => p.PreferredCandidateId == topCandId);
                return new BoothPreferenceDto(
                    g.Key, g.Count(),
                    topCand?.Name ?? "Unknown",
                    topCount,
                    g.Count() > 0 ? Math.Round(topCount * 100.0 / g.Count(), 1) : 0);
            })
            .OrderBy(r => r.BoothNumber).ToList();

        string? recName = null, recReason = null;
        if (candidateRows.Count > 0 && candidateRows[0].Count > 0)
        {
            var top = candidateRows[0];
            recName = top.Name;
            recReason = $"{top.Count} out of {total} surveyed voters ({top.Percent:F1}%) prefer {top.Name}" +
                (top.SubText != null ? $" ({top.SubText})" : "") + ".";
        }

        return Ok(new PreferenceResponse(
            total, candidateRows,
            profiles.Count(p => p.PreferredCandidateId == null),
            partyRows,
            profiles.Count(p => p.PreferredPartyId == null),
            byBooth, recName, recReason));
    }
}
