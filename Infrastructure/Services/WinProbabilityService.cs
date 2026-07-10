using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Nirvachak_AI.Domain.Enums;
using Nirvachak_AI.Infrastructure.Data;

namespace Nirvachak_AI.Infrastructure.Services;

public record WinProbabilityResult(
    double Score,
    string Tier,
    string TierColor,
    int    TotalVoters,
    int    FavourVoters,
    int    FloatingVoters,
    int    AgainstVoters,
    int    ContactedVoters,
    double ContactCoverage,
    double FavourRate,
    double FloatingConversionPotential,
    int    EstimatedWinVotes,
    int    BoothsAtRisk,
    List<string> StrengthPoints,
    List<string> RiskPoints
);

public class WinProbabilityService
{
    private readonly AppDbContext _db;
    private readonly PredictiveAnalyticsService _predictive;
    private readonly IMemoryCache _cache;

    public WinProbabilityService(AppDbContext db, PredictiveAnalyticsService predictive, IMemoryCache cache)
    {
        _db        = db;
        _predictive = predictive;
        _cache      = cache;
    }

    public async Task<WinProbabilityResult> ComputeAsync(int constituencyId)
    {
        var cacheKey = $"winprob_{constituencyId}";
        if (_cache.TryGetValue(cacheKey, out WinProbabilityResult? cached) && cached != null)
            return cached;

        var result = await ComputeInternalAsync(constituencyId);
        _cache.Set(cacheKey, result, TimeSpan.FromMinutes(5));
        return result;
    }

    private async Task<WinProbabilityResult> ComputeInternalAsync(int constituencyId)
    {
        // Single DB round-trip — aggregate sentiment + contacted flag in memory
        var voterAgg = await _db.Voters
            .Where(v => v.ConstituencyId == constituencyId && !v.IsDeleted)
            .Select(v => new { v.Sentiment, Contacted = v.LastContactedAt != null })
            .ToListAsync();

        var total     = voterAgg.Count;
        var favour    = voterAgg.Count(v => v.Sentiment == VoterSentiment.Favour);
        var against   = voterAgg.Count(v => v.Sentiment == VoterSentiment.Against);
        var floating  = voterAgg.Count(v => v.Sentiment == VoterSentiment.Floating);
        var neutral   = voterAgg.Count(v => v.Sentiment == VoterSentiment.Neutral);
        var contacted = voterAgg.Count(v => v.Contacted);

        if (total == 0)
            return EmptyResult();

        double favourRate  = (double)favour  / total * 100;
        double contactRate = (double)contacted / total * 100;
        double floatConvert = floating * 0.5;

        var predictions     = await _predictive.GetPredictionsAsync(constituencyId);
        double predTurnout  = predictions.PredictedOverallTurnout;
        double predSupport  = predictions.PredictedOverallSupport;
        int    atRiskBooths = predictions.AtRiskBoothCount;

        // ?? Weighted scoring (max 100 pts) ???????????????????????
        // W1: Favour share of total voters  (35 pts)
        double w1 = Math.Min(35.0, favourRate / 50.0 * 35.0);
        // W2: Contact coverage              (20 pts)
        double w2 = Math.Min(20.0, contactRate * 0.20);
        // W3: Predicted support %           (25 pts)
        double w3 = Math.Min(25.0, predSupport * 0.25);
        // W4: Predicted turnout             (10 pts)
        double w4 = Math.Min(10.0, predTurnout * 0.10);
        // W5: At-risk booth penalty         (-5 pts each, max -15)
        double w5 = -Math.Min(15.0, atRiskBooths * 5.0);
        // W6: Floating conversion bonus     (10 pts)
        double w6 = total > 0 ? Math.Min(10.0, floatConvert / total * 100) : 0;

        double score = Math.Clamp(Math.Round(w1 + w2 + w3 + w4 + w5 + w6, 1), 0, 100);

        var (tier, tierColor) = score switch
        {
            >= 70 => ("Strong",   "success"),
            >= 50 => ("Moderate", "warning"),
            >= 30 => ("Weak",     "danger"),
            _     => ("Critical", "danger")
        };

        int estimatedVotes = (int)Math.Round((favour + floatConvert) * (predTurnout / 100.0));

        var strengths = new List<string>();
        var risks     = new List<string>();

        if (favourRate >= 40)       strengths.Add($"Strong favour base — {favour:N0} voters ({favourRate:F1}%) are In Favour.");
        if (contactRate >= 60)      strengths.Add($"High contact coverage at {contactRate:F1}% — most voters have been reached.");
        if (floating > 50)          strengths.Add($"{floating:N0} floating voters represent a conversion opportunity (+{(int)floatConvert} potential votes).");
        if (predSupport >= 50)      strengths.Add($"Predicted support is {predSupport:F1}% — above the majority threshold.");
        if (neutral > 100)          strengths.Add($"{neutral:N0} neutral voters can still be influenced with targeted outreach.");

        if (favourRate < 30)        risks.Add($"Low favour rate ({favourRate:F1}%) — door-to-door outreach needs urgent acceleration.");
        if (contactRate < 40)       risks.Add($"Only {contactRate:F1}% of voters contacted — {total - contacted:N0} voters remain unreached.");
        if (atRiskBooths > 0)       risks.Add($"{atRiskBooths} booth(s) flagged as at-risk for low turnout — assign more volunteers.");
        if (against > favour)       risks.Add($"Against voters ({against:N0}) outnumber Favour voters ({favour:N0}) — sentiment reversal needed.");
        if (predTurnout < 55)       risks.Add($"Predicted turnout is low ({predTurnout:F1}%) — voter mobilisation must intensify.");

        return new WinProbabilityResult(
            Score:                       score,
            Tier:                        tier,
            TierColor:                   tierColor,
            TotalVoters:                 total,
            FavourVoters:                favour,
            FloatingVoters:              floating,
            AgainstVoters:               against,
            ContactedVoters:             contacted,
            ContactCoverage:             Math.Round(contactRate, 1),
            FavourRate:                  Math.Round(favourRate, 1),
            FloatingConversionPotential: Math.Round(floatConvert, 0),
            EstimatedWinVotes:           estimatedVotes,
            BoothsAtRisk:                atRiskBooths,
            StrengthPoints:              strengths,
            RiskPoints:                  risks
        );
    }

    private static WinProbabilityResult EmptyResult() => new(
        0, "Critical", "danger", 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        new List<string>(),
        new List<string> { "No voter data found for this constituency." });
}
