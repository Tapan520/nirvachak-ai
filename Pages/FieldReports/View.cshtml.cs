using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Nirvachak_AI.Domain.Entities;
using Nirvachak_AI.Domain.Enums;
using Nirvachak_AI.Infrastructure.Data;

namespace Nirvachak_AI.Pages.FieldReports;

[Authorize]
public class ViewModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _userManager;
    public ViewModel(AppDbContext db, UserManager<AppUser> userManager) { _db = db; _userManager = userManager; }

    public FieldReport? Report { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        Report = await _db.FieldReports.FindAsync(id);
        if (Report == null) return NotFound();
        var isAdmin = user?.Role == UserRole.Admin || user?.Role == UserRole.SuperAdmin;
        var isManager = user?.Role == UserRole.CampaignManager || user?.Role == UserRole.Candidate || isAdmin;
        if (!isManager && Report.WorkerUserId != user?.Id) return Forbid();
        return Page();
    }
}
