using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Nirvachak_AI.Domain.Entities;
using Nirvachak_AI.Domain.Enums;
using Nirvachak_AI.Infrastructure.Data;

namespace Nirvachak_AI.Pages.Results;

[Authorize(Roles = "Admin,SuperAdmin,CampaignManager,Candidate")]
public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _userManager;
    public IndexModel(AppDbContext db, UserManager<AppUser> userManager) { _db = db; _userManager = userManager; }

    public List<ElectionResult> Results { get; set; } = new();
    public List<Constituency> Constituencies { get; set; } = new();
    public bool IsAdmin { get; set; }
    public int TotalCandidateVotes { get; set; }
    public int TotalCompetitor1Votes { get; set; }
    public int TotalCompetitor2Votes { get; set; }
    public string? Competitor1Name { get; set; }
    public string? Competitor2Name { get; set; }
    public bool IsLeading { get; set; }
    public int LeadMargin { get; set; }

    [BindProperty(SupportsGet = true)] public int? ConstituencyFilter { get; set; }
    [BindProperty(SupportsGet = true)] public int? RoundFilter { get; set; }
    [BindProperty] public ElectionResult NewResult { get; set; } = new();

    public async Task OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        IsAdmin = user?.Role == UserRole.Admin || user?.Role == UserRole.SuperAdmin;
        if (IsAdmin) Constituencies = await _db.Constituencies.OrderBy(c => c.Name).ToListAsync();

        int? cId = IsAdmin ? ConstituencyFilter : user?.ConstituencyId;

        var q = _db.ElectionResults.AsQueryable();
        if (cId.HasValue) q = q.Where(r => r.ConstituencyId == cId);
        if (RoundFilter.HasValue) q = q.Where(r => r.RoundNumber == RoundFilter);
        Results = await q.OrderBy(r => r.RoundNumber).ThenBy(r => r.BoothNumber).ToListAsync();

        if (Results.Any())
        {
            TotalCandidateVotes = Results.Sum(r => r.CandidateVotes);
            TotalCompetitor1Votes = Results.Sum(r => r.Competitor1Votes ?? 0);
            TotalCompetitor2Votes = Results.Sum(r => r.Competitor2Votes ?? 0);
            Competitor1Name = Results.FirstOrDefault(r => r.Competitor1Name != null)?.Competitor1Name;
            Competitor2Name = Results.FirstOrDefault(r => r.Competitor2Name != null)?.Competitor2Name;
            var maxCompetitor = Math.Max(TotalCompetitor1Votes, TotalCompetitor2Votes);
            IsLeading = TotalCandidateVotes > maxCompetitor;
            LeadMargin = Math.Abs(TotalCandidateVotes - maxCompetitor);
        }
        if (!IsAdmin && user?.ConstituencyId.HasValue == true) NewResult.ConstituencyId = user.ConstituencyId.Value;
    }

    public async Task<IActionResult> OnPostAddAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user?.Role != UserRole.Admin && user?.Role != UserRole.SuperAdmin)
            NewResult.ConstituencyId = user?.ConstituencyId ?? 1;
        NewResult.EnteredByUserId = user?.Id;
        NewResult.EnteredAt = DateTime.UtcNow;
        _db.ElectionResults.Add(NewResult);
        await _db.SaveChangesAsync();
        TempData["Message"] = $"Result for Booth {NewResult.BoothNumber} Round {NewResult.RoundNumber} saved.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var r = await _db.ElectionResults.FindAsync(id);
        if (r != null) { _db.ElectionResults.Remove(r); await _db.SaveChangesAsync(); }
        TempData["Message"] = "Result removed.";
        return RedirectToPage();
    }
}
