using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Nirvachak_AI.Domain.Entities;
using Nirvachak_AI.Domain.Enums;
using Nirvachak_AI.Infrastructure.Data;

namespace Nirvachak_AI.Pages.Transport;

[Authorize(Roles = "Admin,SuperAdmin,CampaignManager,Candidate")]
public class VehiclesModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _userManager;
    public VehiclesModel(AppDbContext db, UserManager<AppUser> userManager) { _db = db; _userManager = userManager; }

    public List<TransportVehicle> Vehicles { get; set; } = new();
    public List<Constituency> Constituencies { get; set; } = new();
    public bool IsAdmin { get; set; }

    [BindProperty] public TransportVehicle NewVehicle { get; set; } = new();
    [BindProperty(SupportsGet = true)] public int? ConstituencyFilter { get; set; }

    public async Task OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        IsAdmin = user?.Role == UserRole.Admin || user?.Role == UserRole.SuperAdmin;
        if (IsAdmin) Constituencies = await _db.Constituencies.OrderBy(c => c.Name).ToListAsync();
        int? cId = IsAdmin ? ConstituencyFilter : user?.ConstituencyId;
        var q = _db.TransportVehicles.AsQueryable();
        if (cId.HasValue) q = q.Where(v => v.ConstituencyId == cId);
        Vehicles = await q.OrderBy(v => v.BoothNumber).ToListAsync();
        if (!IsAdmin && user?.ConstituencyId.HasValue == true) NewVehicle.ConstituencyId = user.ConstituencyId.Value;
    }

    public async Task<IActionResult> OnPostAddVehicleAsync()
    {
        if (!ModelState.IsValid) return Page();
        var user = await _userManager.GetUserAsync(User);
        if (user?.Role != UserRole.Admin && user?.Role != UserRole.SuperAdmin)
            NewVehicle.ConstituencyId = user?.ConstituencyId ?? 1;
        NewVehicle.CreatedAt = DateTime.UtcNow;
        _db.TransportVehicles.Add(NewVehicle);
        await _db.SaveChangesAsync();
        TempData["Message"] = "Vehicle added.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostToggleAsync(int id)
    {
        var v = await _db.TransportVehicles.FindAsync(id);
        if (v != null) { v.IsAvailable = !v.IsAvailable; await _db.SaveChangesAsync(); }
        TempData["Message"] = "Vehicle status updated.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteVehicleAsync(int id)
    {
        var v = await _db.TransportVehicles.FindAsync(id);
        if (v != null) { _db.TransportVehicles.Remove(v); await _db.SaveChangesAsync(); }
        TempData["Message"] = "Vehicle removed.";
        return RedirectToPage();
    }
}
