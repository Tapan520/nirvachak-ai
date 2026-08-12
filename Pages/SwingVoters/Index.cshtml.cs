using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Nirvachak_AI.Domain.Entities;
using Nirvachak_AI.Domain.Enums;
using Nirvachak_AI.Infrastructure.Data;

namespace Nirvachak_AI.Pages.SwingVoters;

[Authorize(Roles = "Admin,SuperAdmin,CampaignManager,Candidate")]
public class IndexModel : PageModel
{
    private readonly AppDbContext        _db;
    private readonly UserManager<AppUser> _userManager;

    public IndexModel(AppDbContext db, UserManager<AppUser> userManager)
    {
        _db          = db;
        _userManager = userManager;
    }

    // ?? Filters ???????????????????????????????????????????????????
    [BindProperty(SupportsGet = true)] public int?    FilterBooth { get; set; }
    [BindProperty(SupportsGet = true)] public string? FilterWard  { get; set; }
    [BindProperty(SupportsGet = true)] public string? FilterSentiment { get; set; }
    [BindProperty(SupportsGet = true)] public int?    SelectedConstituencyId { get; set; }

    // ?? Output ????????????????????????????????????????????????????
    public List<SwingVoterRow>  SwingVoters      { get; set; } = new();
    public List<int>            AvailableBooths  { get; set; } = new();
    public List<string>         AvailableWards   { get; set; } = new();
    public List<Constituency>   Constituencies   { get; set; } = new();
    public bool                 IsAdmin          { get; set; }
    public int                  TotalSwing       { get; set; }
    public int                  CriticalSwing    { get; set; }  // swung to Against
    public int                  FloatingSwing    { get; set; }  // swung to Floating
    public string               SurveyBaseUrl    { get; set; } = string.Empty;

    public record SwingVoterRow(
        int     Id,
        string  VoterId,
        string  Name,
        string? MobileNumber,
        int     BoothNumber,
        string? WardNumber,
        string? PannaNumber,
        VoterSentiment CurrentSentiment,
        int     FavourVisitCount,    // how many times marked Favour before
        int     TotalVisitCount,
        DateTime? LastVisitedAt,
        string? LastWorkerName);

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

        // Build survey base URL for WhatsApp share links
        SurveyBaseUrl = $"{Request.Scheme}://{Request.Host}/Survey";

        // ?? Identify swing voters ??????????????????????????????????
        // A swing voter = currently Floating or Against
        //                 BUT has at least one visit logged as Favour in the past
        IQueryable<Voter> voterQ = _db.Voters.Where(v =>
            !v.IsDeleted &&
            (v.Sentiment == VoterSentiment.Floating || v.Sentiment == VoterSentiment.Against));

        if (cId.HasValue)
            voterQ = voterQ.Where(v => v.ConstituencyId == cId.Value);

        if (FilterBooth.HasValue)
            voterQ = voterQ.Where(v => v.BoothNumber == FilterBooth.Value);

        if (!string.IsNullOrEmpty(FilterWard))
            voterQ = voterQ.Where(v => v.WardNumber == FilterWard);

        if (FilterSentiment == "Against")
            voterQ = voterQ.Where(v => v.Sentiment == VoterSentiment.Against);
        else if (FilterSentiment == "Floating")
            voterQ = voterQ.Where(v => v.Sentiment == VoterSentiment.Floating);

        // Populate filter dropdowns (before further narrowing)
        var allSwingBase = _db.Voters.Where(v =>
            !v.IsDeleted &&
            (v.Sentiment == VoterSentiment.Floating || v.Sentiment == VoterSentiment.Against));
        if (cId.HasValue) allSwingBase = allSwingBase.Where(v => v.ConstituencyId == cId.Value);

        AvailableBooths = await allSwingBase.Select(v => v.BoothNumber).Distinct().OrderBy(b => b).ToListAsync();
        AvailableWards  = await allSwingBase.Where(v => v.WardNumber != null)
                              .Select(v => v.WardNumber!).Distinct().OrderBy(w => w).ToListAsync();

        var swingVoterIds = await voterQ.Select(v => v.Id).ToListAsync();

        if (!swingVoterIds.Any())
        {
            TotalSwing    = 0;
            CriticalSwing = 0;
            FloatingSwing = 0;
            return Page();
        }

        // Load visits for these voters to find who was ever Favour
        var visits = await _db.DoorToDoorVisits
            .Where(v => swingVoterIds.Contains(v.VoterId))
            .OrderByDescending(v => v.VisitedAt)
            .Select(v => new
            {
                v.VoterId,
                v.SentimentAfterVisit,
                v.VisitedAt,
                v.WorkerName
            })
            .ToListAsync();

        // Only keep voters who have at least one Favour visit (true swing)
        var votersWithFavourVisit = visits
            .Where(v => v.SentimentAfterVisit == VoterSentiment.Favour)
            .Select(v => v.VoterId)
            .ToHashSet();

        var filteredVoterIds = swingVoterIds
            .Where(id => votersWithFavourVisit.Contains(id))
            .ToHashSet();

        if (!filteredVoterIds.Any())
        {
            TotalSwing    = 0;
            CriticalSwing = 0;
            FloatingSwing = 0;
            return Page();
        }

        // Load voter details
        var voters = await voterQ
            .Where(v => filteredVoterIds.Contains(v.Id))
            .OrderBy(v => v.BoothNumber).ThenBy(v => v.Name)
            .ToListAsync();

        // Build rows
        SwingVoters = voters.Select(v =>
        {
            var vVisits       = visits.Where(x => x.VoterId == v.Id).ToList();
            var latestVisit   = vVisits.FirstOrDefault();
            var favourCount   = vVisits.Count(x => x.SentimentAfterVisit == VoterSentiment.Favour);

            return new SwingVoterRow(
                v.Id,
                v.VoterId,
                v.Name,
                v.MobileNumber,
                v.BoothNumber,
                v.WardNumber,
                v.PannaNumber,
                v.Sentiment,
                favourCount,
                vVisits.Count,
                latestVisit?.VisitedAt,
                latestVisit?.WorkerName);
        }).ToList();

        TotalSwing    = SwingVoters.Count;
        CriticalSwing = SwingVoters.Count(r => r.CurrentSentiment == VoterSentiment.Against);
        FloatingSwing = SwingVoters.Count(r => r.CurrentSentiment == VoterSentiment.Floating);

        return Page();
    }
}
