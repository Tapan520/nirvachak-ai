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

    [BindProperty] public new VoterTransportRequest Request { get; set; } = new();
    [BindProperty(SupportsGet = true)] public string? SearchVoter { get; set; }
    [BindProperty(SupportsGet = true)] public int? SelectedConstituencyId { get; set; }
    public List<Voter> SearchResults { get; set; } = new();
    public List<Constituency> Constituencies { get; set; } = new();
    public bool IsAdmin { get; set; }

    public async Task OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        IsAdmin = user?.Role == UserRole.SuperAdmin;
        if (IsAdmin)
            Constituencies = await _db.Constituencies.OrderBy(c => c.Name).ToListAsync();

        if (!string.IsNullOrWhiteSpace(SearchVoter))
        {
            int? cId = IsAdmin ? SelectedConstituencyId ?? user?.ConstituencyId : user?.ConstituencyId;
            SearchResults = await _db.Voters
                .Where(v => !v.IsDeleted && (v.Name.Contains(SearchVoter) || (v.MobileNumber != null && v.MobileNumber.Contains(SearchVoter)) || v.VoterId.Contains(SearchVoter)))
                .Where(v => IsAdmin ? (!cId.HasValue || v.ConstituencyId == cId) : v.ConstituencyId == cId)
                .Take(10).ToListAsync();
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        IsAdmin = user?.Role == UserRole.SuperAdmin;
        if (IsAdmin)
            Constituencies = await _db.Constituencies.OrderBy(c => c.Name).ToListAsync();

        if (!ModelState.IsValid) return Page();

        Request.RequestedByUserId = user?.Id;
        if (IsAdmin)
            Request.ConstituencyId = SelectedConstituencyId ?? user?.ConstituencyId ?? 0;
        else
            Request.ConstituencyId = user?.ConstituencyId ?? 0;

        if (Request.ConstituencyId == 0)
        {
            ModelState.AddModelError("", "Please select a constituency before submitting.");
            return Page();
        }

        Request.RequestedAt = DateTime.UtcNow;
        Request.Status = TransportStatus.Pending;
        _db.VoterTransportRequests.Add(Request);
        await _db.SaveChangesAsync();
        TempData["Message"] = "Transport request added.";
        return RedirectToPage("/Transport/Index");
    }
}
