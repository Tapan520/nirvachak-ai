using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Nirvachak_AI.Domain.Entities;
using Nirvachak_AI.Domain.Enums;
using Nirvachak_AI.Infrastructure.Data;

namespace Nirvachak_AI.Pages.Competitor;

[Authorize(Roles = "Admin,CampaignManager,Candidate")]
public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _userManager;

    public IndexModel(AppDbContext db, UserManager<AppUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public List<CompetitorActivity> Activities { get; set; } = new();
    public Dictionary<string, int> ActivityByType { get; set; } = new();
    public Dictionary<string, int> ThreatSummary { get; set; } = new();
    public Dictionary<string, int> CrowdByCompetitor { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? FilterCompetitor { get; set; }

    [BindProperty(SupportsGet = true)]
    public CompetitorThreatLevel? FilterThreat { get; set; }

    public List<string> KnownCompetitors { get; set; } = new();

    public async Task OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        int? cId = user?.ConstituencyId;

        IQueryable<CompetitorActivity> q = _db.CompetitorActivities.AsNoTracking();
        if (cId.HasValue)
            q = q.Where(a => a.ConstituencyId == cId.Value);
        if (!string.IsNullOrEmpty(FilterCompetitor))
            q = q.Where(a => a.CompetitorName == FilterCompetitor);
        if (FilterThreat.HasValue)
            q = q.Where(a => a.ThreatLevel == FilterThreat.Value);

        Activities = await q.OrderByDescending(a => a.ActivityDate).ToListAsync();

        KnownCompetitors = Activities.Select(a => a.CompetitorName).Distinct().OrderBy(n => n).ToList();

        ActivityByType = Activities
            .GroupBy(a => a.ActivityType.ToString())
            .ToDictionary(g => g.Key, g => g.Count());

        ThreatSummary = Activities
            .GroupBy(a => a.ThreatLevel.ToString())
            .ToDictionary(g => g.Key, g => g.Count());

        CrowdByCompetitor = Activities
            .Where(a => a.EstimatedCrowd.HasValue)
            .GroupBy(a => a.CompetitorName)
            .ToDictionary(g => g.Key, g => g.Sum(a => a.EstimatedCrowd!.Value));
    }
}
