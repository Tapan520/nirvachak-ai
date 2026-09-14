using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nirvachak_AI.Infrastructure.Data;
using System.Text.Json;

namespace Nirvachak_AI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class SurveyDemographicsController : ApiBaseController
{
    private readonly AppDbContext _db;
    public SurveyDemographicsController(AppDbContext db) => _db = db;

    public record BucketDto(string Label, int Count);

    public record DemographicsResponse(
        int TotalVoters,
        int CompletedCount,
        int PendingCount,
        double CompletionRate,
        int CouponsIssued,
        int CouponsRedeemed,
        int ConsentThirdParty,
        int ConsentCampaign,
        int ConsentWhatsApp,
        int ConsentScheme,
        int ConsentAnalytics,
        List<BucketDto> ByCaste,
        List<BucketDto> ByReligion,
        List<BucketDto> ByEducation,
        List<BucketDto> ByOccupation,
        List<BucketDto> ByIncome,
        List<BucketDto> ByAge,
        List<BucketDto> TopConcerns);

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] int? booth = null, [FromQuery] string? ward = null)
    {
        var cId = GetConstituencyId();
        if (!cId.HasValue)
            return Ok(Empty());

        var vq = _db.Voters.Where(v => !v.IsDeleted && v.ConstituencyId == cId.Value);
        if (booth.HasValue) vq = vq.Where(v => v.BoothNumber == booth.Value);
        if (!string.IsNullOrWhiteSpace(ward)) vq = vq.Where(v => v.WardNumber == ward);

        var voterIds = await vq.Select(v => v.Id).ToListAsync();
        var total = voterIds.Count;
        if (total == 0) return Ok(Empty());

        var completions = await _db.SurveyCompletions
            .Where(c => voterIds.Contains(c.VoterId))
            .ToListAsync();

        var couponIds = completions.Where(c => c.CouponId.HasValue).Select(c => c.CouponId!.Value).ToList();
        var couponsRedeemed = couponIds.Count == 0 ? 0
            : await _db.CouponPools.Where(cp => couponIds.Contains(cp.Id)).CountAsync(cp => cp.IsRedeemed);

        var consents = await _db.VoterConsents.Where(c => voterIds.Contains(c.VoterId)).ToListAsync();
        var profiles = await _db.VoterProfiles.Where(p => voterIds.Contains(p.VoterId)).ToListAsync();

        var concernCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var p in profiles.Where(p => !string.IsNullOrEmpty(p.PrimaryConcerns)))
        {
            try
            {
                var list = JsonSerializer.Deserialize<List<string>>(p.PrimaryConcerns!) ?? new();
                foreach (var c in list) concernCounts[c] = concernCounts.GetValueOrDefault(c) + 1;
            }
            catch { /* ignore malformed json */ }
        }

        var completedCount = completions.Count;

        return Ok(new DemographicsResponse(
            total,
            completedCount,
            total - completedCount,
            total > 0 ? Math.Round((double)completedCount / total * 100, 1) : 0,
            completions.Count(c => c.CouponId.HasValue),
            couponsRedeemed,
            consents.Count(c => c.AllowThirdPartyAdvertising),
            consents.Count(c => c.AllowCampaignOutreach),
            consents.Count(c => c.AllowWhatsAppMessages),
            consents.Count(c => c.AllowSchemeNotifications),
            consents.Count(c => c.AllowDataForAnalytics),
            Group(profiles, p => p.CasteCategory),
            Group(profiles, p => p.Religion),
            Group(profiles, p => p.Education),
            Group(profiles, p => p.Occupation),
            Group(profiles, p => p.MonthlyIncomeBracket),
            Group(profiles, p => p.AgeBracket),
            concernCounts.OrderByDescending(x => x.Value).Take(10)
                .Select(x => new BucketDto(x.Key, x.Value)).ToList()));
    }

    private static List<BucketDto> Group(
        IEnumerable<Domain.Entities.VoterProfile> src,
        Func<Domain.Entities.VoterProfile, string?> sel) =>
        src.Where(p => !string.IsNullOrEmpty(sel(p)))
           .GroupBy(p => sel(p)!)
           .OrderByDescending(g => g.Count())
           .Select(g => new BucketDto(g.Key, g.Count()))
           .ToList();

    private static DemographicsResponse Empty() =>
        new(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, new(), new(), new(), new(), new(), new(), new());
}
