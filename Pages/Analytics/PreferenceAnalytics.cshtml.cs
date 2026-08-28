using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Nirvachak_AI.Domain.Entities;
using Nirvachak_AI.Domain.Enums;
using Nirvachak_AI.Infrastructure.Data;

namespace Nirvachak_AI.Pages.Analytics;

[Authorize(Roles = "Admin,CampaignManager,SuperAdmin,Candidate")]
public class PreferenceAnalyticsModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _userManager;

    public PreferenceAnalyticsModel(AppDbContext db, UserManager<AppUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    [BindProperty(SupportsGet = true)] public int? SelectedConstituencyId { get; set; }

    public List<Constituency> Constituencies { get; set; } = new();
    public bool IsAdmin { get; set; }
    public int TotalResponses { get; set; }

    // Candidate preference
    public List<PreferenceRow> CandidateRows { get; set; } = new();
    public int CandidateNoPreference { get; set; }

    // Party preference
    public List<PreferenceRow> PartyRows { get; set; } = new();
    public int PartyNoPreference { get; set; }

    // Cross-analysis
    public List<DemographicPreferenceRow> CandidateByCaste { get; set; } = new();
    public List<DemographicPreferenceRow> CandidateByReligion { get; set; } = new();
    public List<DemographicPreferenceRow> CandidateByAge { get; set; } = new();
    public List<BoothPreferenceRow> CandidateByBooth { get; set; } = new();
    public string? TicketRecommendation { get; set; }
    public string? TicketRecommendationReason { get; set; }

    public async Task OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        IsAdmin = user?.Role == UserRole.SuperAdmin;

        if (IsAdmin)
            Constituencies = await _db.Constituencies.OrderBy(c => c.Name).ToListAsync();

        int? cId = IsAdmin ? SelectedConstituencyId : user?.ConstituencyId;
        if (cId is null) return;

        // All profiles for this constituency with preferences loaded
        var profiles = await _db.VoterProfiles
            .Where(p => p.Voter != null && p.Voter.ConstituencyId == cId)
            .Include(p => p.PreferredCandidate)
            .Include(p => p.PreferredParty)
            .AsNoTracking()
            .ToListAsync();

        TotalResponses = profiles.Count;
        if (TotalResponses == 0) return;

        // ?? Candidate preference ??????????????????????????????
        var candidates = await _db.SurveyCandidates
            .Where(c => c.ConstituencyId == cId).OrderBy(c => c.Name).ToListAsync();

        CandidateNoPreference = profiles.Count(p => p.PreferredCandidateId == null);
        CandidateRows = candidates.Select(c => new PreferenceRow
        {
            Id      = c.Id,
            Name    = c.Name,
            SubText = c.PartyAffiliation,
            Count   = profiles.Count(p => p.PreferredCandidateId == c.Id),
            Total   = TotalResponses
        }).OrderByDescending(r => r.Count).ToList();

        // ?? Party preference ??????????????????????????????????
        var parties = await _db.SurveyParties
            .Where(p => p.ConstituencyId == cId).OrderBy(p => p.Name).ToListAsync();

        PartyNoPreference = profiles.Count(p => p.PreferredPartyId == null);
        PartyRows = parties.Select(p => new PreferenceRow
        {
            Id      = p.Id,
            Name    = p.Name,
            SubText = p.Symbol,
            Count   = profiles.Count(vp => vp.PreferredPartyId == p.Id),
            Total   = TotalResponses
        }).OrderByDescending(r => r.Count).ToList();

        // ?? Cross-analysis: Candidate × Demographic ???????????
        var withCandidate = profiles.Where(p => p.PreferredCandidateId != null).ToList();
        if (withCandidate.Any())
        {
            CandidateByCaste    = BuildCrossTab(withCandidate, candidates, p => p.CasteCategory);
            CandidateByReligion = BuildCrossTab(withCandidate, candidates, p => p.Religion);
            CandidateByAge      = BuildCrossTab(withCandidate, candidates, p => p.AgeBracket);
        }

        // ?? Booth-level candidate preference ?????????????????
        var voterIds  = profiles.Select(p => p.VoterId).ToList();
        var voterBooths = await _db.Voters
            .Where(v => voterIds.Contains(v.Id))
            .Select(v => new { v.Id, v.BoothNumber })
            .ToListAsync();

        var boothMap = voterBooths.ToDictionary(v => v.Id, v => v.BoothNumber);
        CandidateByBooth = profiles
            .Where(p => p.PreferredCandidateId != null && boothMap.ContainsKey(p.VoterId))
            .GroupBy(p => boothMap[p.VoterId])
            .Select(g =>
            {
                var topCandId = g.GroupBy(p => p.PreferredCandidateId)
                                 .OrderByDescending(x => x.Count())
                                 .First().Key;
                var topCand = candidates.FirstOrDefault(c => c.Id == topCandId);
                return new BoothPreferenceRow
                {
                    BoothNumber    = g.Key,
                    TotalResponses = g.Count(),
                    TopCandidate   = topCand?.Name ?? "Unknown",
                    TopCount       = g.Count(p => p.PreferredCandidateId == topCandId)
                };
            })
            .OrderBy(r => r.BoothNumber)
            .ToList();

        // ?? Ticket Recommendation ?????????????????????????????
        if (CandidateRows.Any() && CandidateRows[0].Count > 0)
        {
            var top = CandidateRows[0];
            TicketRecommendation = top.Name;
            TicketRecommendationReason =
                $"{top.Count} out of {TotalResponses} surveyed voters ({top.Pct:F1}%) prefer {top.Name}" +
                (top.SubText != null ? $" ({top.SubText})" : "") + ".";
        }
    }

    private static List<DemographicPreferenceRow> BuildCrossTab(
        List<VoterProfile> profiles,
        List<SurveyCandidate> candidates,
        Func<VoterProfile, string?> keySelector)
    {
        return profiles
            .Where(p => !string.IsNullOrEmpty(keySelector(p)))
            .GroupBy(p => keySelector(p)!)
            .Select(g => new DemographicPreferenceRow
            {
                Group  = g.Key,
                Total  = g.Count(),
                Counts = candidates.Select(c => new CandidateCount
                {
                    CandidateId   = c.Id,
                    CandidateName = c.Name,
                    Count         = g.Count(p => p.PreferredCandidateId == c.Id)
                }).OrderByDescending(x => x.Count).ToList()
            })
            .OrderByDescending(r => r.Total)
            .ToList();
    }
}

public class PreferenceRow
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? SubText { get; set; }
    public int Count { get; set; }
    public int Total { get; set; }
    public double Pct => Total > 0 ? Count * 100.0 / Total : 0;
}

public class DemographicPreferenceRow
{
    public string Group { get; set; } = "";
    public int Total { get; set; }
    public List<CandidateCount> Counts { get; set; } = new();
}

public class CandidateCount
{
    public int CandidateId { get; set; }
    public string CandidateName { get; set; } = "";
    public int Count { get; set; }
}

public class BoothPreferenceRow
{
    public int BoothNumber { get; set; }
    public int TotalResponses { get; set; }
    public string TopCandidate { get; set; } = "";
    public int TopCount { get; set; }
    public double TopPct => TotalResponses > 0 ? TopCount * 100.0 / TotalResponses : 0;
}
