using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Nirvachak_AI.Domain.Entities;
using Nirvachak_AI.Domain.Enums;
using Nirvachak_AI.Infrastructure.Data;

namespace Nirvachak_AI.Pages.FieldReports;

[Authorize]
public class SubmitModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _userManager;
    public SubmitModel(AppDbContext db, UserManager<AppUser> userManager) { _db = db; _userManager = userManager; }

    [BindProperty] public FieldReport Report { get; set; } = new() { ReportDate = DateTime.Today };

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();
        var user = await _userManager.GetUserAsync(User);
        Report.WorkerUserId = user?.Id ?? string.Empty;
        Report.WorkerName = user?.FullName ?? string.Empty;
        Report.ConstituencyId = user?.ConstituencyId ?? 1;
        Report.Status = FieldReportStatus.Submitted;
        Report.CreatedAt = DateTime.UtcNow;
        _db.FieldReports.Add(Report);
        await _db.SaveChangesAsync();
        TempData["Message"] = "Daily report submitted successfully.";
        return RedirectToPage("/FieldReports/Index");
    }
}
