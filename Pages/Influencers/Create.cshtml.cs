using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Nirvachak_AI.Domain.Entities;
using Nirvachak_AI.Infrastructure.Data;

namespace Nirvachak_AI.Pages.Influencers;

[Authorize(Roles = "Admin,CampaignManager,Candidate")]
public class CreateModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _userManager;
    public CreateModel(AppDbContext db, UserManager<AppUser> userManager) { _db = db; _userManager = userManager; }

    [BindProperty]
    public Influencer Influencer { get; set; } = new();

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();
        var user = await _userManager.GetUserAsync(User);
        Influencer.ConstituencyId = user?.ConstituencyId ?? Influencer.ConstituencyId;
        Influencer.CreatedAt = DateTime.UtcNow;
        _db.Influencers.Add(Influencer);
        await _db.SaveChangesAsync();
        return RedirectToPage("./Index");
    }
}
