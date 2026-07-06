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
public class AddRequestModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _userManager;
    public AddRequestModel(AppDbContext db, UserManager<AppUser> userManager) { _db = db; _userManager = userManager; }

    [BindProperty] public VoterTransportRequest Request { get; set; } = new();
    [BindProperty(SupportsGet = true)] public string? SearchVoter { get; set; }
    public List<Voter> SearchResults { get; set; } = new();

    public async Task OnGetAsync()
    {
        if (!string.IsNullOrWhiteSpace(SearchVoter))
        {
            var user = await _userManager.GetUserAsync(User);
            int? cId = user?.ConstituencyId;
            var isSuperAdmin  = user?.Role == UserRole.SuperAdmin;
            SearchResults = await _db.Voters
                .Where(v => !v.IsDeleted && (v.Name.Contains(SearchVoter) || (v.MobileNumber != null && v.MobileNumber.Contains(SearchVoter)) || v.VoterId.Contains(SearchVoter)))
                .Where(v => isSuperAdmin || !cId.HasValue || v.ConstituencyId == cId)
                .Take(10).ToListAsync();
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();
        var user = await _userManager.GetUserAsync(User);
        Request.RequestedByUserId = user?.Id;
        Request.ConstituencyId = user?.ConstituencyId ?? Request.ConstituencyId;
        Request.RequestedAt = DateTime.UtcNow;
        Request.Status = TransportStatus.Pending;
        _db.VoterTransportRequests.Add(Request);
        await _db.SaveChangesAsync();
        TempData["Message"] = "Transport request added.";
        return RedirectToPage("/Transport/Index");
    }
}
