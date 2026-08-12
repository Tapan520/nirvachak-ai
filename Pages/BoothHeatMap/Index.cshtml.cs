using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Nirvachak_AI.Domain.Entities;
using Nirvachak_AI.Domain.Enums;
using Nirvachak_AI.Infrastructure.Data;

namespace Nirvachak_AI.Pages.BoothHeatMap;

[Authorize(Roles = "Admin,SuperAdmin,CampaignManager,Candidate,FieldWorker,BoothAgent")]
public class IndexModel : PageModel
{
    private readonly AppDbContext         _db;
    private readonly UserManager<AppUser> _userManager;

    public IndexModel(AppDbContext db, UserManager<AppUser> userManager)
    {
        _db          = db;
        _userManager = userManager;
    }

    // ?? Filters ???????????????????????????????????????????????????
    [BindProperty(SupportsGet = true)] public int?    SelectedConstituencyId { get; set; }
    [BindProperty(SupportsGet = true)] public string? SortBy                 { get; set; } = "booth";

    // ?? Output ????????????????????????????????????????????????????
    public List<BoothHeatRow>  Booths         { get; set; } = new();
    public List<Constituency>  Constituencies { get; set; } = new();
    public bool                IsAdmin        { get; set; }

    // ?? Summary totals ????????????????????????????????????????????
    public int TotalVoters       { get; set; }
    public int TotalContacted    { get; set; }
    public int TotalFavour       { get; set; }
    public int TotalSwing        { get; set; }   // Floating + Against
    public int RedBooths         { get; set; }   // coverage < 30%
    public int YellowBooths      { get; set; }   // 30–69%
    public int GreenBooths       { get; set; }   // ? 70%

    public record BoothHeatRow(
        int     BoothNumber,
        string  BoothName,
        string? WardNumber,
        string? AssignedAgentName,
        string? AssignedAgentPhone,
        int     TotalVoters,
        int     ContactedVoters,
        int     FavourVoters,
        int     AgainstVoters,
        int     FloatingVoters,
        int     NeutralVoters,
        int     UnknownVoters,
        int     VisitsThisWeek,
        double  CoveragePercent,
        double  FavourPercent,
        string  HeatColor,     // "green" | "yellow" | "red"
        string  HeatLabel,     // "Strong" | "Moderate" | "Weak"
        string  HeatIcon);     // Bootstrap icon class

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToPage("/Account/Login");

        IsAdmin = user.Role == UserRole.SuperAdmin;
        if (IsAdmin)
            Constituencies = await _db.Constituencies.OrderBy(c => c.Name).ToListAsync();

        var cId = IsAdmin
            ? (SelectedConstituencyId ?? user.ConstituencyId)
            : user.ConstituencyId;

        // ?? Load booths ????????????????????????????????????????????
        IQueryable<Booth> boothQuery = _db.Booths;
        if (cId.HasValue)
            boothQuery = boothQuery.Where(b => b.ConstituencyId == cId.Value);

