using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nirvachak_AI.Infrastructure.Services;
using Nirvachak_AI.Models.Api;

namespace Nirvachak_AI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(
    AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
    Roles = "Admin,CampaignManager,Candidate")]
public class PredictiveAnalyticsController : ApiBaseController
{
    private readonly PredictiveAnalyticsService _svc;

    public PredictiveAnalyticsController(PredictiveAnalyticsService svc) => _svc = svc;

    /// <summary>
    /// Returns AI-powered turnout and support predictions for all booths in
    /// the caller's constituency. Restricted to Admin, CampaignManager and Candidate.
    /// </summary>
    [HttpGet("predictions")]
    [ProducesResponseType(typeof(PredictionSummaryResponse), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> GetPredictions()
    {
        var cId = GetConstituencyId();
        if (!cId.HasValue)
            return BadRequest(new ApiResult(false, "Constituency not assigned to your account."));

        var summary = await _svc.GetPredictionsAsync(cId.Value);

        var response = new PredictionSummaryResponse(
            summary.TotalVoters,
            summary.TotalContacted,
            summary.TotalFavour,
            summary.TotalFloating,
            summary.PredictedOverallTurnout,
            summary.PredictedOverallSupport,
            summary.EstimatedTotalFavourVotes,
            summary.AtRiskBoothCount,
            summary.WeakSupportBoothCount,
            summary.BoothPredictions.Select(b => new BoothPredictionResponse(
                b.BoothNumber,
                b.BoothName,
                b.TotalVoters,
                b.FavourVoters,
                b.AgainstVoters,
                b.FloatingVoters,
                b.ContactedVoters,
                b.RecentVisits,
                b.ContactRate,
                b.PredictedTurnoutPercent,
                b.PredictedSupportPercent,
                b.EstimatedFavourVotes,
                b.TurnoutRisk,
                b.SupportConfidence,
                b.StrategyAlerts
            )).ToList()
        );

        return Ok(response);
    }
}
