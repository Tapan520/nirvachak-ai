using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Nirvachak_AI.Domain.Entities;
using Nirvachak_AI.Domain.Enums;
using Nirvachak_AI.Infrastructure.Data;

namespace Nirvachak_AI.Pages.Influencers;

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
    public Influencer Influencer { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        var inf = await _db.Influencers.FindAsync(id);
        if (inf == null) return NotFound();
        if (user?.Role != UserRole.SuperAdmin && inf.ConstituencyId != user?.ConstituencyId)
            return Forbid();
        Influencer = inf;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();
        var user = await _userManager.GetUserAsync(User);
        var existing = await _db.Influencers.FindAsync(Influencer.Id);
        if (existing == null) return NotFound();
        if (user?.Role != UserRole.SuperAdmin && existing.ConstituencyId != user?.ConstituencyId)
            return Forbid();
        existing.Name               = Influencer.Name;
        existing.MobileNumber       = Influencer.MobileNumber;
        existing.Category           = Influencer.Category;
        existing.Community          = Influencer.Community;
        existing.EstimatedFollowers = Influencer.EstimatedFollowers;
        existing.Ward               = Influencer.Ward;
        existing.BoothNumber        = Influencer.BoothNumber;
        existing.Alignment          = Influencer.Alignment;
        existing.LastMetAt          = Influencer.LastMetAt;
        existing.LastMeetingOutcome = Influencer.LastMeetingOutcome;
        existing.Notes              = Influencer.Notes;
        existing.IsActive           = Influencer.IsActive;
        await _db.SaveChangesAsync();
        return RedirectToPage("./Index");
    }
}
