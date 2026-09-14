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
public class BoothHeatMapController : ApiBaseController
{
    private readonly AppDbContext _db;
    public BoothHeatMapController(AppDbContext db) => _db = db;

    public record BoothHeatDto(
        int BoothNumber,
        string BoothName,
        string? WardNumber,
        string? AssignedAgentName,
        string? AssignedAgentPhone,
        int TotalVoters,
        int ContactedVoters,
        int FavourVoters,
        int AgainstVoters,
        int FloatingVoters,
        int NeutralVoters,
        int UnknownVoters,
        int VisitsThisWeek,
        double CoveragePercent,
        double FavourPercent,
        string HeatColor,   // "green" | "yellow" | "red"
        string HeatLabel);  // "Strong" | "Moderate" | "Weak"

    public record HeatMapResponse(
        int TotalVoters,
        int TotalContacted,
        int TotalFavour,
        int TotalSwing,
        int Green,
        int Yellow,
        int Red,
        List<BoothHeatDto> Booths);

    // GET /api/boothheatmap?sort=booth|coverage_asc|coverage_desc|favour_desc|heat
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string sort = "booth")
    {
        var cId = GetConstituencyId();
        if (!cId.HasValue) return Ok(new HeatMapResponse(0, 0, 0, 0, 0, 0, 0, new()));

        var role = GetUserRole();
        var currentUserId = GetUserId();

        var boothQuery = _db.Booths.Where(b => b.ConstituencyId == cId.Value);

        // Restrict field workers / booth agents to their assigned booths
        if (role == nameof(UserRole.FieldWorker) || role == nameof(UserRole.BoothAgent))
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
        if (booths.Count == 0)
            return Ok(new HeatMapResponse(0, 0, 0, 0, 0, 0, 0, new()));

        var boothNumbers = booths.Select(b => b.BoothNumber).ToHashSet();

        var voterQuery = _db.Voters.Where(v => !v.IsDeleted && v.ConstituencyId == cId.Value);

        var sentimentData = await voterQuery
            .Where(v => boothNumbers.Contains(v.BoothNumber))
            .GroupBy(v => new { v.BoothNumber, v.Sentiment })
            .Select(g => new { g.Key.BoothNumber, g.Key.Sentiment, Count = g.Count() })
            .ToListAsync();

        var contactedPerBooth = await voterQuery
            .Where(v => boothNumbers.Contains(v.BoothNumber) && v.LastContactedAt != null)
            .GroupBy(v => v.BoothNumber)
            .Select(g => new { BoothNumber = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.BoothNumber, x => x.Count);

        var weekStart = DateTime.UtcNow.Date.AddDays(-(int)DateTime.UtcNow.DayOfWeek);
        var weekVisits = await _db.DoorToDoorVisits
            .Where(v => v.VisitedAt >= weekStart)
            .Join(voterQuery.Where(v => boothNumbers.Contains(v.BoothNumber)),
                  visit => visit.VoterId,
                  voter => voter.Id,
                  (visit, voter) => new { voter.BoothNumber })
            .GroupBy(x => x.BoothNumber)
            .Select(g => new { BoothNumber = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.BoothNumber, x => x.Count);

        var rows = booths.Select(b =>
        {
            var s = sentimentData.Where(x => x.BoothNumber == b.BoothNumber).ToList();
            int Cnt(VoterSentiment v) => s.FirstOrDefault(x => x.Sentiment == v)?.Count ?? 0;
            var favour = Cnt(VoterSentiment.Favour);
            var against = Cnt(VoterSentiment.Against);
            var floating = Cnt(VoterSentiment.Floating);
            var neutral = Cnt(VoterSentiment.Neutral);
            var unknown = Cnt(VoterSentiment.Unknown);
            var total = b.TotalVoters > 0 ? b.TotalVoters : s.Sum(x => x.Count);
            var contacted = contactedPerBooth.GetValueOrDefault(b.BoothNumber, 0);
            var visitsWeek = weekVisits.GetValueOrDefault(b.BoothNumber, 0);
            var coverage = total > 0 ? Math.Round((double)contacted / total * 100, 1) : 0;
            var favourPct = total > 0 ? Math.Round((double)favour / total * 100, 1) : 0;

            string color, label;
            if (coverage >= 70) { color = "green"; label = "Strong"; }
            else if (coverage >= 30) { color = "yellow"; label = "Moderate"; }
            else { color = "red"; label = "Weak"; }

            return new BoothHeatDto(
                b.BoothNumber, b.BoothName, b.WardNumber,
                b.AssignedAgentName, b.AssignedAgentPhone,
                total, contacted, favour, against, floating, neutral, unknown,
                visitsWeek, coverage, favourPct, color, label);
        }).ToList();

        rows = sort switch
        {
            "coverage_asc"  => rows.OrderBy(r => r.CoveragePercent).ToList(),
            "coverage_desc" => rows.OrderByDescending(r => r.CoveragePercent).ToList(),
            "favour_desc"   => rows.OrderByDescending(r => r.FavourPercent).ToList(),
            "heat"          => rows.OrderBy(r => r.HeatLabel == "Weak" ? 0 : r.HeatLabel == "Moderate" ? 1 : 2).ToList(),
            _               => rows.OrderBy(r => r.BoothNumber).ToList()
        };

        var response = new HeatMapResponse(
            rows.Sum(r => r.TotalVoters),
            rows.Sum(r => r.ContactedVoters),
            rows.Sum(r => r.FavourVoters),
            rows.Sum(r => r.FloatingVoters + r.AgainstVoters),
            rows.Count(r => r.HeatColor == "green"),
            rows.Count(r => r.HeatColor == "yellow"),
            rows.Count(r => r.HeatColor == "red"),
            rows);

        return Ok(response);
    }
}
