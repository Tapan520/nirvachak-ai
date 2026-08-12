using Microsoft.EntityFrameworkCore;
using Nirvachak_AI.Domain.Entities;
using Nirvachak_AI.Domain.Enums;
using Nirvachak_AI.Infrastructure.Data;

namespace Nirvachak_AI.Infrastructure.Services;

public class NoonAlertService : BackgroundService
{
    private readonly IServiceScopeFactory      _scopeFactory;
    private readonly ILogger<NoonAlertService> _logger;
    private static readonly TimeSpan NoonUtc   = TimeSpan.FromHours(6.5);
    private const           double   Threshold = 0.50;

    public NoonAlertService(IServiceScopeFactory scopeFactory, ILogger<NoonAlertService> logger)
    { _scopeFactory = scopeFactory; _logger = logger; }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("NoonAlert: Service started.");
        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = TimeUntilNoon();
            _logger.LogInformation("NoonAlert: Next check in {M} minutes.", (int)delay.TotalMinutes);
            await Task.Delay(delay, stoppingToken);
            if (!stoppingToken.IsCancellationRequested)
            {
                try   { await RunChecksAsync(); }
                catch (Exception ex) { _logger.LogError(ex, "NoonAlert: Error during noon check."); }
            }
        }
    }

    private static TimeSpan TimeUntilNoon()
    {
        var now    = DateTime.UtcNow;
        var target = now.Date.Add(NoonUtc);
        if (target <= now) target = target.AddDays(1);
        return target - now;
    }

    private async Task RunChecksAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var db   = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var push = scope.ServiceProvider.GetRequiredService<PushNotificationService>();
        var todayUtc = DateTime.UtcNow.Date;
        var cs = await db.Constituencies.ToListAsync();
        foreach (var c in cs)
        {
            try   { await CheckAsync(db, push, c, todayUtc); }
            catch (Exception ex) { _logger.LogError(ex, "NoonAlert: Error for constituency {N}.", c.Name); }
        }
        await db.SaveChangesAsync();
        _logger.LogInformation("NoonAlert: Done for {N} constituencies.", cs.Count);
    }

    private async Task CheckAsync(
        AppDbContext db, PushNotificationService push, Constituency c, DateTime todayUtc)
    {
        // Guard: one alert per constituency per calendar day
        var done = await db.Announcements.AnyAsync(a =>
            a.ConstituencyId == c.Id &&
            a.CreatedByName  == "System (Noon Alert)" &&
            a.CreatedAt      >= todayUtc &&
            a.CreatedAt      <  todayUtc.AddDays(1));
        if (done) return;

        var booths = await db.Booths
            .Where(b => b.ConstituencyId == c.Id)
            .OrderBy(b => b.BoothNumber).ToListAsync();
        var bNums  = booths.Select(b => b.BoothNumber).ToHashSet();

        var favPer = await db.Voters
            .Where(v => v.ConstituencyId == c.Id && !v.IsDeleted
                     && v.Sentiment == VoterSentiment.Favour
                     && bNums.Contains(v.BoothNumber))
            .GroupBy(v => v.BoothNumber)
            .Select(g => new { Booth = g.Key, Total = g.Count() })
            .ToDictionaryAsync(x => x.Booth, x => x.Total);

        var votPer = await db.Voters
            .Where(v => v.ConstituencyId == c.Id && !v.IsDeleted
                     && v.Sentiment == VoterSentiment.Favour
                     && v.ElectionDayStatus == ElectionDayStatus.Voted
                     && bNums.Contains(v.BoothNumber))
            .GroupBy(v => v.BoothNumber)
            .Select(g => new { Booth = g.Key, Voted = g.Count() })
            .ToDictionaryAsync(x => x.Booth, x => x.Voted);

        var tf = favPer.Values.Sum();
        var tv = votPer.Values.Sum();
        if (tf == 0) return;

        var rate = (double)tv / tf;
        var crit = booths.Select(b =>
        {
            var t = favPer.GetValueOrDefault(b.BoothNumber, 0);
            var v = votPer.GetValueOrDefault(b.BoothNumber, 0);
            var r = t > 0 ? (double)v / t : 1.0;
            return new { b.BoothNumber, b.BoothName, Total = t, Voted = v, Rate = r };
        }).Where(b => b.Total > 0 && b.Rate < Threshold).OrderBy(b => b.Rate).ToList();

        if (rate >= Threshold && !crit.Any()) return;

        var pct = (int)Math.Round(rate * 100);
        var sb  = new System.Text.StringBuilder();
        sb.AppendLine($"It is 12:00 PM IST. Only {pct}% of Favour voters have voted so far.");
        sb.AppendLine($"Overall: {tv} of {tf} Favour voters have cast their vote.");
        sb.AppendLine();
        if (crit.Any())
        {
            sb.AppendLine($"Booths below 50% Favour turnout ({crit.Count} booth(s)):");
            foreach (var b in crit.Take(8))
                sb.AppendLine($"  Booth {b.BoothNumber} - {b.BoothName}: {b.Voted}/{b.Total} ({(int)Math.Round(b.Rate * 100)}%)");
            if (crit.Count > 8) sb.AppendLine($"  ...and {crit.Count - 8} more");
            sb.AppendLine();
        }
        sb.AppendLine("IMMEDIATE ACTIONS:");
        sb.AppendLine("  1. Open Election Day -> Chase List");
        sb.AppendLine("  2. Use WhatsApp Blast for all unvoted Favour voters");
        sb.AppendLine("  3. Deploy transport volunteers for home pick-up");
        sb.AppendLine("  4. Polling closes at 6:00 PM - every vote counts!");

        var title = crit.Any()
            ? $"Noon Alert - {crit.Count} booth(s) critical! Only {pct}% Favour voters voted"
            : $"Noon Alert - Only {pct}% Favour voters voted by noon - mobilise NOW!";

        db.Announcements.Add(new Announcement
        {
            Title                   = title,
            Body                    = sb.ToString().Trim(),
            Category                = AnnouncementCategory.CriticalAlert,
            CreatedByUserId         = "system",
            CreatedByName           = "System (Noon Alert)",
            ConstituencyId          = c.Id,
            TargetRoles             = "All",
            IsPinned                = true,
            RequiresAcknowledgement = false,
            IsActive                = true,
            ExpiresAt               = todayUtc.AddHours(20),
            CreatedAt               = DateTime.UtcNow
        });

        await push.SendToConstituencyAsync(db, c.Id,
            "Noon Alert - Mobilise Favour Voters NOW!",
            $"Only {pct}% of Favour voters voted. {crit.Count} booths critical. Open Chase List!",
            new { type = "noon_alert", constituencyId = c.Id });

        _logger.LogWarning(
            "NoonAlert: Alert for {Name}: {Pct}% turnout, {Count} critical booths.",
            c.Name, pct, crit.Count);
    }
}


