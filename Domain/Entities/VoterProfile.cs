namespace Nirvachak_AI.Domain.Entities;

public class VoterProfile
{
    public int Id { get; set; }
    public int VoterId { get; set; }
    public Voter? Voter { get; set; }

    public string? AgeBracket { get; set; }            // 18–25 / 26–35 / 36–50 / 51–65 / 65+
    public string? CasteCategory { get; set; }         // General / OBC / SC / ST / NT
    public string? Religion { get; set; }              // Hindu / Muslim / Christian / Sikh / Buddhist / Jain / Other
    public string? Education { get; set; }             // Below 10th / 10th / 12th / Graduate / PG+
    public string? Occupation { get; set; }            // Farmer / Service / Business / Student / Homemaker / Other
    public string? MonthlyIncomeBracket { get; set; }  // <10K / 10-25K / 25-50K / 50K+
    public string? PrimaryConcerns { get; set; }       // JSON array — up to 3 picks from issue list
    public string? PreferredLanguage { get; set; }     // For outreach personalisation
    public bool PhoneVerified { get; set; } = false;   // OTP verification flag
    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
    public string? IpAddress { get; set; }
}
