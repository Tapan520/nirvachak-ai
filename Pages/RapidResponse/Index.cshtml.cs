using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Nirvachak_AI.Domain.Entities;
using Nirvachak_AI.Domain.Enums;
using Nirvachak_AI.Infrastructure.Data;

namespace Nirvachak_AI.Pages.RapidResponse;

[Authorize(Roles = "Admin,SuperAdmin,CampaignManager,Candidate")]
public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _userManager;
    public IndexModel(AppDbContext db, UserManager<AppUser> userManager) { _db = db; _userManager = userManager; }

    public List<RapidResponseItem> Items { get; set; } = new();
    public List<Constituency> Constituencies { get; set; } = new();
    public bool IsAdmin { get; set; }

    [BindProperty(SupportsGet = true)] public int? ConstituencyFilter { get; set; }
    [BindProperty(SupportsGet = true)] public string? StatusFilter { get; set; }

    public async Task OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        IsAdmin = user?.Role == UserRole.Admin || user?.Role == UserRole.SuperAdmin;
        if (IsAdmin) Constituencies = await _db.Constituencies.OrderBy(c => c.Name).ToListAsync();

        var q = _db.RapidResponseItems.AsQueryable();
        if (IsAdmin) { if (ConstituencyFilter.HasValue) q = q.Where(r => r.ConstituencyId == ConstituencyFilter); }
        else if (user?.ConstituencyId.HasValue == true) q = q.Where(r => r.ConstituencyId == user.ConstituencyId);

        if (!string.IsNullOrEmpty(StatusFilter) && Enum.TryParse<RapidResponseStatus>(StatusFilter, out var st))
            q = q.Where(r => r.Status == st);

        Items = await q.OrderByDescending(r => r.ThreatLevel).ThenByDescending(r => r.DetectedAt).ToListAsync();
    }

    public async Task<IActionResult> OnPostUpdateStatusAsync(int id, RapidResponseStatus status)
    {
        var item = await _db.RapidResponseItems.FindAsync(id);
        if (item != null)
        {
            item.Status = status;
            if (status == RapidResponseStatus.Resolved) item.ResolvedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
        TempData["Message"] = "Status updated.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var item = await _db.RapidResponseItems.FindAsync(id);
        if (item != null) { _db.RapidResponseItems.Remove(item); await _db.SaveChangesAsync(); }
        TempData["Message"] = "Item removed.";
        return RedirectToPage();
    }
}
