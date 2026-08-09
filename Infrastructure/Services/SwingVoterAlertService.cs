using Microsoft.EntityFrameworkCore;
using Nirvachak_AI.Domain.Entities;
using Nirvachak_AI.Domain.Enums;
using Nirvachak_AI.Infrastructure.Data;

namespace Nirvachak_AI.Infrastructure.Services;

public class SwingVoterAlertService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SwingVoterAlertService> _logger;
    private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(1);

    public SwingVoterAlertService(IServiceScopeFactory scopeFactory,
        ILogger<SwingVoterAlertService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger       = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[SwingAlert] Service started.");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckForSwingsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SwingAlert] Error during swing check.");
            }
            await Task.Delay(CheckInterval, stoppingToken);
        }
    }

    private async Task CheckForSwingsAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var cutoff = DateTime.UtcNow.AddHours(-1);

        // Find visits in the last hour where the visit recorded Favour sentiment
        // but the voter's CURRENT sentiment is now Floating or Against (swing detected)
        var swings = await db.DoorToDoorVisits
            .Include(v => v.Voter)
            .Where(v => v.VisitedAt >= cutoff
                && v.SentimentAfterVisit == VoterSentiment.Favour
                && v.Voter != null
                && (v.Voter.Sentiment == VoterSentiment.Floating
                    || v.Voter.Sentiment == VoterSentiment.Against))
            .Select(v => new {
                v.Voter!.Name,
                v.Voter.BoothNumber,
                v.Voter.WardNumber,
                v.Voter.ConstituencyId,
                CurrentSentiment = v.Voter.Sentiment
            })
            .ToListAsync();

        if (!swings.Any()) return;

        var grouped = swings.GroupBy(s => s.ConstituencyId);
        foreach (var cGroup in grouped)
        {
            var cId = cGroup.Key;
            var list = cGroup.ToList();

            var swingText = string.Join(", ", list.Take(5)
                .Select(s => $"{s.Name} (Booth {s.BoothNumber}) ? {s.CurrentSentiment}"));
            if (list.Count > 5) swingText += $" and {list.Count - 5} more";

            var existing = await db.Announcements
                .Where(a => a.ConstituencyId == cId
                    && a.Category == AnnouncementCategory.LiveDataNudge
                    && a.Title.StartsWith("?? Swing Voter Alert")
                    && a.CreatedAt >= DateTime.UtcNow.AddHours(-2))
                .AnyAsync();

            if (existing) continue;

            db.Announcements.Add(new Announcement
            {
                Title                  = $"?? Swing Voter Alert — {list.Count} voter(s) changed sentiment",
                Body                   = $"The following voter(s) who were previously marked as 'Favour' have recently changed to 'Floating' or 'Against':\n\n{swingText}\n\nPlease assign field workers to re-engage these voters urgently.",
                Category               = AnnouncementCategory.LiveDataNudge,
                CreatedByUserId        = "system",
                CreatedByName          = "System (Auto — Swing Alert)",
                ConstituencyId         = cId,
                TargetRoles            = "CampaignManager,Admin",
                IsPinned               = false,
                RequiresAcknowledgement = true,
                IsActive               = true,
                CreatedAt              = DateTime.UtcNow,
                ExpiresAt              = DateTime.UtcNow.AddDays(1)
            });

            // Push notify managers in this constituency
            var push = scope.ServiceProvider.GetRequiredService<PushNotificationService>();
            await push.SendToConstituencyAsync(db, cId,
                "?? Swing Voter Alert",
                $"{list.Count} Favour voter(s) swung to Floating/Against. Immediate re-engagement needed.",
                new { type = "swing_alert", constituencyId = cId });

            _logger.LogWarning("[SwingAlert] Swing alert created for constituency {CId}: {Count} voters swung.",
                cId, list.Count);
        }

        await db.SaveChangesAsync();
    }
}
