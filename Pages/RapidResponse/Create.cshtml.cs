using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Nirvachak_AI.Domain.Entities;
using Nirvachak_AI.Domain.Enums;
using Nirvachak_AI.Infrastructure.Data;

namespace Nirvachak_AI.Pages.RapidResponse;

[Authorize(Roles = "Admin,SuperAdmin,CampaignManager,Candidate")]
public class CreateModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _userManager;
    public CreateModel(AppDbContext db, UserManager<AppUser> userManager) { _db = db; _userManager = userManager; }

    [BindProperty] public RapidResponseItem Item { get; set; } = new();

    public void OnGet() { Item.DetectedAt = DateTime.Now; }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();
        var user = await _userManager.GetUserAsync(User);
        Item.LoggedByUserId = user?.Id;
        Item.ConstituencyId = user?.ConstituencyId ?? Item.ConstituencyId;
        Item.CreatedAt = DateTime.UtcNow;
        _db.RapidResponseItems.Add(Item);
        await _db.SaveChangesAsync();
        TempData["Message"] = "Rapid response item logged.";
        return RedirectToPage("/RapidResponse/Index");
    }
}
