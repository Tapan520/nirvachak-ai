using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Nirvachak_AI.Domain.Entities;
using Nirvachak_AI.Domain.Enums;
using Nirvachak_AI.Infrastructure.Data;

namespace Nirvachak_AI.Pages.Competitor;

[Authorize(Roles = "Admin,SuperAdmin,CampaignManager,Candidate")]
public class CreateModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _userManager;

    public CreateModel(AppDbContext db, UserManager<AppUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    [BindProperty]
    public CompetitorActivity Activity { get; set; } = new();

    public void OnGet() { Activity.ActivityDate = DateTime.Today; }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();
        var user = await _userManager.GetUserAsync(User);
        Activity.LoggedByUserId = user?.Id;
        if (user?.Role != UserRole.SuperAdmin)
            Activity.ConstituencyId = user?.ConstituencyId ?? Activity.ConstituencyId;
        Activity.CreatedAt = DateTime.UtcNow;
        _db.CompetitorActivities.Add(Activity);
        await _db.SaveChangesAsync();
        return RedirectToPage("./Index");
    }
}
