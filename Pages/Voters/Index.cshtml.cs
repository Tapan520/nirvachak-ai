using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Nirvachak_AI.Domain.Entities;
using Nirvachak_AI.Domain.Enums;
using Nirvachak_AI.Infrastructure.Data;
using Nirvachak_AI.Infrastructure.Services;

namespace Nirvachak_AI.Pages.Voters;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _userManager;
    private readonly AuditService _audit;
    private const int PageSize = 50;

    public IndexModel(AppDbContext db, UserManager<AppUser> userManager, AuditService audit)
    {
        _db = db;
        _userManager = userManager;
        _audit = audit;
    }

    public List<Voter> Voters { get; set; } = new();
    public List<int> BoothNumbers { get; set; } = new();
    public List<Constituency> Constituencies { get; set; } = new();
    public List<Ward> Wards { get; set; } = new();
    public bool IsAdmin { get; set; }
    public bool CanImportCsv { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }

    [Microsoft.AspNetCore.Mvc.BindProperty(SupportsGet = true)]
    public string? Search { get; set; }
    [Microsoft.AspNetCore.Mvc.BindProperty(SupportsGet = true)]
    public int? ConstituencyFilter { get; set; }
    [Microsoft.AspNetCore.Mvc.BindProperty(SupportsGet = true)]
    public int? BoothFilter { get; set; }
    [Microsoft.AspNetCore.Mvc.BindProperty(SupportsGet = true)]
    public string? SentimentFilter { get; set; }
    [Microsoft.AspNetCore.Mvc.BindProperty(SupportsGet = true)]
    public string? GenderFilter { get; set; }
    [Microsoft.AspNetCore.Mvc.BindProperty(SupportsGet = true)]
    public string? WardFilter { get; set; }
    [Microsoft.AspNetCore.Mvc.BindProperty(SupportsGet = true)]
    public int CurrentPage { get; set; } = 1;

    public async Task OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        IsAdmin = user?.Role == UserRole.SuperAdmin;
        CanImportCsv = user?.Role != UserRole.FieldWorker && user?.Role != UserRole.BoothAgent;

        if (IsAdmin)
            Constituencies = await _db.Constituencies.OrderBy(c => c.Name).ToListAsync();

        IQueryable<Voter> query = _db.Voters.Where(v => !v.IsDeleted);

        if (IsAdmin)
        {
            if (ConstituencyFilter.HasValue)
                query = query.Where(v => v.ConstituencyId == ConstituencyFilter);
        }
        else if (user?.ConstituencyId.HasValue == true)
            query = query.Where(v => v.ConstituencyId == user.ConstituencyId);

        // Load wards for the active constituency
        var cId = IsAdmin ? ConstituencyFilter : user?.ConstituencyId;
        if (cId.HasValue)
            Wards = await _db.Wards.Where(w => w.ConstituencyId == cId.Value).OrderBy(w => w.WardNumber).ToListAsync();

        if (!string.IsNullOrEmpty(WardFilter))
            query = query.Where(v => v.WardNumber == WardFilter);

        if (!string.IsNullOrWhiteSpace(Search))
            query = query.Where(v => v.Name.Contains(Search) || v.VoterId.Contains(Search) ||
                (v.MobileNumber != null && v.MobileNumber.Contains(Search)));

        if (BoothFilter.HasValue)
            query = query.Where(v => v.BoothNumber == BoothFilter);

        if (!string.IsNullOrEmpty(SentimentFilter) && Enum.TryParse<VoterSentiment>(SentimentFilter, out var sentiment))
            query = query.Where(v => v.Sentiment == sentiment);

        if (!string.IsNullOrEmpty(GenderFilter))
            query = query.Where(v => v.Gender == GenderFilter);

        TotalCount = await query.CountAsync();
        TotalPages = (int)Math.Ceiling((double)TotalCount / PageSize);
        CurrentPage = Math.Max(1, Math.Min(CurrentPage, Math.Max(1, TotalPages)));

        Voters = await query
            .OrderBy(v => v.BoothNumber).ThenBy(v => v.SerialNumber)
            .Skip((CurrentPage - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();

        // Booth numbers for filter dropdown � scoped to selected constituency for Admin
        IQueryable<Voter> allVoters = _db.Voters.Where(v => !v.IsDeleted);
        if (IsAdmin)
        {
            if (ConstituencyFilter.HasValue)
                allVoters = allVoters.Where(v => v.ConstituencyId == ConstituencyFilter);
        }
        else if (user?.ConstituencyId.HasValue == true)
            allVoters = allVoters.Where(v => v.ConstituencyId == user.ConstituencyId);
        BoothNumbers = await allVoters.Select(v => v.BoothNumber).Distinct().OrderBy(n => n).ToListAsync();
    }

    public async Task<IActionResult> OnPostBulkSentimentAsync(List<int> selectedIds, VoterSentiment bulkSentiment)
    {
        if (selectedIds == null || selectedIds.Count == 0)
        {
            TempData["Error"] = "No voters selected.";
            return RedirectToPage();
        }
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser == null) return Forbid();

        var voters = await _db.Voters
            .Where(v => selectedIds.Contains(v.Id) && !v.IsDeleted)
            .ToListAsync();

        foreach (var voter in voters)
            voter.Sentiment = bulkSentiment;

        _audit.Track(currentUser.Id, currentUser.FullName,
            "BulkUpdateSentiment", "Voter", string.Join(",", selectedIds),
            $"Bulk sentiment set to {bulkSentiment} for {voters.Count} voters",
            currentUser.ConstituencyId);

        await _db.SaveChangesAsync();
        TempData["Message"] = $"Sentiment updated to '{bulkSentiment}' for {voters.Count} voter(s).";
        return RedirectToPage();
    }
}
