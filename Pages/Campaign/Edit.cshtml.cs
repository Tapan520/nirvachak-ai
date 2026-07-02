using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Nirvachak_AI.Domain.Entities;
using Nirvachak_AI.Domain.Enums;
using Nirvachak_AI.Infrastructure.Data;

namespace Nirvachak_AI.Pages.Campaign;

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
    public CampaignEvent Event { get; set; } = null!;

    public List<Constituency> Constituencies { get; set; } = new();
    public bool IsAdmin { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        IsAdmin = user?.Role == UserRole.Admin || user?.Role == UserRole.SuperAdmin;

        var ev = await _db.CampaignEvents.FindAsync(id);
        if (ev == null) return NotFound();

        if (!IsAdmin && ev.ConstituencyId != user?.ConstituencyId)
            return Forbid();

        Event = ev;
        if (IsAdmin)
            Constituencies = await _db.Constituencies.OrderBy(c => c.Name).ToListAsync();

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        var user = await _userManager.GetUserAsync(User);
        IsAdmin = user?.Role == UserRole.Admin || user?.Role == UserRole.SuperAdmin;

        var existing = await _db.CampaignEvents.FindAsync(Event.Id);
        if (existing == null) return NotFound();

        if (!IsAdmin && existing.ConstituencyId != user?.ConstituencyId)
            return Forbid();

        existing.Title          = Event.Title;
        existing.EventType      = Event.EventType;
        existing.Description    = Event.Description;
        existing.Location       = Event.Location;
        existing.ScheduledAt    = Event.ScheduledAt;
        existing.ExpectedAttendance  = Event.ExpectedAttendance;
        existing.ActualAttendance    = Event.ActualAttendance;
        existing.TargetBoothNumbers  = Event.TargetBoothNumbers;
        existing.TargetWards         = Event.TargetWards;
        existing.IsCompleted         = Event.IsCompleted;

        if (IsAdmin)
            existing.ConstituencyId = Event.ConstituencyId;

        await _db.SaveChangesAsync();
        TempData["Message"] = "Event updated successfully.";
        return RedirectToPage("/Campaign/Index");
    }
}
