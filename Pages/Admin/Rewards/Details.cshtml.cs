using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Nirvachak_AI.Domain.Entities;
using Nirvachak_AI.Infrastructure.Data;

namespace Nirvachak_AI.Pages.Admin.Rewards;

[Authorize(Roles = "Admin,CampaignManager,SuperAdmin")]
public class DetailsModel : PageModel
{
    private readonly AppDbContext _db;

    public DetailsModel(AppDbContext db) => _db = db;

    public RewardConfig? Reward { get; set; }
    public List<CouponPool> Coupons { get; set; } = new();
    public int TotalCoupons { get; set; }
    public int IssuedCount { get; set; }
    public int RedeemedCount { get; set; }
    public int AvailableCount { get; set; }

    [BindProperty(SupportsGet = true)]
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; }
    private const int PageSize = 50;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Reward = await _db.RewardConfigs
            .Include(r => r.Constituency)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (Reward is null) return NotFound();

        TotalCoupons   = await _db.CouponPools.CountAsync(c => c.RewardConfigId == id);
        IssuedCount    = await _db.CouponPools.CountAsync(c => c.RewardConfigId == id && c.IsIssued);
        RedeemedCount  = await _db.CouponPools.CountAsync(c => c.RewardConfigId == id && c.IsRedeemed);
        AvailableCount = TotalCoupons - IssuedCount;
        TotalPages     = (int)Math.Ceiling((double)TotalCoupons / PageSize);

        Coupons = await _db.CouponPools
            .Where(c => c.RewardConfigId == id)
            .Include(c => c.IssuedToVoter)
            .OrderBy(c => c.Id)
            .Skip((CurrentPage - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();

        return Page();
    }

    public async Task<IActionResult> OnPostMarkRedeemedAsync(int couponId, int rewardId)
    {
        var coupon = await _db.CouponPools.FindAsync(couponId);
        if (coupon is not null && coupon.IsIssued && !coupon.IsRedeemed)
        {
            coupon.IsRedeemed  = true;
            coupon.RedeemedAt  = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            TempData["Message"] = $"Coupon {coupon.CouponCode} marked as redeemed.";
        }
        return RedirectToPage(new { id = rewardId });
    }
}
