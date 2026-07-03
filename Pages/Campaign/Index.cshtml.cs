using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Nirvachak_AI.Domain.Entities;
using Nirvachak_AI.Domain.Enums;
using Nirvachak_AI.Infrastructure.Data;

namespace Nirvachak_AI.Pages.Campaign;

public class IndexModel : PageModel
{
    private static readonly UserRole[] ManageRoles = [UserRole.Admin, UserRole.SuperAdmin, UserRole.CampaignManager, UserRole.Candidate];
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _userManager;

    public IndexModel(AppDbContext db, UserManager<AppUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public List<CampaignEvent> Events { get; set; } = new();
    public List<Constituency> Constituencies { get; set; } = new();
    public bool IsAdmin { get; set; }
    public bool CanManage { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? ConstituencyFilter { get; set; }

    public async Task OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        IsAdmin = user?.Role == UserRole.SuperAdmin;
        CanManage = user != null && ManageRoles.Contains(user.Role);
        if (IsAdmin)
            Constituencies = await _db.Constituencies.OrderBy(c => c.Name).ToListAsync();

        IQueryable<CampaignEvent> query = _db.CampaignEvents.OrderByDescending(e => e.ScheduledAt);
        if (IsAdmin)
        {
            if (ConstituencyFilter.HasValue)
                query = query.Where(e => e.ConstituencyId == ConstituencyFilter);
        }
        else if (user?.ConstituencyId.HasValue == true)
            query = query.Where(e => e.ConstituencyId == user.ConstituencyId);
        Events = await query.ToListAsync();
    }

    public async Task<IActionResult> OnPostCompleteAsync(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null || !ManageRoles.Contains(user.Role)) return Forbid();
        var ev = await _db.CampaignEvents.FindAsync(id);
        if (ev != null)
        {
            if (user.Role != UserRole.SuperAdmin && ev.ConstituencyId != user.ConstituencyId)
                return Forbid();
            ev.IsCompleted = true;
            await _db.SaveChangesAsync();
            TempData["Message"] = "Event marked as completed.";
        }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null || !ManageRoles.Contains(user.Role)) return Forbid();
        var ev = await _db.CampaignEvents.FindAsync(id);
        if (ev != null)
        {
            if (user.Role != UserRole.SuperAdmin && ev.ConstituencyId != user.ConstituencyId)
                return Forbid();
            _db.CampaignEvents.Remove(ev);
            await _db.SaveChangesAsync();
            TempData["Message"] = $"Event '{ev.Title}' deleted.";
        }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostUpdateAttendanceAsync(int id, int actualAttendance)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null || !ManageRoles.Contains(user.Role)) return Forbid();
        var ev = await _db.CampaignEvents.FindAsync(id);
        if (ev != null)
        {
            if (user.Role != UserRole.SuperAdmin && ev.ConstituencyId != user.ConstituencyId)
                return Forbid();
            ev.ActualAttendance = actualAttendance;
            await _db.SaveChangesAsync();
            TempData["Message"] = $"Attendance updated for '{ev.Title}'.";
        }
        return RedirectToPage();
    }
}
