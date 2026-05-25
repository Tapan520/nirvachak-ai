using Nirvachak_AI.Domain.Enums;

namespace Nirvachak_AI.Domain.Entities;

public class Influencer
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? MobileNumber { get; set; }
    public string? Category { get; set; }   // Religious, Caste, Youth, Women, Farmer, etc.
    public string? Community { get; set; }
    public int? EstimatedFollowers { get; set; }
    public string? Ward { get; set; }
    public int? BoothNumber { get; set; }
    public InfluencerAlignment Alignment { get; set; } = InfluencerAlignment.Unknown;
    public string? Notes { get; set; }
    public DateTime? LastMetAt { get; set; }
    public string? LastMeetingOutcome { get; set; }
    public int ConstituencyId { get; set; }
    public Constituency? Constituency { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
