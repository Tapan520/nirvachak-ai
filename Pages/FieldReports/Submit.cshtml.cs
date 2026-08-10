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
        if (user?.ConstituencyId == null)
        {
            ModelState.AddModelError(string.Empty, "Your account is not assigned to a constituency. Please contact the administrator.");
            return Page();
        }
        Report.WorkerUserId = user.Id;
        Report.WorkerName = user.FullName;
        Report.ConstituencyId = user.ConstituencyId.Value;
        Report.Status = FieldReportStatus.Submitted;
        Report.CreatedAt = DateTime.UtcNow;
        _db.FieldReports.Add(Report);
        await _db.SaveChangesAsync();
        TempData["Message"] = "Daily report submitted successfully.";
        return RedirectToPage("/FieldReports/Index");
    }
}
