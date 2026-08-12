using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Nirvachak_AI.Domain.Entities;
using Nirvachak_AI.Domain.Enums;
using Nirvachak_AI.Infrastructure.Data;

namespace Nirvachak_AI.Pages.Leaderboard;

[Authorize(Roles = "Admin,SuperAdmin,CampaignManager,Candidate,FieldWorker,BoothAgent")]
public class IndexModel : PageModel
{
    private readonly AppDbContext         _db;
    private readonly UserManager<AppUser> _userManager;

    public IndexModel(AppDbContext db, UserManager<AppUser> userManager)
    {
        _db          = db;
        _userManager = userManager;
    }

    // ?? Filters ???????????????????????????????????????????????????
    [BindProperty(SupportsGet = true)] public string  Period                { get; set; } = "week";
    [BindProperty(SupportsGet = true)] public int?    SelectedConstituencyId { get; set; }

    // ?? Output ????????????????????????????????????????????????????
    public List<LeaderboardRow> Rows           { get; set; } = new();
    public List<Constituency>   Constituencies { get; set; } = new();
    public bool                 IsAdmin        { get; set; }
    public string               CurrentUserId  { get; set; } = string.Empty;
    public string               PeriodLabel    { get; set; } = "This Week";

    // ?? Top-3 highlights ?????????????????????????????????????????
    public LeaderboardRow? Gold   => Rows.Count > 0 ? Rows[0] : null;
    public LeaderboardRow? Silver => Rows.Count > 1 ? Rows[1] : null;
    public LeaderboardRow? Bronze => Rows.Count > 2 ? Rows[2] : null;

    public record LeaderboardRow(
        string  UserId,
        string  FullName,
        string  Role,
        int?    ConstituencyId,
        string? AssignedBooths,
        int     VisitsThisPeriod,
        int     CallsThisPeriod,
        int     FavourConversions,   // visits that recorded Favour sentiment
        int     SurveysCollected,    // surveys where this worker's voter was completed
        int     TotalScore,
        int     TotalVisitsAllTime,
        int     TotalCallsAllTime,
        DateTime? LastActivityAt);

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToPage("/Account/Login");

        IsAdmin       = user.Role == UserRole.SuperAdmin;
        CurrentUserId = user.Id;

        if (IsAdmin)
            Constituencies = await _db.Constituencies.OrderBy(c => c.Name).ToListAsync();

        var cId = IsAdmin
            ? (SelectedConstituencyId ?? user.ConstituencyId)
            : user.ConstituencyId;

        // ?? Date range for period ??????????????????????????????????
        var now    = DateTime.UtcNow;
        DateTime cutoff;
        switch (Period)
        {
            case "today":
                cutoff      = now.Date;
                PeriodLabel = "Today";
                break;
            case "month":
                cutoff      = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                PeriodLabel = "This Month";
                break;
            case "alltime":
                cutoff      = DateTime.MinValue;
                PeriodLabel = "All Time";
                break;
            default: // week
                cutoff      = now.Date.AddDays(-(int)now.DayOfWeek);   // start of Sunday
                PeriodLabel = "This Week";
                Period      = "week";
                break;
        }

        // ?? Get all app users in constituency (field-level roles) ??
        var usersQuery = _userManager.Users.AsQueryable();
        if (cId.HasValue)
            usersQuery = usersQuery.Where(u => u.ConstituencyId == cId.Value || u.Role == UserRole.SuperAdmin);

        // Include all roles who do ground work
        var groundUsers = await usersQuery
            .Where(u => u.IsActive &&
                        (u.Role == UserRole.FieldWorker  ||
                         u.Role == UserRole.BoothAgent   ||
                         u.Role == UserRole.CampaignManager))
            .OrderBy(u => u.FullName)
            .ToListAsync();

        if (!groundUsers.Any())
        {
            Rows = new();
            return Page();
        }

        var userIds   = groundUsers.Select(u => u.Id).ToHashSet();
        var userNames = groundUsers.Select(u => u.FullName).ToHashSet();

        // ?? Door-to-door visits ????????????????????????????????????
        var allVisits = await _db.DoorToDoorVisits
            .Where(v => userIds.Contains(v.WorkerUserId))
            .Select(v => new
            {
                v.WorkerUserId,
                v.WorkerName,
                v.VisitedAt,
                v.SentimentAfterVisit
            })
            .ToListAsync();

        // ?? Phone call logs ????????????????????????????????????????
        var allCalls = await _db.PhoneCallLogs
            .Where(c => userIds.Contains(c.CalledByUserId))
            .Select(c => new
            {
                c.CalledByUserId,
                c.CalledByName,
                c.CalledAt,
                c.Outcome
            })
            .ToListAsync();

        // ?? Survey completions (match via voter ? last contacting worker) ??
        // Use DoorToDoorVisits: count Favour visits in period as proxy for survey motivations
        // (No direct worker FK on SurveyCompletion — use visit-based Favour conversions instead)

        // ?? Build leaderboard rows ?????????????????????????????????
        Rows = groundUsers.Select(u =>
        {
            var uVisits      = allVisits.Where(v => v.WorkerUserId == u.Id).ToList();
            var uCalls       = allCalls .Where(c => c.CalledByUserId == u.Id).ToList();

            var periodVisits = uVisits.Where(v => v.VisitedAt >= cutoff).ToList();
            var periodCalls  = uCalls .Where(c => c.CalledAt  >= cutoff).ToList();

            var favourConversions = periodVisits
                .Count(v => v.SentimentAfterVisit == VoterSentiment.Favour);

            var surveyProxy = periodVisits
                .Count(v => v.SentimentAfterVisit == VoterSentiment.Favour ||
                            v.SentimentAfterVisit == VoterSentiment.Neutral);

            // Score: visits×2 + calls×1 + favour conversions×3
            var score = (periodVisits.Count * 2)
                      + (periodCalls.Count  * 1)
                      + (favourConversions   * 3);

            var lastActivity = new[] {
                uVisits.OrderByDescending(v => v.VisitedAt).FirstOrDefault()?.VisitedAt,
                uCalls .OrderByDescending(c => c.CalledAt ).FirstOrDefault()?.CalledAt
            }.Where(d => d.HasValue).Select(d => d!.Value)
             .OrderByDescending(d => d).FirstOrDefault();

            return new LeaderboardRow(
                u.Id,
                u.FullName,
                u.Role.ToString(),
                u.ConstituencyId,
                u.AssignedBoothNumbers,
                periodVisits.Count,
                periodCalls.Count,
                favourConversions,
                surveyProxy,
                score,
                uVisits.Count,
                uCalls.Count,
                lastActivity == default ? null : lastActivity);
        })
        .OrderByDescending(r => r.TotalScore)
        .ToList();

        return Page();
    }
}
