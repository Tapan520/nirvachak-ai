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
public class EditModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _userManager;
    public EditModel(AppDbContext db, UserManager<AppUser> userManager) { _db = db; _userManager = userManager; }

    [BindProperty] public Domain.Entities.PannaPramukh PannaPramukh { get; set; } = null!;
    public List<Constituency> Constituencies { get; set; } = new();
    public bool IsAdmin { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        IsAdmin = user?.Role == UserRole.SuperAdmin;
        var item = await _db.PannaPramukhs.FindAsync(id);
        if (item == null) return NotFound();
        if (!IsAdmin && item.ConstituencyId != user?.ConstituencyId) return Forbid();
        PannaPramukh = item;
        if (IsAdmin) Constituencies = await _db.Constituencies.OrderBy(c => c.Name).ToListAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();
        var user = await _userManager.GetUserAsync(User);
        IsAdmin = user?.Role == UserRole.SuperAdmin;
        var existing = await _db.PannaPramukhs.FindAsync(PannaPramukh.Id);
        if (existing == null) return NotFound();
        if (!IsAdmin && existing.ConstituencyId != user?.ConstituencyId) return Forbid();

        existing.Name = PannaPramukh.Name;
        existing.Phone = PannaPramukh.Phone;
        existing.Email = PannaPramukh.Email;
        existing.Address = PannaPramukh.Address;
        existing.BoothNumber = PannaPramukh.BoothNumber;
        existing.PannaNumber = PannaPramukh.PannaNumber;
        existing.TotalVotersAssigned = PannaPramukh.TotalVotersAssigned;
        existing.VotersContacted = PannaPramukh.VotersContacted;
        existing.IsActive = PannaPramukh.IsActive;
        existing.Notes = PannaPramukh.Notes;
        if (IsAdmin) existing.ConstituencyId = PannaPramukh.ConstituencyId;

        await _db.SaveChangesAsync();
        TempData["Message"] = "Panna Pramukh updated.";
        return RedirectToPage("/PannaPramukh/Index");
    }
}
