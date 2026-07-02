using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Nirvachak_AI.Domain.Entities;
using Nirvachak_AI.Domain.Enums;
using Nirvachak_AI.Infrastructure.Data;

namespace Nirvachak_AI.Pages.Volunteers;

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
    public Volunteer Volunteer { get; set; } = null!;

    public List<Constituency> Constituencies { get; set; } = new();
    public bool IsAdmin { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        IsAdmin = user?.Role == UserRole.Admin || user?.Role == UserRole.SuperAdmin;

        var volunteer = await _db.Volunteers.FindAsync(id);
        if (volunteer == null) return NotFound();

        if (!IsAdmin && volunteer.ConstituencyId != user?.ConstituencyId)
            return Forbid();

        Volunteer = volunteer;
        if (IsAdmin)
            Constituencies = await _db.Constituencies.OrderBy(c => c.Name).ToListAsync();

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        var user = await _userManager.GetUserAsync(User);
        IsAdmin = user?.Role == UserRole.Admin || user?.Role == UserRole.SuperAdmin;

        var existing = await _db.Volunteers.FindAsync(Volunteer.Id);
        if (existing == null) return NotFound();

        if (!IsAdmin && existing.ConstituencyId != user?.ConstituencyId)
            return Forbid();

        existing.Name                 = Volunteer.Name;
        existing.Phone                = Volunteer.Phone;
        existing.Email                = Volunteer.Email;
        existing.Address              = Volunteer.Address;
        existing.AssignedArea         = Volunteer.AssignedArea;
        existing.AssignedBoothNumbers = Volunteer.AssignedBoothNumbers;
        existing.Task                 = Volunteer.Task;
        existing.IsActive             = Volunteer.IsActive;
        existing.Notes                = Volunteer.Notes;

        if (IsAdmin)
            existing.ConstituencyId = Volunteer.ConstituencyId;

        await _db.SaveChangesAsync();
        TempData["Message"] = "Volunteer updated successfully.";
        return RedirectToPage("/Volunteers/Index");
    }
}