        // Restrict field workers to their assigned booths
        if (user.Role is UserRole.FieldWorker or UserRole.BoothAgent)
        {
            var assigned = (user.AssignedBoothNumbers ?? "")
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => int.TryParse(s.Trim(), out var n) ? (int?)n : null)
                .Where(n => n.HasValue).Select(n => n!.Value).ToHashSet();
            if (assigned.Any())
                boothQuery = boothQuery.Where(b => assigned.Contains(b.BoothNumber));
        }

        var booths = await boothQuery.OrderBy(b => b.BoothNumber).ToListAsync();
        if (!booths.Any()) return Page();

        var boothNumbers = booths.Select(b => b.BoothNumber).ToHashSet();

        // ?? Load voter sentiment counts per booth ??????????????????
        IQueryable<Voter> voterQuery = _db.Voters.Where(v => !v.IsDeleted);
        if (cId.HasValue) voterQuery = voterQuery.Where(v => v.ConstituencyId == cId.Value);

        var sentimentData = await voterQuery
            .Where(v => boothNumbers.Contains(v.BoothNumber))
            .GroupBy(v => new { v.BoothNumber, v.Sentiment })
            .Select(g => new { g.Key.BoothNumber, g.Key.Sentiment, Count = g.Count() })
            .ToListAsync();

        // ?? Contacted voters per booth (have LastContactedAt) ??????
        var contactedPerBooth = await voterQuery
            .Where(v => boothNumbers.Contains(v.BoothNumber) && v.LastContactedAt != null)
            .GroupBy(v => v.BoothNumber)
            .Select(g => new { BoothNumber = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.BoothNumber, x => x.Count);

        // ?? Visits this week ???????????????????????????????????????
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

        // ?? Build rows ?????????????????????????????????????????????
        Booths = booths.Select(b =>
        {
            var bSentiments  = sentimentData.Where(s => s.BoothNumber == b.BoothNumber).ToList();
            var favour       = bSentiments.FirstOrDefault(s => s.Sentiment == VoterSentiment.Favour)?.Count  ?? 0;
            var against      = bSentiments.FirstOrDefault(s => s.Sentiment == VoterSentiment.Against)?.Count ?? 0;
            var floating     = bSentiments.FirstOrDefault(s => s.Sentiment == VoterSentiment.Floating)?.Count?? 0;
            var neutral      = bSentiments.FirstOrDefault(s => s.Sentiment == VoterSentiment.Neutral)?.Count ?? 0;
            var unknown      = bSentiments.FirstOrDefault(s => s.Sentiment == VoterSentiment.Unknown)?.Count ?? 0;
            var total        = b.TotalVoters > 0 ? b.TotalVoters : bSentiments.Sum(s => s.Count);
            var contacted    = contactedPerBooth.GetValueOrDefault(b.BoothNumber, 0);
            var visitsWeek   = weekVisits.GetValueOrDefault(b.BoothNumber, 0);

            var coverage     = total > 0 ? Math.Round((double)contacted / total * 100, 1) : 0;
            var favourPct    = total > 0 ? Math.Round((double)favour    / total * 100, 1) : 0;

            // Heat classification
            string heat, heatLabel, heatIcon;
            if (coverage >= 70)
            {
                heat      = "success";
                heatLabel = "Strong";
                heatIcon  = "bi-check-circle-fill";
            }
            else if (coverage >= 30)
            {
                heat      = "warning";
                heatLabel = "Moderate";
                heatIcon  = "bi-exclamation-circle-fill";
            }
            else
            {
                heat      = "danger";
                heatLabel = "Weak";
                heatIcon  = "bi-x-circle-fill";
            }

            return new BoothHeatRow(
                b.BoothNumber, b.BoothName, b.WardNumber,
                b.AssignedAgentName, b.AssignedAgentPhone,
                total, contacted, favour, against, floating, neutral, unknown,
                visitsWeek, coverage, favourPct,
                heat, heatLabel, heatIcon);
        }).ToList();

        // ?? Apply sort ?????????????????????????????????????????????
        Booths = SortBy switch
        {
            "coverage_asc"  => Booths.OrderBy(b => b.CoveragePercent).ToList(),
            "coverage_desc" => Booths.OrderByDescending(b => b.CoveragePercent).ToList(),
            "favour_desc"   => Booths.OrderByDescending(b => b.FavourPercent).ToList(),
            "heat"          => Booths.OrderBy(b => b.HeatLabel == "Weak" ? 0 : b.HeatLabel == "Moderate" ? 1 : 2).ToList(),
            _               => Booths.OrderBy(b => b.BoothNumber).ToList()
        };

        // ?? Summary ????????????????????????????????????????????????
        TotalVoters    = Booths.Sum(b => b.TotalVoters);
        TotalContacted = Booths.Sum(b => b.ContactedVoters);
        TotalFavour    = Booths.Sum(b => b.FavourVoters);
        TotalSwing     = Booths.Sum(b => b.FloatingVoters + b.AgainstVoters);
        RedBooths      = Booths.Count(b => b.HeatColor == "danger");
        YellowBooths   = Booths.Count(b => b.HeatColor == "warning");
        GreenBooths    = Booths.Count(b => b.HeatColor == "success");

        return Page();
    }
}
