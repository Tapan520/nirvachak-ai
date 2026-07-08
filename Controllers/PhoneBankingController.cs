using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nirvachak_AI.Domain.Enums;
using Nirvachak_AI.Infrastructure.Data;
using Nirvachak_AI.Models.Api;

namespace Nirvachak_AI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class PhoneBankingController : ApiBaseController
{
    private readonly AppDbContext _db;
    public PhoneBankingController(AppDbContext db) => _db = db;

    /// <summary>Today's call stats + recent calls for the current user</summary>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(PhoneBankingStatsResponse), 200)]
    public async Task<IActionResult> GetStats()
    {
        var userId = GetUserId();
        var role   = GetUserRole();
        var cId    = GetConstituencyId();
        var isSuperAdmin = role == nameof(UserRole.SuperAdmin);
        var todayStart = DateTime.UtcNow.Date;

        IQueryable<Domain.Entities.PhoneCallLog> q = _db.PhoneCallLogs
            .Include(c => c.Voter).AsNoTracking();

        if (!isSuperAdmin) q = q.Where(c => c.CalledByUserId == userId);
        if (!isSuperAdmin && cId.HasValue) q = q.Where(c => c.ConstituencyId == cId.Value);

        var todaysCalls = await q
            .Where(c => c.CalledAt >= todayStart)
            .OrderByDescending(c => c.CalledAt)
            .Take(50)
            .ToListAsync();

        var recent = todaysCalls.Take(20).Select(c => new PhoneCallItem(
            c.Id,
            c.VoterId,
            c.Voter?.Name ?? "Unknown",
            c.Voter?.MobileNumber,
            c.CalledAt,
            c.Outcome.ToString(),
            c.DurationSeconds,
            c.Notes,
            c.SentimentAfterCall?.ToString()
        )).ToList();

        var calledIds = todaysCalls.Select(c => c.VoterId).ToHashSet();

        IQueryable<Domain.Entities.Voter> voterQ = _db.Voters
            .Where(v => !v.IsDeleted && v.MobileNumber != null)
            .AsNoTracking();
        if (!isSuperAdmin && cId.HasValue) voterQ = voterQ.Where(v => v.ConstituencyId == cId.Value);

        var pending = await voterQ
            .Where(v => !calledIds.Contains(v.Id) &&
                (v.Sentiment == VoterSentiment.Floating || v.Sentiment == VoterSentiment.Unknown))
            .OrderBy(v => v.BoothNumber).ThenBy(v => v.SerialNumber)
            .Take(20)
            .Select(v => new PendingCallVoter(
                v.Id, v.Name, v.MobileNumber!, v.BoothNumber,
                v.WardNumber, v.Sentiment.ToString()))
            .ToListAsync();

        var stats = new PhoneBankingStatsResponse(
            TotalCallsToday: todaysCalls.Count,
            TalkedCount:     todaysCalls.Count(c => c.Outcome == CallOutcome.Talked),
            NoAnswerCount:   todaysCalls.Count(c => c.Outcome == CallOutcome.NoAnswer),
            CallBackCount:   todaysCalls.Count(c => c.Outcome == CallOutcome.CallBack),
            RecentCalls:     recent,
            PendingVoters:   pending
        );

        return Ok(stats);
    }

    /// <summary>Log a phone call outcome</summary>
    [HttpPost("log")]
    [ProducesResponseType(typeof(ApiResult), 200)]
    public async Task<IActionResult> LogCall([FromBody] LogPhoneCallRequest req)
    {
        var userId   = GetUserId();
        var userName = GetUserFullName();
        var cId      = GetConstituencyId() ?? 0;

        if (!Enum.TryParse<CallOutcome>(req.Outcome, out var outcome))
            outcome = CallOutcome.NoAnswer;

        VoterSentiment? sentiment = null;
        if (!string.IsNullOrEmpty(req.SentimentAfterCall) &&
            Enum.TryParse<VoterSentiment>(req.SentimentAfterCall, out var s))
            sentiment = s;

        _db.PhoneCallLogs.Add(new Domain.Entities.PhoneCallLog
        {
            VoterId            = req.VoterId,
            CalledByUserId     = userId,
            CalledByName       = userName,
            CalledAt           = DateTime.UtcNow,
            Outcome            = outcome,
            DurationSeconds    = req.DurationSeconds,
            Notes              = req.Notes,
            SentimentAfterCall = sentiment,
            ConstituencyId     = cId,
        });

        var voter = await _db.Voters.FindAsync(req.VoterId);
        if (voter != null)
        {
            voter.LastContactedAt = DateTime.UtcNow;
            if (sentiment.HasValue) voter.Sentiment = sentiment.Value;
        }

        await _db.SaveChangesAsync();
        return Ok(new ApiResult(true, "Call logged successfully."));
    }

    /// <summary>Search voters by name or phone (for quick dial)</summary>
    [HttpGet("search")]
    [ProducesResponseType(typeof(List<PendingCallVoter>), 200)]
    public async Task<IActionResult> SearchVoters([FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q)) return Ok(new List<PendingCallVoter>());

        var cId = GetConstituencyId();
        var role = GetUserRole();
        var isSuperAdmin = role == nameof(UserRole.SuperAdmin);

        IQueryable<Domain.Entities.Voter> voterQ = _db.Voters
            .Where(v => !v.IsDeleted && v.MobileNumber != null &&
                (v.Name.Contains(q) || v.MobileNumber!.Contains(q)))
            .AsNoTracking();

        if (!isSuperAdmin && cId.HasValue) voterQ = voterQ.Where(v => v.ConstituencyId == cId.Value);

        var results = await voterQ.Take(10)
            .Select(v => new PendingCallVoter(
                v.Id, v.Name, v.MobileNumber!, v.BoothNumber,
                v.WardNumber, v.Sentiment.ToString()))
            .ToListAsync();

        return Ok(results);
    }
}
