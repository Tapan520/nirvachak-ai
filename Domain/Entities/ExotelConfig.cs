namespace Nirvachak_AI.Domain.Entities;

/// <summary>
/// Stores Exotel API credentials and settings scoped per constituency.
/// Managed by Admin/SuperAdmin via the Settings > Exotel page.
/// </summary>
public class ExotelConfig
{
    public int Id { get; set; }

    /// <summary>Constituency this config belongs to. Null = global fallback for SuperAdmin.</summary>
    public int? ConstituencyId { get; set; }
    public Constituency? Constituency { get; set; }

    // ?? Exotel Credentials ?????????????????????????????????????????????????
    /// <summary>Exotel API Key (found in Exotel Dashboard ? Settings ? API).</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Exotel API Token / Secret.</summary>
    public string ApiToken { get; set; } = string.Empty;

    /// <summary>Account SID (sub-domain prefix, e.g. "exotel" in exotel.exotel.com).</summary>
    public string AccountSid { get; set; } = string.Empty;

    /// <summary>Exotel API subdomain. Default: api.exotel.com. For other regions e.g. sg1.exotel.com.</summary>
    public string Subdomain { get; set; } = "api.exotel.com";

    /// <summary>Your Exotel ExoPhone number used as caller ID (0XXXXXXXXXX format).</summary>
    public string ExoPhone { get; set; } = string.Empty;

    // ?? SMS ????????????????????????????????????????????????????????????????
    /// <summary>Optional DLT-registered SMS sender ID for transactional messages.</summary>
    public string? SmsSenderId { get; set; }

    /// <summary>Whether this config is active.</summary>
    public bool IsEnabled { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
