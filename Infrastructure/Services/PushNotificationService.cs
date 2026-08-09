using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Nirvachak_AI.Domain.Entities;
using Nirvachak_AI.Infrastructure.Data;

namespace Nirvachak_AI.Infrastructure.Services;

/// <summary>
/// Sends push notifications to mobile users via Expo's push notification service.
/// Tokens are registered by the mobile app via POST /api/mobile/push-token.
/// </summary>
public class PushNotificationService
{
    private readonly IHttpClientFactory _http;
    private readonly ILogger<PushNotificationService> _logger;
    private const string ExpoEndpoint = "https://exp.host/--/api/v2/push/send";

    public PushNotificationService(IHttpClientFactory http,
        ILogger<PushNotificationService> logger)
    {
        _http   = http;
        _logger = logger;
    }

    /// <summary>Send a push to all tokens registered for a given userId.</summary>
    public async Task SendToUserAsync(AppDbContext db, string userId,
        string title, string body, object? data = null)
    {
        var tokens = await db.UserPushTokens
            .Where(t => t.UserId == userId)
            .Select(t => t.ExpoPushToken)
            .ToListAsync();

        if (!tokens.Any()) return;
        await SendBatchAsync(tokens, title, body, data);
    }

    /// <summary>Send a push to all tokens in a constituency with matching role.</summary>
    public async Task SendToConstituencyAsync(AppDbContext db, int constituencyId,
        string title, string body, object? data = null, string[]? roles = null)
    {
        var userIds = await db.Set<AppUser>()
            .Where(u => u.ConstituencyId == constituencyId && u.IsActive)
            .Select(u => u.Id)
            .ToListAsync();

        if (!userIds.Any()) return;

        var tokens = await db.UserPushTokens
            .Where(t => userIds.Contains(t.UserId))
            .Select(t => t.ExpoPushToken)
            .ToListAsync();

        if (!tokens.Any()) return;
        await SendBatchAsync(tokens, title, body, data);
    }

    /// <summary>Core batch send — Expo accepts up to 100 messages per request.</summary>
    public async Task SendBatchAsync(IEnumerable<string> tokens,
        string title, string body, object? data = null)
    {
        var messages = tokens
            .Where(t => t.StartsWith("ExponentPushToken[") || t.StartsWith("ExpoPushToken["))
            .Select(t => new
            {
                to      = t,
                title,
                body,
                sound   = "default",
                data    = data ?? new { }
            }).ToList();

        if (!messages.Any()) return;

        try
        {
            var client = _http.CreateClient();
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate");

            // Expo allows up to 100 per request — chunk if needed
            foreach (var batch in messages.Chunk(100))
            {
                var resp = await client.PostAsJsonAsync(ExpoEndpoint, batch);
                if (!resp.IsSuccessStatusCode)
                    _logger.LogWarning("[Push] Expo returned {Status}", resp.StatusCode);
                else
                    _logger.LogInformation("[Push] Sent {Count} notifications: {Title}", batch.Length, title);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Push] Failed to send push notifications.");
        }
    }
}
