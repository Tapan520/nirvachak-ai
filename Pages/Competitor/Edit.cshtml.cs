using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Nirvachak_AI.Domain.Entities;
using Nirvachak_AI.Domain.Enums;
using Nirvachak_AI.Infrastructure.Data;

namespace Nirvachak_AI.Pages.Competitor;

[Authorize(Roles = "Admin,SuperAdmin,CampaignManager,Candidate")]
public class EditModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _userManager;

    public EditModel(AppDbContext db, UserManager<AppUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    [BindProperty]
    public CompetitorActivity Activity { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        var a = await _db.CompetitorActivities.FindAsync(id);
        if (a == null) return NotFound();
        if (user?.Role != UserRole.SuperAdmin && a.ConstituencyId != user?.ConstituencyId)
            return Forbid();
        Activity = a;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();
        var user = await _userManager.GetUserAsync(User);
        var existing = await _db.CompetitorActivities.FindAsync(Activity.Id);
        if (existing == null) return NotFound();
        if (user?.Role != UserRole.SuperAdmin && existing.ConstituencyId != user?.ConstituencyId)
            return Forbid();
        existing.CompetitorName   = Activity.CompetitorName;
        existing.PartyName        = Activity.PartyName;
        existing.ActivityTitle    = Activity.ActivityTitle;
        existing.ActivityType     = Activity.ActivityType;
        existing.Location         = Activity.Location;
        existing.Ward             = Activity.Ward;
        existing.BoothNumber      = Activity.BoothNumber;
        existing.ActivityDate     = Activity.ActivityDate;
        existing.EstimatedCrowd   = Activity.EstimatedCrowd;
        existing.ThreatLevel      = Activity.ThreatLevel;
        existing.Notes            = Activity.Notes;
        await _db.SaveChangesAsync();
        return RedirectToPage("./Index");
    }
}
