using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nirvachak_AI.Infrastructure.Data;

namespace Nirvachak_AI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin,SuperAdmin,CampaignManager")]
public class RewardsController : ApiBaseController
{
    private readonly AppDbContext _db;
    public RewardsController(AppDbContext db) => _db = db;

    public record RewardDto(
        int Id, string Title, string? PartnerBrand, string? Description,
        DateTime ExpiryDate, bool IsActive,
        int TotalCoupons, int IssuedCount, int RedeemedCount);

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var cId = GetConstituencyId();
        var q = _db.RewardConfigs.Include(r => r.Coupons).AsQueryable();
        if (cId.HasValue) q = q.Where(r => r.ConstituencyId == cId.Value);

        var rewards = await q
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new RewardDto(
                r.Id, r.Title, r.PartnerBrand, r.Description,
                r.ExpiryDate, r.IsActive,
                r.Coupons.Count,
                r.Coupons.Count(c => c.IsIssued),
                r.Coupons.Count(c => c.IsRedeemed)))
            .ToListAsync();

        return Ok(rewards);
    }

    [HttpPost("{id:int}/toggle")]
    public async Task<IActionResult> Toggle(int id)
    {
        var cId = GetConstituencyId();
        var r = await _db.RewardConfigs.FindAsync(id);
        if (r == null) return NotFound();
        if (cId.HasValue && r.ConstituencyId != cId.Value) return Forbid();

        r.IsActive = !r.IsActive;
        await _db.SaveChangesAsync();
        return Ok(new { isActive = r.IsActive });
    }
}
