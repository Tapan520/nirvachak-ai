namespace Nirvachak_AI.Domain.Entities;

/// <summary>Stores Expo push tokens for mobile users to enable server-side push notifications.</summary>
public class UserPushToken
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string ExpoPushToken { get; set; } = string.Empty;
    public string? DeviceId { get; set; }
    public string? Platform { get; set; }    // ios | android
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
    public DateTime LastSeenAt { get; set; } = DateTime.UtcNow;
}
