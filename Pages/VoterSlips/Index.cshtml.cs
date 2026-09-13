using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Nirvachak_AI.Domain.Entities;
using Nirvachak_AI.Domain.Enums;
using Nirvachak_AI.Infrastructure.Data;
using Nirvachak_AI.Infrastructure.Services;

namespace Nirvachak_AI.Pages.VoterSlips;

[Microsoft.AspNetCore.Authorization.Authorize]
public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _userManager;
    private readonly VoterSlipService _slipService;
    private readonly Dictionary<int, string> _qrCache = new();

    public IndexModel(AppDbContext db, UserManager<AppUser> userManager, VoterSlipService slipService)
    {
        _db = db;
        _userManager = userManager;
        _slipService = slipService;
    }

    public List<Voter> Voters { get; set; } = new();
    public List<int> BoothNumbers { get; set; } = new();
    public List<Constituency> Constituencies { get; set; } = new();
    public List<Booth> Booths { get; set; } = new();
    public bool IsAdmin { get; set; }

    [Microsoft.AspNetCore.Mvc.BindProperty(SupportsGet = true)]
    public int? ConstituencyFilter { get; set; }

    [Microsoft.AspNetCore.Mvc.BindProperty(SupportsGet = true)]
    public int? BoothFilter { get; set; }

    [Microsoft.AspNetCore.Mvc.BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    [Microsoft.AspNetCore.Mvc.BindProperty(SupportsGet = true)]
    public bool IncludeCandidatePhotos { get; set; }

    [Microsoft.AspNetCore.Mvc.BindProperty(SupportsGet = true)]
    public string? SelectedCandidateIds { get; set; }

    public List<SurveyCandidate> Candidates { get; set; } = new();

    // Printer's personal candidate profile (Voter/Booth Manager use case).
    // When present, takes priority over the constituency-wide Candidates list.
    public AppUser? PrinterProfile { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        var assignedBooths = ParseAssignedBooths(user?.AssignedBoothNumbers);

        IsAdmin = user?.Role == UserRole.SuperAdmin;

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

        // VoterManager and BoothAgent are restricted to their assigned booths only
        if (user?.Role == UserRole.VoterManager || user?.Role == UserRole.BoothAgent)
        {
            if (assignedBooths.Any())
                query = query.Where(v => assignedBooths.Contains(v.BoothNumber));
        }

        if (BoothFilter.HasValue)
            query = query.Where(v => v.BoothNumber == BoothFilter);

        if (!string.IsNullOrWhiteSpace(Search))
        {
            var s = Search.Trim();
            query = query.Where(v => v.Name.Contains(s) || v.VoterId.Contains(s) ||
                (v.MobileNumber != null && v.MobileNumber.Contains(s)));
        }

        Voters = await query
            .OrderBy(v => v.BoothNumber).ThenBy(v => v.SerialNumber)
            .Take(200)
            .ToListAsync();

        IQueryable<Voter> allQuery = _db.Voters.Where(v => !v.IsDeleted);
        if (IsAdmin)
        {
            if (ConstituencyFilter.HasValue)
                allQuery = allQuery.Where(v => v.ConstituencyId == ConstituencyFilter);
        }
        else if (user?.ConstituencyId.HasValue == true)
            allQuery = allQuery.Where(v => v.ConstituencyId == user.ConstituencyId);

        // Scope booth dropdown to assigned booths for VoterManager and BoothAgent
        if (user?.Role == UserRole.VoterManager || user?.Role == UserRole.BoothAgent)
        {
            if (assignedBooths.Any())
                allQuery = allQuery.Where(v => assignedBooths.Contains(v.BoothNumber));
        }

        BoothNumbers = await allQuery.Select(v => v.BoothNumber).Distinct().OrderBy(n => n).ToListAsync();

        var boothNums = Voters.Select(v => v.BoothNumber).Distinct().ToList();
        int? constId = IsAdmin ? ConstituencyFilter : (int?)null;
        IQueryable<Booth> boothQuery = _db.Booths.Where(b => boothNums.Contains(b.BoothNumber));
        if (constId.HasValue)
            boothQuery = boothQuery.Where(b => b.ConstituencyId == constId.Value);
        else if (!IsAdmin && user?.ConstituencyId.HasValue == true)
            boothQuery = boothQuery.Where(b => b.ConstituencyId == user.ConstituencyId);
        Booths = await boothQuery.ToListAsync();

        foreach (var v in Voters)
            _qrCache[v.Id] = _slipService.GenerateQrCodeBase64(v);

        // Candidate photo strip (optional)
        int? candidateConstituencyId = IsAdmin ? ConstituencyFilter : user?.ConstituencyId;
        if (IncludeCandidatePhotos)
        {
            // Priority 1 — current user's own candidate profile (Panchayat / party-agent flow)
            if (user != null && !string.IsNullOrEmpty(user.CandidatePhotoUrl))
            {
                PrinterProfile = user;
            }
            // Priority 2 — constituency-wide SurveyCandidates
            else if (candidateConstituencyId.HasValue)
            {
                var candQuery = _db.SurveyCandidates
                    .Where(c => c.ConstituencyId == candidateConstituencyId.Value
                             && c.IsActive
                             && c.PhotoUrl != null && c.PhotoUrl != "");

                if (!string.IsNullOrWhiteSpace(SelectedCandidateIds))
                {
                    var ids = SelectedCandidateIds
                        .Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => int.TryParse(s.Trim(), out var n) ? n : 0)
                        .Where(n => n > 0).ToList();
                    if (ids.Any()) candQuery = candQuery.Where(c => ids.Contains(c.Id));
                }

                Candidates = await candQuery
                    .OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name)
                    .ToListAsync();
            }
        }

        return Page();
    }

    public string GetQrCode(int voterId) => _qrCache.TryGetValue(voterId, out var qr) ? qr : "";

    private static List<int> ParseAssignedBooths(string? assignedBoothNumbers) =>
        (assignedBoothNumbers ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => int.TryParse(s.Trim(), out var n) ? (int?)n : null)
            .Where(n => n.HasValue).Select(n => n!.Value).ToList();
}
