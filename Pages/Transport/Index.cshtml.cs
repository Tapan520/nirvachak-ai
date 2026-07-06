using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Nirvachak_AI.Domain.Entities;
using Nirvachak_AI.Domain.Enums;
using Nirvachak_AI.Infrastructure.Data;

namespace Nirvachak_AI.Pages.Transport;

[Authorize(Roles = "Admin,SuperAdmin,CampaignManager,Candidate,FieldWorker,BoothAgent")]
public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _userManager;
    public IndexModel(AppDbContext db, UserManager<AppUser> userManager) { _db = db; _userManager = userManager; }

    public List<VoterTransportRequest> Requests { get; set; } = new();
    public List<TransportVehicle> Vehicles { get; set; } = new();
    public List<Constituency> Constituencies { get; set; } = new();
    public bool IsAdmin { get; set; }
    public bool CanManage { get; set; }
    public int PendingCount { get; set; }
    public int AssignedCount { get; set; }
    public int PickedUpCount { get; set; }
    public int VotedCount { get; set; }

    [BindProperty(SupportsGet = true)] public int? ConstituencyFilter { get; set; }
    [BindProperty(SupportsGet = true)] public string? StatusFilter { get; set; }

    public async Task OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        IsAdmin = user?.Role == UserRole.SuperAdmin;
        CanManage = user?.Role != UserRole.FieldWorker && user?.Role != UserRole.BoothAgent;

        if (IsAdmin) Constituencies = await _db.Constituencies.OrderBy(c => c.Name).ToListAsync();

        int? cId = IsAdmin ? ConstituencyFilter : user?.ConstituencyId;

        var reqQ = _db.VoterTransportRequests.Include(r => r.Voter).Include(r => r.Vehicle).AsQueryable();
        if (cId.HasValue) reqQ = reqQ.Where(r => r.ConstituencyId == cId);
        if (!string.IsNullOrEmpty(StatusFilter) && Enum.TryParse<TransportStatus>(StatusFilter, out var st))
            reqQ = reqQ.Where(r => r.Status == st);
        Requests = await reqQ.OrderBy(r => r.Status).ThenBy(r => r.RequestedAt).ToListAsync();

        var vehQ = _db.TransportVehicles.AsQueryable();
        if (cId.HasValue) vehQ = vehQ.Where(v => v.ConstituencyId == cId);
        Vehicles = await vehQ.OrderBy(v => v.BoothNumber).ToListAsync();

        PendingCount  = Requests.Count(r => r.Status == TransportStatus.Pending);
        AssignedCount = Requests.Count(r => r.Status == TransportStatus.Assigned);
        PickedUpCount = Requests.Count(r => r.Status == TransportStatus.PickedUp);
        VotedCount    = Requests.Count(r => r.Status == TransportStatus.Voted);
    }

    public async Task<IActionResult> OnPostUpdateStatusAsync(int id, TransportStatus status)
    {
        var req = await _db.VoterTransportRequests.FindAsync(id);
        if (req != null)
        {
            req.Status = status;
            if (status == TransportStatus.Assigned) req.AssignedAt = DateTime.UtcNow;
            if (status == TransportStatus.PickedUp) req.PickedUpAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
        TempData["Message"] = "Status updated.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostAssignVehicleAsync(int requestId, int vehicleId)
    {
        var req = await _db.VoterTransportRequests.FindAsync(requestId);
        if (req != null) { req.VehicleId = vehicleId; req.Status = TransportStatus.Assigned; req.AssignedAt = DateTime.UtcNow; await _db.SaveChangesAsync(); }
        TempData["Message"] = "Vehicle assigned.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteRequestAsync(int id)
    {
        var req = await _db.VoterTransportRequests.FindAsync(id);
        if (req != null) { _db.VoterTransportRequests.Remove(req); await _db.SaveChangesAsync(); }
        TempData["Message"] = "Request removed.";
        return RedirectToPage();
    }
}
