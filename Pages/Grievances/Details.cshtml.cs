using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Nirvachak_AI.Domain.Entities;
using Nirvachak_AI.Domain.Enums;
using Nirvachak_AI.Infrastructure.Data;

namespace Nirvachak_AI.Pages.Grievances;

public class DetailsModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _userManager;

    public DetailsModel(AppDbContext db, UserManager<AppUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public Grievance? Grievance { get; set; }

    private async Task<bool> IsRestrictedRoleAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        return user?.Role == UserRole.FieldWorker || user?.Role == UserRole.BoothAgent;
    }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user?.Role == UserRole.FieldWorker || user?.Role == UserRole.BoothAgent) return Forbid();
        Grievance = await _db.Grievances.FindAsync(id);
        if (Grievance == null) return NotFound();
        if (user?.Role != UserRole.SuperAdmin && Grievance.ConstituencyId != user?.ConstituencyId)
            return Forbid();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id, GrievanceStatus status, string? notes)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user?.Role == UserRole.FieldWorker || user?.Role == UserRole.BoothAgent) return Forbid();
        var g = await _db.Grievances.FindAsync(id);
        if (g != null && user?.Role != UserRole.SuperAdmin && g.ConstituencyId != user?.ConstituencyId)
            return Forbid();
        if (g != null)
        {
            g.Status = status;
            g.ResolutionNotes = notes;
            if (status == GrievanceStatus.Resolved) g.ResolvedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
        return RedirectToPage(new { id });
    }
}
