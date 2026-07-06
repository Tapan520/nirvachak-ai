using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Nirvachak_AI.Domain.Entities;
using Nirvachak_AI.Domain.Enums;
using Nirvachak_AI.Infrastructure.Data;

namespace Nirvachak_AI.Pages.FieldReports;

[Authorize]
public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _userManager;
    public IndexModel(AppDbContext db, UserManager<AppUser> userManager) { _db = db; _userManager = userManager; }

    public List<FieldReport> Reports { get; set; } = new();
    public List<Constituency> Constituencies { get; set; } = new();
    public bool IsAdmin { get; set; }
    public bool IsManager { get; set; }

    [BindProperty(SupportsGet = true)] public int? ConstituencyFilter { get; set; }
    [BindProperty(SupportsGet = true)] public string? WorkerFilter { get; set; }

    public async Task OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        IsAdmin = user?.Role == UserRole.SuperAdmin;
        IsManager = user?.Role == UserRole.CampaignManager || user?.Role == UserRole.Candidate || IsAdmin;

        if (IsAdmin) Constituencies = await _db.Constituencies.OrderBy(c => c.Name).ToListAsync();

        var q = _db.FieldReports.AsQueryable();
        if (IsAdmin)
        { if (ConstituencyFilter.HasValue) q = q.Where(r => r.ConstituencyId == ConstituencyFilter); }
        else if (user?.ConstituencyId.HasValue == true)
            q = q.Where(r => r.ConstituencyId == user.ConstituencyId);

        // Field workers see only their own reports
        if (!IsManager && user != null)
            q = q.Where(r => r.WorkerUserId == user.Id);

        if (!string.IsNullOrEmpty(WorkerFilter))
            q = q.Where(r => r.WorkerName.Contains(WorkerFilter));

        Reports = await q.OrderByDescending(r => r.ReportDate).ToListAsync();
    }

    public async Task<IActionResult> OnPostReviewAsync(int id, FieldReportStatus status, string? reviewerNotes)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Forbid();
        var isManager = user.Role == UserRole.CampaignManager || user.Role == UserRole.Candidate ||
                        user.Role == UserRole.Admin || user.Role == UserRole.SuperAdmin;
        if (!isManager) return Forbid();

        var report = await _db.FieldReports.FindAsync(id);
        if (report != null)
        {
            report.Status = status;
            report.ReviewerNotes = reviewerNotes;
            report.ReviewedByUserId = user.Id;
            report.ReviewedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            TempData["Message"] = "Report reviewed.";
        }
        return RedirectToPage();
    }
}
