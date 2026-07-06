using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Nirvachak_AI.Domain.Entities;
using Nirvachak_AI.Domain.Enums;
using Nirvachak_AI.Infrastructure.Data;

namespace Nirvachak_AI.Pages.RapidResponse;

[Authorize(Roles = "Admin,SuperAdmin,CampaignManager,Candidate")]
public class EditModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _userManager;
    public EditModel(AppDbContext db, UserManager<AppUser> userManager) { _db = db; _userManager = userManager; }

    [BindProperty] public RapidResponseItem Item { get; set; } = null!;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        var item = await _db.RapidResponseItems.FindAsync(id);
        if (item == null) return NotFound();
        var isAdmin = user?.Role == UserRole.SuperAdmin;
        if (!isAdmin && item.ConstituencyId != user?.ConstituencyId) return Forbid();
        Item = item;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();
        var existing = await _db.RapidResponseItems.FindAsync(Item.Id);
        if (existing == null) return NotFound();
        existing.Title = Item.Title;
        existing.Description = Item.Description;
        existing.Source = Item.Source;
        existing.ThreatLevel = Item.ThreatLevel;
        existing.Status = Item.Status;
        existing.AffectedWards = Item.AffectedWards;
        existing.AssignedToName = Item.AssignedToName;
        existing.ResponseText = Item.ResponseText;
        if (Item.Status == RapidResponseStatus.Resolved && existing.ResolvedAt == null)
            existing.ResolvedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        TempData["Message"] = "Item updated.";
        return RedirectToPage("/RapidResponse/Index");
    }
}
