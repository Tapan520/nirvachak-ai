using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nirvachak_AI.Domain.Enums;
using Nirvachak_AI.Infrastructure.Data;

namespace Nirvachak_AI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class LeaderboardController : ApiBaseController
{
    private readonly AppDbContext _db;
    public LeaderboardController(AppDbContext db) => _db = db;

    public record LeaderboardRowDto(
        string UserId,
        string FullName,
        string Role,
        string? AssignedBooths,
        int Visits,
        int Calls,
        int FavourConversions,
        int TotalScore,
        int TotalVisitsAllTime,
        int TotalCallsAllTime,
        DateTime? LastActivityAt,
        int Rank,
        bool IsCurrentUser);

    public record LeaderboardResponse(string Period, string PeriodLabel, List<LeaderboardRowDto> Rows);

    // GET /api/leaderboard?period=today|week|month|alltime
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string period = "week")
    {
        var cId = GetConstituencyId();
        var currentUserId = GetUserId();

        var (cutoff, label, normalizedPeriod) = ResolvePeriod(period);

        var usersQuery = _db.Users.AsQueryable();
        if (cId.HasValue)
            usersQuery = usersQuery.Where(u => u.ConstituencyId == cId.Value);

        var groundUsers = await usersQuery
            .Where(u => u.IsActive &&
                (u.Role == UserRole.FieldWorker ||
                 u.Role == UserRole.BoothAgent ||
                 u.Role == UserRole.CampaignManager))
            .Select(u => new { u.Id, u.FullName, u.Role, u.AssignedBoothNumbers })
            .ToListAsync();

        if (groundUsers.Count == 0)
            return Ok(new LeaderboardResponse(normalizedPeriod, label, new()));

        var userIds = groundUsers.Select(u => u.Id).ToList();

        var visits = await _db.DoorToDoorVisits
            .Where(v => userIds.Contains(v.WorkerUserId))
            .Select(v => new { v.WorkerUserId, v.VisitedAt, v.SentimentAfterVisit })
            .ToListAsync();

        var calls = await _db.PhoneCallLogs
            .Where(c => userIds.Contains(c.CalledByUserId))
            .Select(c => new { c.CalledByUserId, c.CalledAt })
            .ToListAsync();

        var rows = groundUsers.Select(u =>
        {
            var uVisits = visits.Where(v => v.WorkerUserId == u.Id).ToList();
            var uCalls = calls.Where(c => c.CalledByUserId == u.Id).ToList();

            var periodVisits = uVisits.Where(v => v.VisitedAt >= cutoff).ToList();
            var periodCalls = uCalls.Where(c => c.CalledAt >= cutoff).ToList();
            var favourConv = periodVisits.Count(v => v.SentimentAfterVisit == VoterSentiment.Favour);

            var score = (periodVisits.Count * 2) + (periodCalls.Count * 1) + (favourConv * 3);

            DateTime? lastActivity = null;
            var lv = uVisits.OrderByDescending(v => v.VisitedAt).FirstOrDefault()?.VisitedAt;
            var lc = uCalls.OrderByDescending(c => c.CalledAt).FirstOrDefault()?.CalledAt;
            if (lv.HasValue || lc.HasValue)
                lastActivity = new[] { lv, lc }.Where(d => d.HasValue).Max();

            return new
            {
                u.Id,
                u.FullName,
                Role = u.Role.ToString(),
                u.AssignedBoothNumbers,
                Visits = periodVisits.Count,
                Calls = periodCalls.Count,
                FavourConv = favourConv,
                Score = score,
                TotalVisits = uVisits.Count,
                TotalCalls = uCalls.Count,
                LastActivity = lastActivity
            };
        })
        .OrderByDescending(r => r.Score)
        .ToList();

        // Dense ranking (tied scores share the same rank)
        var ranked = new List<LeaderboardRowDto>(rows.Count);
        int rank = 0, prevScore = int.MinValue;
        foreach (var r in rows)
        {
            if (r.Score != prevScore) { rank++; prevScore = r.Score; }
            ranked.Add(new LeaderboardRowDto(
                r.Id, r.FullName, r.Role, r.AssignedBoothNumbers,
                r.Visits, r.Calls, r.FavourConv, r.Score,
                r.TotalVisits, r.TotalCalls, r.LastActivity,
                rank, r.Id == currentUserId));
        }

        return Ok(new LeaderboardResponse(normalizedPeriod, label, ranked));
    }

    private static (DateTime cutoff, string label, string normalized) ResolvePeriod(string period)
    {
        var now = DateTime.UtcNow;
        return (period?.ToLowerInvariant()) switch
        {
            "today" => (now.Date, "Today", "today"),
            "month" => (new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc), "This Month", "month"),
            "alltime" => (DateTime.MinValue, "All Time", "alltime"),
            _ => (now.Date.AddDays(-(int)now.DayOfWeek), "This Week", "week"),
        };
    }
}
