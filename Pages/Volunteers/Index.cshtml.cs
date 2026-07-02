using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Nirvachak_AI.Domain.Entities;
using Nirvachak_AI.Domain.Enums;
using Nirvachak_AI.Infrastructure.Data;

namespace Nirvachak_AI.Pages.Volunteers;

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

    public List<Volunteer> Volunteers { get; set; } = new();
    public List<Constituency> Constituencies { get; set; } = new();
    public bool IsAdmin { get; set; }
    public bool CanManage { get; set; }
    public Dictionary<string, int> VolunteerVisitCounts { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public int? ConstituencyFilter { get; set; }

    public async Task OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        IsAdmin = user?.Role == UserRole.Admin || user?.Role == UserRole.SuperAdmin;
        CanManage = user != null && ManageRoles.Contains(user.Role);
        if (IsAdmin)
            Constituencies = await _db.Constituencies.OrderBy(c => c.Name).ToListAsync();

        IQueryable<Volunteer> query = _db.Volunteers.OrderByDescending(v => v.RegisteredAt);
        if (IsAdmin)
        {
            if (ConstituencyFilter.HasValue)
                query = query.Where(v => v.ConstituencyId == ConstituencyFilter);
        }
        else if (user?.ConstituencyId.HasValue == true)
            query = query.Where(v => v.ConstituencyId == user.ConstituencyId);
        Volunteers = await query.ToListAsync();

        // Count door-to-door visits matched by volunteer name
        var names = Volunteers.Select(v => v.Name).ToHashSet();
        var visitCounts = await _db.DoorToDoorVisits
            .Where(d => names.Contains(d.WorkerName))
            .GroupBy(d => d.WorkerName)
            .Select(g => new { Name = g.Key, Count = g.Count() })
            .ToListAsync();
        VolunteerVisitCounts = visitCounts.ToDictionary(x => x.Name, x => x.Count);
    }

    public async Task<IActionResult> OnPostToggleAsync(int id)
    {
        var v = await _db.Volunteers.FindAsync(id);
        if (v != null) { v.IsActive = !v.IsActive; await _db.SaveChangesAsync(); }
        TempData["Message"] = "Volunteer status updated.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null || !ManageRoles.Contains(user.Role)) return Forbid();
        var v = await _db.Volunteers.FindAsync(id);
        if (v != null)
        {
            if (user.Role != UserRole.Admin && user.Role != UserRole.SuperAdmin && v.ConstituencyId != user.ConstituencyId)
                return Forbid();
            _db.Volunteers.Remove(v);
            await _db.SaveChangesAsync();
            TempData["Message"] = $"Volunteer '{v.Name}' deleted.";
        }
        return RedirectToPage();
    }
}
