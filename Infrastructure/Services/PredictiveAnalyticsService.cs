using Microsoft.EntityFrameworkCore;
using Nirvachak_AI.Domain.Enums;
using Nirvachak_AI.Infrastructure.Data;

namespace Nirvachak_AI.Infrastructure.Services;

// ?? Output DTOs ????????????????????????????????????????????????????????????????

public class BoothPrediction
{
    public int    BoothNumber              { get; set; }
    public string BoothName               { get; set; } = string.Empty;
    public int    TotalVoters             { get; set; }
    public int    FavourVoters            { get; set; }
    public int    AgainstVoters           { get; set; }
    public int    FloatingVoters          { get; set; }
    public int    ContactedVoters         { get; set; }
    public int    RecentVisits            { get; set; }   // last 7 days
    public double ContactRate             { get; set; }   // 0–1
    public double PredictedTurnoutPercent { get; set; }
    public double PredictedSupportPercent { get; set; }
    public int    EstimatedFavourVotes    { get; set; }
    public string TurnoutRisk             { get; set; } = string.Empty;   // Low | Medium | High
    public string SupportConfidence       { get; set; } = string.Empty;   // Weak | Moderate | Strong
    public List<string> StrategyAlerts   { get; set; } = new();
}

public class PredictionSummary
{
    public int    TotalVoters                { get; set; }
    public int    TotalContacted             { get; set; }
    public int    TotalFavour                { get; set; }
    public int    TotalFloating              { get; set; }
    public double PredictedOverallTurnout    { get; set; }
    public double PredictedOverallSupport    { get; set; }
    public int    EstimatedTotalFavourVotes  { get; set; }
    public int    AtRiskBoothCount           { get; set; }   // TurnoutRisk == High
    public int    WeakSupportBoothCount      { get; set; }   // SupportConfidence == Weak
    public List<BoothPrediction> BoothPredictions { get; set; } = new();
}

// ?? Service ????????????????????????????????????????????????????????????????????

public class PredictiveAnalyticsService
{
    private readonly AppDbContext _db;

    public PredictiveAnalyticsService(AppDbContext db) => _db = db;

