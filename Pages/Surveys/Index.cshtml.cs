using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Nirvachak_AI.Domain.Entities;
using Nirvachak_AI.Domain.Enums;
using Nirvachak_AI.Infrastructure.Data;
using SurveyEntity = Nirvachak_AI.Domain.Entities.Survey;

namespace Nirvachak_AI.Pages.Surveys;

[Microsoft.AspNetCore.Authorization.Authorize]
public class IndexModel : PageModel
{
    private static readonly UserRole[] ManageRoles = [UserRole.Admin, UserRole.SuperAdmin, UserRole.CampaignManager, UserRole.Candidate];
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _userManager;

    public IndexModel(AppDbContext db, UserManager<AppUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    [BindProperty(SupportsGet = true)]
    public int? SelectedConstituencyId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SelectedWard { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? SelectedBoothNumber { get; set; }

    public List<Constituency> Constituencies { get; set; } = new();
    public List<Ward> Wards { get; set; } = new();
    public List<Booth> Booths { get; set; } = new();
    public bool IsAdmin { get; set; }
    public bool CanManage { get; set; }

    public List<SurveyRow> Surveys { get; set; } = new();

    public record SurveyRow(SurveyEntity Survey, int ResponseCount, double AvgRating);

    public async Task OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        IsAdmin = user?.Role == UserRole.SuperAdmin;
        CanManage = user != null && ManageRoles.Contains(user.Role);
        var isRestricted = user?.Role == UserRole.FieldWorker || user?.Role == UserRole.BoothAgent;

        // Parse assigned booth numbers for restricted roles
        var assignedBoothNumbers = isRestricted
            ? (user?.AssignedBoothNumbers ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => int.TryParse(s.Trim(), out var n) ? (int?)n : null)
                .Where(n => n.HasValue).Select(n => n!.Value).ToList()
            : new List<int>();

        if (IsAdmin)
            Constituencies = await _db.Constituencies.OrderBy(c => c.Name).ToListAsync();

        int? cId = IsAdmin ? SelectedConstituencyId : user?.ConstituencyId;

        // Load wards for drill-down (hidden for restricted roles � they only see their booth)
        if (cId.HasValue && !isRestricted)
            Wards = await _db.Wards.Where(w => w.ConstituencyId == cId.Value).OrderBy(w => w.WardNumber).ToListAsync();

        // Load booths for drill-down
        if (cId.HasValue)
        {
            var boothQuery = _db.Booths.Where(b => b.ConstituencyId == cId.Value);
            if (!string.IsNullOrEmpty(SelectedWard))
                boothQuery = boothQuery.Where(b => b.WardNumber == SelectedWard);
            // Restricted roles only see their own assigned booths
            if (isRestricted && assignedBoothNumbers.Any())
                boothQuery = boothQuery.Where(b => assignedBoothNumbers.Contains(b.BoothNumber));
            Booths = await boothQuery.OrderBy(b => b.BoothNumber).ToListAsync();
        }

        IQueryable<SurveyEntity> query = _db.Surveys
            .Include(s => s.Constituency)
            .Include(s => s.Responses)
            .OrderByDescending(s => s.CreatedAt);

        if (IsAdmin)
        {
            if (SelectedConstituencyId.HasValue)
                query = query.Where(s => s.ConstituencyId == SelectedConstituencyId);
        }
        else if (user?.ConstituencyId.HasValue == true)
        {
            query = query.Where(s => s.ConstituencyId == user.ConstituencyId);
        }

        var surveys = await query.ToListAsync();

        // For restricted roles: force filter to assigned booths regardless of UI selection
        var effectiveBoothFilter = isRestricted && assignedBoothNumbers.Any()
            ? assignedBoothNumbers
            : (SelectedBoothNumber.HasValue ? new List<int> { SelectedBoothNumber.Value } : new List<int>());

        // Apply ward / booth drill-down by filtering on responses
        if (!isRestricted && !string.IsNullOrEmpty(SelectedWard))
            surveys = surveys.Where(s => s.Responses.Any(r => r.Ward == SelectedWard)).ToList();

        if (effectiveBoothFilter.Any())
            surveys = surveys.Where(s => s.Responses.Any(r => r.BoothNumber.HasValue && effectiveBoothFilter.Contains(r.BoothNumber.Value))).ToList();

        Surveys = surveys.Select(s =>
        {
            var responses = s.Responses.AsEnumerable();
            if (!isRestricted && !string.IsNullOrEmpty(SelectedWard))
                responses = responses.Where(r => r.Ward == SelectedWard);
            if (effectiveBoothFilter.Any())
                responses = responses.Where(r => r.BoothNumber.HasValue && effectiveBoothFilter.Contains(r.BoothNumber.Value));
            var list = responses.ToList();
            return new SurveyRow(s, list.Count, list.Any() ? Math.Round(list.Average(r => r.Rating), 1) : 0);
        }).ToList();
    }

    public async Task<IActionResult> OnPostToggleAsync(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null || !ManageRoles.Contains(user.Role)) return Forbid();
        var survey = await _db.Surveys.FindAsync(id);
        if (survey != null)
        {
        if (user.Role != UserRole.SuperAdmin && survey.ConstituencyId != user.ConstituencyId)
            return Forbid();
            survey.IsActive = !survey.IsActive;
            await _db.SaveChangesAsync();
            TempData["Message"] = $"Survey '{survey.Title}' {(survey.IsActive ? "activated" : "deactivated")}.";
        }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null || !ManageRoles.Contains(user.Role)) return Forbid();
        var survey = await _db.Surveys
            .Include(s => s.Responses)
            .FirstOrDefaultAsync(s => s.Id == id);
        if (survey != null)
        {
        if (user.Role != UserRole.SuperAdmin && survey.ConstituencyId != user.ConstituencyId)
            return Forbid();
            _db.SurveyResponses.RemoveRange(survey.Responses);
            _db.Surveys.Remove(survey);
            await _db.SaveChangesAsync();
            TempData["Message"] = $"Survey '{survey.Title}' deleted.";
        }
        return RedirectToPage();
    }
}
