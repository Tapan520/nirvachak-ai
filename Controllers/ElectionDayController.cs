using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Nirvachak_AI.Domain.Enums;
using Nirvachak_AI.Hubs;
using Nirvachak_AI.Infrastructure.Data;
using Nirvachak_AI.Infrastructure.Services;
using Nirvachak_AI.Models.Api;

namespace Nirvachak_AI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class ElectionDayController : ApiBaseController
{
    private readonly AppDbContext _db;
    private readonly ElectionDayService _service;
    private readonly IHubContext<ElectionDayHub> _hub;

    public ElectionDayController(
        AppDbContext db,
        ElectionDayService service,
        IHubContext<ElectionDayHub> hub)
    {
        _db = db;
        _service = service;
        _hub = hub;
    }

    /// <summary>Get live turnout stats for all booths</summary>
    [HttpGet("turnout")]
    [ProducesResponseType(typeof(LiveTurnoutResponse), 200)]
    public async Task<IActionResult> GetTurnout()
    {
        var cId = GetConstituencyId() ?? 1;
        var booths = await _service.GetLiveTurnoutAsync(cId);
        var (total, voted, pct) = await _service.GetConstituencyTurnoutAsync(cId);

        var items = booths
            .Select(b => new BoothTurnoutItem(
                b.BoothNumber, b.BoothName, b.TotalVoters, b.VotedCount, b.TurnoutPercent))
            .ToList();

        return Ok(new LiveTurnoutResponse(total, voted, pct, items));
    }

    /// <summary>Mark a voter as voted (election day � booth agent)</summary>
    [HttpPost("mark-voted")]
    [ProducesResponseType(typeof(ApiResult), 200)]
    public async Task<IActionResult> MarkVoted([FromBody] MarkVotedRequest req)
    {
        var cId = GetConstituencyId() ?? 1;
        var voter = await _db.Voters.FindAsync(req.VoterId);
        if (voter is null) return NotFound(new ApiResult(false, "Voter not found."));

        var success = await _service.MarkVotedAsync(req.VoterId);
        if (!success) return BadRequest(new ApiResult(false, "Could not mark voter."));

        // Broadcast live update via SignalR
        var booth = _db.Booths.FirstOrDefault(b =>
            b.BoothNumber == voter.BoothNumber && b.ConstituencyId == cId);
        if (booth is not null)
            await ElectionDayHub.BroadcastTurnoutUpdate(
                _hub, cId, booth.BoothNumber, booth.VotedCount, booth.TotalVoters);

        return Ok(new ApiResult(true, $"{voter.Name} marked as voted."));
    }

    /// <summary>Mark a voter as absent</summary>
    [HttpPost("mark-absent")]
    [ProducesResponseType(typeof(ApiResult), 200)]
    public async Task<IActionResult> MarkAbsent([FromBody] MarkVotedRequest req)
    {
        var voter = await _db.Voters.FindAsync(req.VoterId);
        if (voter is null) return NotFound(new ApiResult(false, "Voter not found."));
        await _service.MarkAbsentAsync(req.VoterId);
        return Ok(new ApiResult(true, $"{voter.Name} marked as absent."));
    }

    /// <summary>Get live turnout stats for a constituency (public quick-access endpoint)</summary>
    [HttpGet("stats")]
    [AllowAnonymous]
    [ProducesResponseType(200)]
    public async Task<IActionResult> GetStats([FromQuery] int constituencyId)
    {
        var total   = await _db.Voters.CountAsync(v => v.ConstituencyId == constituencyId);
        var voted   = await _db.Voters.CountAsync(v => v.ConstituencyId == constituencyId
            && v.ElectionDayStatus == ElectionDayStatus.Voted);
        var percent = total > 0 ? Math.Round((double)voted / total * 100, 1) : 0;
        return Ok(new { total, voted, percent });
    }
}
