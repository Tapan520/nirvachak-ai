using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Nirvachak_AI.Domain.Entities;
using Nirvachak_AI.Domain.Enums;
using Nirvachak_AI.Infrastructure.Data;

namespace Nirvachak_AI.Pages.Competitor;

[Authorize(Roles = "Admin,SuperAdmin,CampaignManager")]
public class DeleteModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _userManager;

    public DeleteModel(AppDbContext db, UserManager<AppUser> userManager)
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
        var user = await _userManager.GetUserAsync(User);
        var a = await _db.CompetitorActivities.FindAsync(Activity.Id);
        if (a != null)
        {
            if (user?.Role != UserRole.SuperAdmin && a.ConstituencyId != user?.ConstituencyId)
                return Forbid();
            _db.CompetitorActivities.Remove(a);
            await _db.SaveChangesAsync();
        }
        return RedirectToPage("./Index");
    }
}
