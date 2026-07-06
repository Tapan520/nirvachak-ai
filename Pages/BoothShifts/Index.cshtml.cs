using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Nirvachak_AI.Domain.Entities;
using Nirvachak_AI.Domain.Enums;
using Nirvachak_AI.Infrastructure.Data;

namespace Nirvachak_AI.Pages.BoothShifts;

[Authorize(Roles = "Admin,SuperAdmin,CampaignManager,Candidate")]
public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _userManager;
    public IndexModel(AppDbContext db, UserManager<AppUser> userManager) { _db = db; _userManager = userManager; }

    public List<BoothShiftAssignment> Assignments { get; set; } = new();
    public List<Volunteer> Volunteers { get; set; } = new();
    public List<Constituency> Constituencies { get; set; } = new();
    public bool IsAdmin { get; set; }
    public int CoveredBooths { get; set; }

    [BindProperty] public BoothShiftAssignment NewShift { get; set; } = new();
    [BindProperty(SupportsGet = true)] public int? ConstituencyFilter { get; set; }
    [BindProperty(SupportsGet = true)] public int? BoothFilter { get; set; }

    public async Task OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        IsAdmin = user?.Role == UserRole.Admin || user?.Role == UserRole.SuperAdmin;
        if (IsAdmin) Constituencies = await _db.Constituencies.OrderBy(c => c.Name).ToListAsync();

        int? cId = IsAdmin ? ConstituencyFilter : user?.ConstituencyId;

        var aQ = _db.BoothShiftAssignments.Include(a => a.Volunteer).AsQueryable();
        if (cId.HasValue) aQ = aQ.Where(a => a.ConstituencyId == cId);
        if (BoothFilter.HasValue) aQ = aQ.Where(a => a.BoothNumber == BoothFilter);
        Assignments = await aQ.OrderBy(a => a.BoothNumber).ThenBy(a => a.ShiftStart).ToListAsync();

        var vQ = _db.Volunteers.AsQueryable();
        if (cId.HasValue) vQ = vQ.Where(v => v.ConstituencyId == cId && v.IsActive);
        Volunteers = await vQ.OrderBy(v => v.Name).ToListAsync();

        CoveredBooths = Assignments.Select(a => a.BoothNumber).Distinct().Count();
        if (!IsAdmin && user?.ConstituencyId.HasValue == true) NewShift.ConstituencyId = user.ConstituencyId.Value;
    }

    public async Task<IActionResult> OnPostAddAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user?.Role != UserRole.Admin && user?.Role != UserRole.SuperAdmin)
            NewShift.ConstituencyId = user?.ConstituencyId ?? 1;
        NewShift.CreatedAt = DateTime.UtcNow;
        _db.BoothShiftAssignments.Add(NewShift);
        await _db.SaveChangesAsync();
        TempData["Message"] = "Shift assigned.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostConfirmAsync(int id)
    {
        var a = await _db.BoothShiftAssignments.FindAsync(id);
        if (a != null) { a.IsConfirmed = !a.IsConfirmed; await _db.SaveChangesAsync(); }
        TempData["Message"] = "Confirmation updated.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var a = await _db.BoothShiftAssignments.FindAsync(id);
        if (a != null) { _db.BoothShiftAssignments.Remove(a); await _db.SaveChangesAsync(); }
        TempData["Message"] = "Assignment removed.";
        return RedirectToPage();
    }
}
