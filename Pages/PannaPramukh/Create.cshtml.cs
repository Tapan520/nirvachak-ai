using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Nirvachak_AI.Domain.Entities;
using Nirvachak_AI.Domain.Enums;
using Nirvachak_AI.Infrastructure.Data;

namespace Nirvachak_AI.Pages.PannaPramukh;

[Authorize(Roles = "Admin,SuperAdmin,CampaignManager,Candidate")]
public class CreateModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _userManager;
    public CreateModel(AppDbContext db, UserManager<AppUser> userManager) { _db = db; _userManager = userManager; }

    [BindProperty] public Domain.Entities.PannaPramukh PannaPramukh { get; set; } = new();
    public List<Constituency> Constituencies { get; set; } = new();
    public bool IsAdmin { get; set; }

    public async Task OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        IsAdmin = user?.Role == UserRole.Admin || user?.Role == UserRole.SuperAdmin;
        if (IsAdmin) Constituencies = await _db.Constituencies.OrderBy(c => c.Name).ToListAsync();
        else if (user?.ConstituencyId.HasValue == true)
            PannaPramukh.ConstituencyId = user.ConstituencyId.Value;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();
        var user = await _userManager.GetUserAsync(User);
        if (user?.Role != UserRole.Admin && user?.Role != UserRole.SuperAdmin)
            PannaPramukh.ConstituencyId = user?.ConstituencyId ?? 1;
        PannaPramukh.CreatedAt = DateTime.UtcNow;
        _db.PannaPramukhs.Add(PannaPramukh);
        await _db.SaveChangesAsync();
        TempData["Message"] = $"Panna Pramukh '{PannaPramukh.Name}' added.";
        return RedirectToPage("/PannaPramukh/Index");
    }
}
