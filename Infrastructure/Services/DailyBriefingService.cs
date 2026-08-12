using Microsoft.EntityFrameworkCore;
using Nirvachak_AI.Domain.Entities;
using Nirvachak_AI.Domain.Enums;
using Nirvachak_AI.Infrastructure.Data;

namespace Nirvachak_AI.Infrastructure.Services;

/// <summary>
/// Background service that fires every morning at 7:00 AM IST (1:30 AM UTC).
/// Auto-generates a DailyBriefing announcement per constituency — no manual work needed.
/// Covers: today's targets, weak booths, swing voters, open grievances, upcoming events.
/// Guards against duplicate briefings (one per constituency per calendar day).
/// </summary>
public class DailyBriefingService : BackgroundService
{
    private readonly IServiceScopeFactory              _scopeFactory;
    private readonly ILogger<DailyBriefingService>     _logger;

    // 7:00 AM IST = 01:30 AM UTC
    private static readonly TimeSpan BriefingTimeUtc = TimeSpan.FromHours(1.5);

    public DailyBriefingService(
        IServiceScopeFactory          scopeFactory,
        ILogger<DailyBriefingService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger       = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[DailyBriefing] Service started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = TimeUntilNextBriefing();
            _logger.LogInformation("[DailyBriefing] Next briefing in {Minutes} minutes.", (int)delay.TotalMinutes);

            await Task.Delay(delay, stoppingToken);

            if (!stoppingToken.IsCancellationRequested)
            {
                try   { await GenerateBriefingsAsync(); }
                catch (Exception ex)
                { _logger.LogError(ex, "[DailyBriefing] Failed to generate briefings."); }
            }
        }
    }

    // ?? Calculate time until next 7 AM IST (1:30 UTC) ?????????????????????
    private static TimeSpan TimeUntilNextBriefing()
    {
        var now    = DateTime.UtcNow;
        var target = now.Date.Add(BriefingTimeUtc);
        if (target <= now) target = target.AddDays(1);
        return target - now;
    }

    // ?? Main briefing generator ????????????????????????????????????????????
    private async Task GenerateBriefingsAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var todayUtc  = DateTime.UtcNow.Date;
        var todayIst  = todayUtc.AddHours(5).AddMinutes(30); // IST date for display

        var constituencies = await db.Constituencies.ToListAsync();

        foreach (var c in constituencies)
        {
            try   { await GenerateForConstituencyAsync(db, c, todayUtc, todayIst); }
            catch (Exception ex)
            { _logger.LogError(ex, "[DailyBriefing] Error for constituency {Name}.", c.Name); }
        }

        await db.SaveChangesAsync();
        _logger.LogInformation("[DailyBriefing] Briefings generated for {Count} constituencies.", constituencies.Count);
    }

    private static async Task GenerateForConstituencyAsync(
        AppDbContext db, Constituency c, DateTime todayUtc, DateTime todayIst)
    {
        // ?? Guard: skip if already posted today ???????????????????????????
        var alreadyPosted = await db.Announcements.AnyAsync(a =>
            a.ConstituencyId == c.Id &&
            a.Category == AnnouncementCategory.DailyBriefing &&
            a.CreatedAt >= todayUtc &&
            a.CreatedAt <  todayUtc.AddDays(1));

        if (alreadyPosted) return;

        // ?? 1. Booth coverage — find weak booths (<30%) ???????????????????
        var booths = await db.Booths
            .Where(b => b.ConstituencyId == c.Id)
            .OrderBy(b => b.BoothNumber)
            .ToListAsync();

        var contactedPerBooth = await db.Voters
            .Where(v => v.ConstituencyId == c.Id && !v.IsDeleted && v.LastContactedAt != null)
            .GroupBy(v => v.BoothNumber)
            .Select(g => new { Booth = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Booth, x => x.Count);

        var totalPerBooth = await db.Voters
            .Where(v => v.ConstituencyId == c.Id && !v.IsDeleted)
            .GroupBy(v => v.BoothNumber)
            .Select(g => new { Booth = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Booth, x => x.Count);

        var weakBooths = booths
            .Select(b =>
            {
                var total     = totalPerBooth.GetValueOrDefault(b.BoothNumber, b.TotalVoters);
                var contacted = contactedPerBooth.GetValueOrDefault(b.BoothNumber, 0);
                var pct       = total > 0 ? (int)Math.Round((double)contacted / total * 100) : 0;
                return new { b.BoothNumber, b.BoothName, Total = total, Contacted = contacted, Pct = pct };
            })
            .Where(b => b.Pct < 30)
            .OrderBy(b => b.Pct)
            .Take(5)
            .ToList();

        // ?? 2. Today's upcoming events ?????????????????????????????????????
        var tomorrowUtc = todayUtc.AddDays(1);
        var todayEvents = await db.CampaignEvents
            .Where(e => e.ConstituencyId == c.Id
                     && !e.IsCompleted
                     && e.ScheduledAt >= todayUtc
                     && e.ScheduledAt <  tomorrowUtc)
            .OrderBy(e => e.ScheduledAt)
            .ToListAsync();

        // ?? 3. Swing voters (Floating/Against with prior Favour visits) ????
        var swingCount = await db.Voters
            .Where(v => v.ConstituencyId == c.Id && !v.IsDeleted &&
                       (v.Sentiment == VoterSentiment.Floating ||
                        v.Sentiment == VoterSentiment.Against))
            .Join(db.DoorToDoorVisits
                      .Where(dv => dv.SentimentAfterVisit == VoterSentiment.Favour),
                  voter => voter.Id,
                  visit => visit.VoterId,
                  (voter, visit) => voter.Id)
            .Distinct()
            .CountAsync();

        // ?? 4. Open grievances ?????????????????????????????????????????????
        var openGrievances = await db.Grievances
            .Where(g => g.ConstituencyId == c.Id &&
                       (g.Status == GrievanceStatus.Open ||
                        g.Status == GrievanceStatus.InProgress))
            .CountAsync();

        // ?? 5. Favour / Total voters for headline ?????????????????????????
        var totalVoters  = await db.Voters.CountAsync(v => v.ConstituencyId == c.Id && !v.IsDeleted);
        var favourVoters = await db.Voters.CountAsync(v => v.ConstituencyId == c.Id && !v.IsDeleted
                                                           && v.Sentiment == VoterSentiment.Favour);
        var overallCoverage = totalVoters > 0
            ? (int)Math.Round((double)contactedPerBooth.Values.Sum() / totalVoters * 100)
            : 0;

        // ?? 6. Yesterday's visits (performance snapshot) ??????????????????
        var yesterdayUtc = todayUtc.AddDays(-1);
        var visitsYesterday = await db.DoorToDoorVisits
            .Join(db.Voters.Where(v => v.ConstituencyId == c.Id && !v.IsDeleted),
                  visit => visit.VoterId, voter => voter.Id, (visit, voter) => visit)
            .CountAsync(v => v.VisitedAt >= yesterdayUtc && v.VisitedAt < todayUtc);

        // ?? Build briefing body ????????????????????????????????????????????
        var dateLabel = todayIst.ToString("dddd, dd MMMM yyyy");
        var lines     = new System.Text.StringBuilder();

        lines.AppendLine($"?? Good morning team! Here is your campaign briefing for {dateLabel}.");
        lines.AppendLine();

        // Overall status
        lines.AppendLine("?? Campaign Status:");
        lines.AppendLine($"   • Total Voters: {totalVoters:N0}  |  In Favour: {favourVoters:N0}  |  Coverage: {overallCoverage}%");
        if (visitsYesterday > 0)
            lines.AppendLine($"   • Yesterday's visits logged: {visitsYesterday}");
        lines.AppendLine();

        // Priority booths
        if (weakBooths.Any())
        {
            lines.AppendLine("?? Priority Booths Today (coverage below 30% — needs urgent attention):");
            foreach (var b in weakBooths)
                lines.AppendLine($"   ?? Booth {b.BoothNumber} — {b.BoothName}: {b.Contacted}/{b.Total} contacted ({b.Pct}%)");
            lines.AppendLine("   ? Assign your best field workers to these booths first.");
            lines.AppendLine();
        }
        else
        {
            lines.AppendLine("? All booths are above 30% coverage — great progress!");
            lines.AppendLine();
        }

        // Swing voters
        if (swingCount > 0)
        {
            lines.AppendLine($"?? Swing Voters: {swingCount} voter(s) previously marked Favour have swung to Floating/Against.");
            lines.AppendLine("   ? Visit the Swing Voter Intelligence page and assign re-engagement immediately.");
            lines.AppendLine();
        }

        // Today's events
        if (todayEvents.Any())
        {
            lines.AppendLine("?? Today's Campaign Events:");
            foreach (var ev in todayEvents)
            {
                var ist = ev.ScheduledAt.AddHours(5).AddMinutes(30);
                lines.AppendLine($"   ?? {ev.Title} — {ist:h:mm tt} at {ev.Location}");
                if (ev.ExpectedAttendance.HasValue)
                    lines.AppendLine($"       Expected attendance: {ev.ExpectedAttendance}");
            }
            lines.AppendLine();
        }

        // Grievances
        if (openGrievances > 0)
        {
            lines.AppendLine($"?? Open Grievances: {openGrievances} pending resolution.");
            lines.AppendLine("   ? Check the Grievances section and follow up on critical items.");
            lines.AppendLine();
        }

        lines.AppendLine("?? Every contact counts. Update sentiments in the app after each visit.");
        lines.AppendLine("Good luck today — let's make every vote count! ???");

        // ?? Build title ????????????????????????????????????????????????????
        var priorityNote = weakBooths.Any()
            ? $" | ?? {weakBooths.Count} weak booth(s) need attention"
            : " | ? Coverage looking good";

        var title = $"?? Daily Briefing — {todayIst:dd MMM}{priorityNote}";

        // ?? Save announcement ??????????????????????????????????????????????
        db.Announcements.Add(new Announcement
        {
            Title                   = title,
            Body                    = lines.ToString().Trim(),
            Category                = AnnouncementCategory.DailyBriefing,
            CreatedByUserId         = "system",
            CreatedByName           = "System (Auto-Briefing)",
            ConstituencyId          = c.Id,
            TargetRoles             = "All",
            IsPinned                = false,
            RequiresAcknowledgement = false,
            IsActive                = true,
            ExpiresAt               = todayUtc.AddHours(22),   // expires tonight
            CreatedAt               = DateTime.UtcNow
        });
    }
}
