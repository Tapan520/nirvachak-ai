using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Nirvachak_AI.Domain.Entities;
using Nirvachak_AI.Domain.Enums;
using Nirvachak_AI.Infrastructure.Data;

namespace Nirvachak_AI.Pages.Admin.Rewards;

[Authorize(Roles = "Admin,CampaignManager,SuperAdmin")]
public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _userManager;

    public IndexModel(AppDbContext db, UserManager<AppUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public List<RewardSummary> Rewards { get; set; } = new();

    public record RewardSummary(
        int Id, string Title, string? PartnerBrand, DateTime ExpiryDate,
        bool IsActive, int TotalCoupons, int IssuedCount, int RedeemedCount);

    public async Task OnGetAsync()
    {
        var currentUser = await _userManager.GetUserAsync(User);
        bool isSuperAdmin = User.IsInRole(nameof(UserRole.SuperAdmin));

        IQueryable<RewardConfig> query = _db.RewardConfigs
            .Include(r => r.Coupons)
            .OrderByDescending(r => r.CreatedAt);

        if (!isSuperAdmin && currentUser?.ConstituencyId != null)
            query = query.Where(r => r.ConstituencyId == currentUser.ConstituencyId);

        Rewards = await query
            .Select(r => new RewardSummary(
                r.Id, r.Title, r.PartnerBrand, r.ExpiryDate, r.IsActive,
                r.Coupons.Count,
                r.Coupons.Count(c => c.IsIssued),
                r.Coupons.Count(c => c.IsRedeemed)))
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostToggleAsync(int id)
    {
        var currentUser = await _userManager.GetUserAsync(User);
        bool isSuperAdmin = User.IsInRole(nameof(UserRole.SuperAdmin));
        var config = await _db.RewardConfigs.FindAsync(id);
        if (config is not null)
        {
            if (!isSuperAdmin && config.ConstituencyId != currentUser?.ConstituencyId)
                return Forbid();
            config.IsActive = !config.IsActive;
            await _db.SaveChangesAsync();
            TempData["Message"] = $"Reward '{config.Title}' has been {(config.IsActive ? "activated" : "deactivated")}.";
        }
        return RedirectToPage();
    }
}
