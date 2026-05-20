using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nirvachak_AI.Domain.Entities;
using Nirvachak_AI.Domain.Enums;
using Nirvachak_AI.Infrastructure.Data;
using Nirvachak_AI.Models.Api;

namespace Nirvachak_AI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class VoterConsentController : ApiBaseController
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _userManager;

    public VoterConsentController(AppDbContext db, UserManager<AppUser> userManager)
    {
        _db          = db;
        _userManager = userManager;
    }

    // Build a voter query scoped to constituency + assigned booths + optional filters
    private async Task<IQueryable<Voter>> BuildVoterQueryAsync(int? filterBooth, string? filterWard)
    {
        var cId  = GetConstituencyId();
        var user = await _userManager.FindByIdAsync(GetUserId());

        HashSet<int>? assignedBooths = null;
        if (user?.Role is UserRole.FieldWorker or UserRole.BoothAgent)
        {
            assignedBooths = (user.AssignedBoothNumbers ?? "")
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => int.TryParse(s.Trim(), out var n) ? (int?)n : null)
                .Where(n => n.HasValue).Select(n => n!.Value)
                .ToHashSet();
        }

        IQueryable<Voter> q = _db.Voters.Where(v => !v.IsDeleted);
        if (cId.HasValue)
            q = q.Where(v => v.ConstituencyId == cId.Value);
        if (assignedBooths != null && assignedBooths.Count > 0)
            q = q.Where(v => assignedBooths.Contains(v.BoothNumber));
        if (filterBooth.HasValue)
            q = q.Where(v => v.BoothNumber == filterBooth.Value);
        if (!string.IsNullOrEmpty(filterWard))
            q = q.Where(v => v.WardNumber == filterWard);

        return q;
    }

    /// <summary>Summary stats: completion rate, consent counts, booth/ward lists</summary>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(VoterConsentStatsResponse), 200)]
    public async Task<IActionResult> GetStats(
        [FromQuery] int? booth, [FromQuery] string? ward)
    {
        var voterQ   = await BuildVoterQueryAsync(booth, ward);
        var voterIds = await voterQ.Select(v => v.Id).ToListAsync();
        var total    = voterIds.Count;

        var completions = await _db.SurveyCompletions
            .Where(c => voterIds.Contains(c.VoterId))
            .AsNoTracking().ToListAsync();

        var completed = completions.Count;
        var pending   = total - completed;
        var rate      = total > 0 ? Math.Round((double)completed / total * 100, 1) : 0;

        var couponsIssued = completions.Count(c => c.CouponId.HasValue);
        var couponIds     = completions.Where(c => c.CouponId.HasValue)
                                       .Select(c => c.CouponId!.Value).ToList();
        var couponsRedeemed = await _db.CouponPools
            .Where(cp => couponIds.Contains(cp.Id))
            .CountAsync(cp => cp.IsRedeemed);

        var consents = await _db.VoterConsents
            .Where(c => voterIds.Contains(c.VoterId))
            .AsNoTracking().ToListAsync();

        var boothList = await voterQ.Select(v => v.BoothNumber).Distinct()
                            .OrderBy(b => b).ToListAsync();
        var wardList  = await voterQ.Where(v => v.WardNumber != null)
                            .Select(v => v.WardNumber!).Distinct()
                            .OrderBy(w => w).ToListAsync();

        var boothMap = await voterQ
                           .Select(v => new { v.Id, v.BoothNumber })
                           .ToDictionaryAsync(v => v.Id, v => v.BoothNumber);
        var byBooth = completions
            .Where(c => boothMap.ContainsKey(c.VoterId))
            .GroupBy(c => boothMap[c.VoterId])
            .Select(g => new BoothSurveyCount(g.Key, g.Count()))
            .OrderBy(b => b.BoothNumber)
            .ToList();

        return Ok(new VoterConsentStatsResponse(
            TotalVoters:       total,
            CompletedCount:    completed,
            PendingCount:      pending,
            CompletionRate:    rate,
            CouponsIssued:     couponsIssued,
            CouponsRedeemed:   couponsRedeemed,
            ConsentThirdParty: consents.Count(c => c.AllowThirdPartyAdvertising),
            ConsentCampaign:   consents.Count(c => c.AllowCampaignOutreach),
            ConsentWhatsApp:   consents.Count(c => c.AllowWhatsAppMessages),
            ConsentScheme:     consents.Count(c => c.AllowSchemeNotifications),
            ConsentAnalytics:  consents.Count(c => c.AllowDataForAnalytics),
            AvailableBooths:   boothList,
            AvailableWards:    wardList,
            CompletionByBooth: byBooth));
    }

    /// <summary>Paginated completed voters (searchable by name / EPIC / mobile)</summary>
    [HttpGet("completed")]
    [ProducesResponseType(typeof(PagedResult<SurveyCompletedVoter>), 200)]
    public async Task<IActionResult> GetCompleted(
        [FromQuery] int? booth, [FromQuery] string? ward,
        [FromQuery] string? search,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 30)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);

        var voterQ   = await BuildVoterQueryAsync(booth, ward);
        var voterIds = await voterQ.Select(v => v.Id).ToListAsync();

        var completions = await _db.SurveyCompletions
            .Where(c => voterIds.Contains(c.VoterId))
            .AsNoTracking().ToListAsync();

        var completedIds  = completions.Select(c => c.VoterId).ToHashSet();
        var couponIds     = completions.Where(c => c.CouponId.HasValue)
                                       .Select(c => c.CouponId!.Value).ToList();
        var couponMap     = await _db.CouponPools
            .Where(cp => couponIds.Contains(cp.Id))
            .ToDictionaryAsync(cp => cp.Id, cp => cp.CouponCode);
        var completionMap = completions.ToDictionary(c => c.VoterId, c => c);

        var completedQ = voterQ.Where(v => completedIds.Contains(v.Id));
        if (!string.IsNullOrWhiteSpace(search))
            completedQ = completedQ.Where(v =>
                v.Name.Contains(search) ||
                v.VoterId.Contains(search) ||
                (v.MobileNumber != null && v.MobileNumber.Contains(search)));

        var total  = await completedQ.CountAsync();
        var voters = await completedQ
            .OrderBy(v => v.BoothNumber).ThenBy(v => v.Name)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(v => new { v.Id, v.Name, v.VoterId, v.MobileNumber, v.BoothNumber, v.WardNumber })
            .ToListAsync();

        var items = voters.Select(v =>
        {
            completionMap.TryGetValue(v.Id, out var comp);
            var couponId   = comp?.CouponId;
            var couponCode = couponId.HasValue && couponMap.TryGetValue(couponId.Value, out var code) ? code : null;
            return new SurveyCompletedVoter(
                v.Id, v.Name, v.VoterId, v.MobileNumber,
                v.BoothNumber, v.WardNumber,
                comp?.CompletedAt ?? DateTime.MinValue,
                couponId.HasValue, couponCode);
        }).ToList();

        return Ok(new PagedResult<SurveyCompletedVoter>(
            items, total, page, pageSize,
            (int)Math.Ceiling((double)total / pageSize)));
    }

    /// <summary>Paginated pending voters — have NOT yet completed the survey</summary>
    [HttpGet("pending")]
    [ProducesResponseType(typeof(PagedResult<SurveyPendingVoter>), 200)]
    public async Task<IActionResult> GetPending(
        [FromQuery] int? booth, [FromQuery] string? ward,
        [FromQuery] string? search,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 30)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);

        var voterQ   = await BuildVoterQueryAsync(booth, ward);
        var voterIds = await voterQ.Select(v => v.Id).ToListAsync();

        var completedIds = (await _db.SurveyCompletions
            .Where(c => voterIds.Contains(c.VoterId))
            .Select(c => c.VoterId)
            .ToListAsync()).ToHashSet();

        var pendingQ = voterQ.Where(v => !completedIds.Contains(v.Id));
        if (!string.IsNullOrWhiteSpace(search))
            pendingQ = pendingQ.Where(v =>
                v.Name.Contains(search) ||
                v.VoterId.Contains(search) ||
                (v.MobileNumber != null && v.MobileNumber.Contains(search)));

        var total = await pendingQ.CountAsync();
        var items = await pendingQ
            .OrderBy(v => v.BoothNumber).ThenBy(v => v.Name)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(v => new SurveyPendingVoter(
                v.Id, v.Name, v.VoterId, v.MobileNumber, v.BoothNumber, v.WardNumber))
            .ToListAsync();

        return Ok(new PagedResult<SurveyPendingVoter>(
            items, total, page, pageSize,
            (int)Math.Ceiling((double)total / pageSize)));
    }
}
