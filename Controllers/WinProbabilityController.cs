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
    Roles = "Admin,SuperAdmin,CampaignManager,Candidate")]
public class WinProbabilityController : ApiBaseController
{
    private readonly WinProbabilityService _svc;

    public WinProbabilityController(WinProbabilityService svc) => _svc = svc;

    /// <summary>
    /// Returns the computed win-probability score and analysis for the
    /// caller's constituency.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(WinProbabilityResponse), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Get()
    {
        var cId = GetConstituencyId();
        if (!cId.HasValue)
            return BadRequest(new ApiResult(false, "Constituency not assigned to your account."));

        var r = await _svc.ComputeAsync(cId.Value);

        return Ok(new WinProbabilityResponse(
            r.Score, r.Tier, r.TierColor,
            r.TotalVoters, r.FavourVoters, r.FloatingVoters,
            r.AgainstVoters, r.ContactedVoters,
            r.ContactCoverage, r.FavourRate,
            r.FloatingConversionPotential, r.EstimatedWinVotes,
            r.BoothsAtRisk,
            r.StrengthPoints, r.RiskPoints));
    }
}
