using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nirvachak_AI.Domain.Entities;
using Nirvachak_AI.Domain.Enums;
using Nirvachak_AI.Infrastructure.Data;
using Nirvachak_AI.Infrastructure.Services;

namespace Nirvachak_AI.Controllers;

/// <summary>
/// Mobile-specific endpoints:
///  - Offline sync (batch visits + sentiment updates)
///  - GPS check-in on visit
///  - Expo push token registration
///  - Volunteer location update (real-time tracking)
///  - Volunteer locations list for map view
///  - WhatsApp message templates
/// </summary>
[ApiController]
[Route("api/mobile")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class MobileController : ApiBaseController
{
    private readonly AppDbContext _db;
    private readonly AuditService _audit;
    private readonly PushNotificationService _push;

    public MobileController(AppDbContext db, AuditService audit, PushNotificationService push)
    {
        _db    = db;
        _audit = audit;
        _push  = push;
    }

    // ?? 1. Offline Sync ??????????????????????????????????????????????????????
    /// <summary>
    /// Accepts a batch of offline-queued visits and sentiment updates from the mobile app.
    /// Idempotent — duplicate visits for the same voter+worker+day are silently skipped.
    /// </summary>
    [HttpPost("sync")]
    public async Task<IActionResult> SyncOfflineData([FromBody] OfflineSyncRequest req)
    {
        var userId    = GetUserId();
        var userName  = GetUserFullName();
        var cId       = GetConstituencyId() ?? 0;
        var syncedAt  = DateTime.UtcNow;

        int visitsAdded   = 0;
        int sentimentUpd  = 0;
        var skipped       = new List<string>();

        // ?? a) Process queued visits ?????????????????????????????????????
        foreach (var v in req.Visits ?? new())
        {
            var voter = await _db.Voters.FindAsync(v.VoterId);
            if (voter == null) { skipped.Add($"Voter {v.VoterId} not found"); continue; }

            // Skip if already logged a visit from this worker today
            var visitDate = v.VisitedAt ?? syncedAt;
            var dayStart  = visitDate.Date;
            var already   = await _db.DoorToDoorVisits.AnyAsync(d =>
                d.VoterId       == v.VoterId &&
                d.WorkerUserId  == userId &&
                d.VisitedAt     >= dayStart &&
                d.VisitedAt     <  dayStart.AddDays(1));

            if (already) { skipped.Add($"Voter {v.VoterId} visit already exists for today"); continue; }

            if (!Enum.TryParse<VisitStatus>(v.Status, out var status))
                status = VisitStatus.Visited;
            if (!Enum.TryParse<VoterSentiment>(v.Sentiment, out var sentiment))
                sentiment = VoterSentiment.Unknown;

            _db.DoorToDoorVisits.Add(new DoorToDoorVisit
            {
                VoterId            = v.VoterId,
                WorkerUserId       = userId,
                WorkerName         = userName,
                VisitedAt          = visitDate,
                Status             = status,
                SentimentAfterVisit = sentiment,
                Notes              = v.Notes,
                IssuesRaised       = v.IssuesRaised,
                Latitude           = v.Latitude,
                Longitude          = v.Longitude,
            });

            voter.Sentiment       = sentiment;
            voter.LastContactedAt = visitDate;
            visitsAdded++;
        }

        // ?? b) Process sentiment-only updates ????????????????????????????
        foreach (var su in req.SentimentUpdates ?? new())
        {
            var voter = await _db.Voters.FindAsync(su.VoterId);
            if (voter == null) { skipped.Add($"Voter {su.VoterId} not found"); continue; }
            if (!Enum.TryParse<VoterSentiment>(su.Sentiment, out var s)) continue;
            voter.Sentiment = s;
            sentimentUpd++;
        }

        await _db.SaveChangesAsync();

        _audit.Track(userId, userName, "OfflineSync", "Mobile",
            details: $"Synced {visitsAdded} visits, {sentimentUpd} sentiment updates. Skipped: {skipped.Count}",
            constituencyId: cId);
        await _db.SaveChangesAsync();

        return Ok(new { synced = true, visitsAdded, sentimentUpdates = sentimentUpd, skipped });
    }

    // ?? 2. GPS Check-In Visit ????????????????????????????????????????????????
    [HttpPost("visit")]
    public async Task<IActionResult> LogVisitWithGps([FromBody] GpsVisitRequest req)
    {
        var userId   = GetUserId();
        var userName = GetUserFullName();
        var cId      = GetConstituencyId() ?? 0;

        var voter = await _db.Voters.FindAsync(req.VoterId);
        if (voter == null) return NotFound(new { error = "Voter not found" });

        if (!Enum.TryParse<VisitStatus>(req.Status, out var status))
            status = VisitStatus.Visited;
        if (!Enum.TryParse<VoterSentiment>(req.Sentiment, out var sentiment))
            sentiment = VoterSentiment.Unknown;

        _db.DoorToDoorVisits.Add(new DoorToDoorVisit
        {
            VoterId             = req.VoterId,
            WorkerUserId        = userId,
            WorkerName          = userName,
            VisitedAt           = DateTime.UtcNow,
            Status              = status,
            SentimentAfterVisit = sentiment,
            Notes               = req.Notes,
            IssuesRaised        = req.IssuesRaised,
            Latitude            = req.Latitude,
            Longitude           = req.Longitude,
        });

        voter.Sentiment       = sentiment;
        voter.LastContactedAt = DateTime.UtcNow;

        // Update volunteer's last known location
        if (req.Latitude.HasValue && req.Longitude.HasValue)
            await UpsertVolunteerLocationAsync(userId, userName, cId,
                req.Latitude.Value, req.Longitude.Value, req.AccuracyMeters);

        _audit.Track(userId, userName, "LogVisit", "Voter", req.VoterId.ToString(),
            $"GPS visit: {status}, {sentiment}. Lat={req.Latitude}, Lon={req.Longitude}",
            cId);
        await _db.SaveChangesAsync();

        return Ok(new { success = true, message = "Visit logged with GPS." });
    }

    // ?? 3. Push Token Registration ???????????????????????????????????????????
    [HttpPost("push-token")]
    public async Task<IActionResult> RegisterPushToken([FromBody] PushTokenRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Token))
            return BadRequest(new { error = "Token is required." });

        var userId = GetUserId();

        // Upsert by token value
        var existing = await _db.UserPushTokens
            .FirstOrDefaultAsync(t => t.ExpoPushToken == req.Token);

        if (existing == null)
        {
            _db.UserPushTokens.Add(new UserPushToken
            {
                UserId        = userId,
                ExpoPushToken = req.Token,
                DeviceId      = req.DeviceId,
                Platform      = req.Platform,
                RegisteredAt  = DateTime.UtcNow,
                LastSeenAt    = DateTime.UtcNow
            });
        }
        else
        {
            existing.UserId    = userId;  // re-assign if device token was recycled
            existing.LastSeenAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        return Ok(new { success = true });
    }

    // ?? 4. Volunteer Location Update ?????????????????????????????????????????
    [HttpPost("location")]
    public async Task<IActionResult> UpdateLocation([FromBody] LocationUpdateRequest req)
    {
        var userId   = GetUserId();
        var userName = GetUserFullName();
        var cId      = GetConstituencyId();
        await UpsertVolunteerLocationAsync(userId, userName, cId ?? 0,
            req.Latitude, req.Longitude, req.AccuracyMeters);
        await _db.SaveChangesAsync();
        return Ok(new { success = true });
    }

    // ?? 5. Volunteer Locations (for map) ?????????????????????????????????????
    [HttpGet("volunteer-locations")]
    public async Task<IActionResult> GetVolunteerLocations()
    {
        var cId      = GetConstituencyId();
        var role     = GetUserRole();
        var isAdmin  = role == nameof(UserRole.SuperAdmin);

        // Only active locations (updated in last 8 hours)
        var cutoff = DateTime.UtcNow.AddHours(-8);
        var locs   = await _db.VolunteerLocations
            .Where(l => l.UpdatedAt >= cutoff &&
                        (isAdmin || l.ConstituencyId == cId))
            .Select(l => new
            {
                l.UserId,
                l.UserName,
                l.Latitude,
                l.Longitude,
                l.AccuracyMeters,
                updatedAt = l.UpdatedAt
            })
            .ToListAsync();

        return Ok(locs);
    }

    // ?? 6. WhatsApp Message Templates ????????????????????????????????????????
    [HttpGet("whatsapp-templates")]
    public async Task<IActionResult> GetWhatsAppTemplates()
    {
        var cId = GetConstituencyId();
        var templates = await _db.MessageTemplates
            .Where(t => !cId.HasValue || t.ConstituencyId == cId)
            .OrderBy(t => t.Category)
            .ThenBy(t => t.Title)
            .Select(t => new {
                t.Id,
                t.Title,
                t.Body,
                t.Language,
                category = t.Category.ToString()
            })
            .ToListAsync();
        return Ok(templates);
    }

    // ?? Helper ???????????????????????????????????????????????????????????????
    private async Task UpsertVolunteerLocationAsync(string userId, string userName,
        int? cId, double lat, double lon, double? accuracy)
    {
        var existing = await _db.VolunteerLocations
            .FirstOrDefaultAsync(l => l.UserId == userId);
        if (existing == null)
        {
            _db.VolunteerLocations.Add(new VolunteerLocation
            {
                UserId         = userId,
                UserName       = userName,
                ConstituencyId = cId,
                Latitude       = lat,
                Longitude      = lon,
                AccuracyMeters = accuracy,
                UpdatedAt      = DateTime.UtcNow
            });
        }
        else
        {
            existing.Latitude       = lat;
            existing.Longitude      = lon;
            existing.AccuracyMeters = accuracy;
            existing.UpdatedAt      = DateTime.UtcNow;
        }
    }
}

// ?? Request / Response Models ????????????????????????????????????????????????

public record OfflineSyncRequest(
    List<QueuedVisit>?          Visits,
    List<QueuedSentimentUpdate>? SentimentUpdates);

public record QueuedVisit(
    int      VoterId,
    string   Status,
    string   Sentiment,
    string?  Notes,
    string?  IssuesRaised,
    double?  Latitude,
    double?  Longitude,
    DateTime? VisitedAt);

public record QueuedSentimentUpdate(int VoterId, string Sentiment);

public record GpsVisitRequest(
    int     VoterId,
    string  Status,
    string  Sentiment,
    string? Notes,
    string? IssuesRaised,
    double? Latitude,
    double? Longitude,
    double? AccuracyMeters);

public record PushTokenRequest(
    string  Token,
    string? DeviceId,
    string? Platform);

public record LocationUpdateRequest(
    double  Latitude,
    double  Longitude,
    double? AccuracyMeters);
