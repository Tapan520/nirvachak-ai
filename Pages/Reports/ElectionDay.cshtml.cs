using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Nirvachak_AI.Domain.Entities;
using Nirvachak_AI.Domain.Enums;
using Nirvachak_AI.Infrastructure.Data;
using Nirvachak_AI.Infrastructure.Services;

namespace Nirvachak_AI.Pages.Reports;

[Authorize(Roles = "Admin,SuperAdmin,CampaignManager,Candidate")]
public class ElectionDayModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _userManager;
    private readonly ElectionDayService _electionDayService;

    public ElectionDayModel(AppDbContext db, UserManager<AppUser> userManager, ElectionDayService electionDayService)
    {
        _db = db;
        _userManager = userManager;
        _electionDayService = electionDayService;
    }

    [BindProperty(SupportsGet = true)]
    public int? ConstituencyFilter { get; set; }

    public string ConstituencyName   { get; set; } = "";
    public int    TotalVoters        { get; set; }
    public int    TotalVoted         { get; set; }
    public double OverallPercent     { get; set; }
    public int    FavourNotVoted     { get; set; }
    public DateTime GeneratedAt      { get; set; }

    public List<BoothTurnoutDto>     BoothTurnout       { get; set; } = new();
    public List<Voter>               ChaseList          { get; set; } = new();
    public List<Constituency>        Constituencies     { get; set; } = new();
    public bool                      IsAdmin            { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        IsAdmin = user?.Role == UserRole.SuperAdmin;

        if (IsAdmin)
            Constituencies = await _db.Constituencies.OrderBy(c => c.Name).ToListAsync();

        int cId = IsAdmin
            ? (ConstituencyFilter ?? Constituencies.FirstOrDefault()?.Id ?? 0)
            : (user?.ConstituencyId ?? 0);

        if (cId == 0) return Page();

        var constituency = await _db.Constituencies.FindAsync(cId);
        ConstituencyName = constituency?.Name ?? "";
        GeneratedAt      = DateTime.Now;

        BoothTurnout = await _electionDayService.GetLiveTurnoutAsync(cId);
        var (total, voted, pct) = await _electionDayService.GetConstituencyTurnoutAsync(cId);
        TotalVoters    = total;
        TotalVoted     = voted;
        OverallPercent = pct;

        ChaseList = await _db.Voters
            .Where(v => v.ConstituencyId == cId && !v.IsDeleted
                     && v.ElectionDayStatus == ElectionDayStatus.NotVoted
                     && v.Sentiment == VoterSentiment.Favour)
            .OrderBy(v => v.BoothNumber).ThenBy(v => v.SerialNumber)
            .ToListAsync();

        FavourNotVoted = ChaseList.Count;
        return Page();
    }
}
