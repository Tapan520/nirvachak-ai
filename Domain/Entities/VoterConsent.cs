namespace Nirvachak_AI.Domain.Entities;

public class VoterConsent
{
    public int Id { get; set; }
    public int VoterId { get; set; }
    public Voter? Voter { get; set; }

    // ?? Mandatory: required to receive coupon / reward ????????????
    // Voter explicitly consents to their demographic data being
    // shared with third-party partner brands for coupon delivery
    // and targeted advertisements.
    public bool AllowThirdPartyAdvertising { get; set; }

    // ?? Optional campaign consents ????????????????????????????????
    public bool AllowCampaignOutreach { get; set; }
    public bool AllowWhatsAppMessages { get; set; }
    public bool AllowSchemeNotifications { get; set; }
    public bool AllowDataForAnalytics { get; set; }

    public DateTime ConsentGivenAt { get; set; } = DateTime.UtcNow;
    public string? IpAddress { get; set; }
}