    public async Task<PredictionSummary> GetPredictionsAsync(int constituencyId)
    {
        var booths = await _db.Booths
            .Where(b => b.ConstituencyId == constituencyId)
            .OrderBy(b => b.BoothNumber)
            .ToListAsync();

        var voters = await _db.Voters
            .Where(v => v.ConstituencyId == constituencyId && !v.IsDeleted)
            .Select(v => new
            {
                v.Id, v.BoothNumber, v.Sentiment, v.LastContactedAt
            })
            .ToListAsync();

        // Recent visits (last 7 days): load visits for this constituency's voters
        var recentCutoff = DateTime.UtcNow.AddDays(-7);
        var voterIdSet   = voters.Select(v => v.Id).ToHashSet();

        var recentVisits = await _db.DoorToDoorVisits
            .Where(d => d.VisitedAt >= recentCutoff && voterIdSet.Contains(d.VoterId))
            .Select(d => new { d.VoterId })
            .ToListAsync();

        // Index: boothNumber ? recentVisitCount
        var voterBoothMap      = voters.ToDictionary(v => v.Id, v => v.BoothNumber);
        var recentByBooth      = recentVisits
            .GroupBy(d => voterBoothMap.GetValueOrDefault(d.VoterId, 0))
            .ToDictionary(g => g.Key, g => g.Count());

        var predictions = new List<BoothPrediction>();

        foreach (var booth in booths)
        {
            var bv = voters.Where(v => v.BoothNumber == booth.BoothNumber).ToList();
            if (!bv.Any()) continue;

            int total     = bv.Count;
            int favour    = bv.Count(v => v.Sentiment == VoterSentiment.Favour);
            int against   = bv.Count(v => v.Sentiment == VoterSentiment.Against);
            int floating  = bv.Count(v => v.Sentiment == VoterSentiment.Floating);
            int contacted = bv.Count(v => v.LastContactedAt.HasValue);
            int recent    = recentByBooth.GetValueOrDefault(booth.BoothNumber, 0);

            double contactRate = total > 0 ? (double)contacted / total : 0;

            // ?? Turnout forecast ??????????????????????????????????????????????
            // Base: 60% (Indian MLA historical average)
            // Coverage bonus/penalty: each 10% above 50% adds 5%; below 20% subtracts 5%
            // Momentum: recent visits add up to 2%
            double baseTurnout    = 60.0;
            double coverageAdj    = contactRate >= 0.5
                                    ? (contactRate - 0.5) * 50.0
                                    : contactRate < 0.2 ? -5.0 : 0.0;
            double momentumAdj    = recent >= 10 ? 2.0 : recent >= 5 ? 1.0 : 0.0;
            double predictedTurnout = Math.Clamp(baseTurnout + coverageAdj + momentumAdj, 20.0, 90.0);

            // ?? Support forecast ??????????????????????????????????????????????
            // Effective favour = confirmed + 40% of floating (convertible)
            // Shrink toward 50% uncertainty when contact rate is low
            double effectiveFavour = favour + (floating * 0.4);
            double supportRaw      = contacted > 0
                                     ? effectiveFavour / Math.Max(contacted, 1)
                                     : effectiveFavour / Math.Max(total, 1);
            double certainty       = Math.Min(contactRate * 2.0, 1.0);
            double predictedSupport = Math.Clamp(
                supportRaw * certainty + 0.5 * (1.0 - certainty), 0.05, 0.95);

            int estFavourVotes = (int)Math.Round(total * (predictedTurnout / 100.0) * predictedSupport);

            // ?? Risk classification ???????????????????????????????????????????
            string turnoutRisk = predictedTurnout < 45 ? "High"
                               : predictedTurnout < 60 ? "Medium" : "Low";

            string supportConf = contactRate > 0.6 ? "Strong"
                               : contactRate > 0.3 ? "Moderate" : "Weak";

            // ?? Strategy alerts ???????????????????????????????????????????????
            var alerts = new List<string>();
            if (contactRate < 0.3)
                alerts.Add($"Only {contactRate:P0} of voters contacted — urgently increase outreach.");
            if (floating > favour)
                alerts.Add($"{floating} floating voters outnumber {favour} favour voters — focus on conversion.");
            if (against > favour)
                alerts.Add($"Against ({against}) exceeds Favour ({favour}) — damage control required.");
            if (recent == 0)
                alerts.Add("No door-to-door visits in the last 7 days — momentum has stalled.");
            if (predictedTurnout < 45)
                alerts.Add("Low predicted turnout — mobilise transport and send reminders to favour voters.");

            predictions.Add(new BoothPrediction
            {
                BoothNumber              = booth.BoothNumber,
                BoothName                = booth.BoothName,
                TotalVoters              = total,
                FavourVoters             = favour,
                AgainstVoters            = against,
                FloatingVoters           = floating,
                ContactedVoters          = contacted,
                RecentVisits             = recent,
                ContactRate              = Math.Round(contactRate * 100, 1),
                PredictedTurnoutPercent  = Math.Round(predictedTurnout, 1),
                PredictedSupportPercent  = Math.Round(predictedSupport * 100, 1),
                EstimatedFavourVotes     = estFavourVotes,
                TurnoutRisk              = turnoutRisk,
                SupportConfidence        = supportConf,
                StrategyAlerts           = alerts
            });
        }

        int sumVoters = predictions.Sum(p => p.TotalVoters);

        double weightedTurnout = sumVoters > 0
            ? predictions.Sum(p => p.PredictedTurnoutPercent * p.TotalVoters) / sumVoters : 0;

        double weightedSupport = sumVoters > 0
            ? predictions.Sum(p => p.PredictedSupportPercent * p.TotalVoters) / sumVoters : 0;

        return new PredictionSummary
        {
            TotalVoters               = sumVoters,
            TotalContacted            = predictions.Sum(p => p.ContactedVoters),
            TotalFavour               = predictions.Sum(p => p.FavourVoters),
            TotalFloating             = predictions.Sum(p => p.FloatingVoters),
            PredictedOverallTurnout   = Math.Round(weightedTurnout, 1),
            PredictedOverallSupport   = Math.Round(weightedSupport, 1),
            EstimatedTotalFavourVotes = predictions.Sum(p => p.EstimatedFavourVotes),
            AtRiskBoothCount          = predictions.Count(p => p.TurnoutRisk == "High"),
            WeakSupportBoothCount     = predictions.Count(p => p.SupportConfidence == "Weak"),
            BoothPredictions          = predictions
        };
    }
}
