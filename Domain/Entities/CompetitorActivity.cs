using Nirvachak_AI.Domain.Enums;

namespace Nirvachak_AI.Domain.Entities;

public class CompetitorActivity
{
    public int Id { get; set; }
    public string CompetitorName { get; set; } = string.Empty;
    public string? PartyName { get; set; }
    public string ActivityTitle { get; set; } = string.Empty;
    public CompetitorActivityType ActivityType { get; set; }
    public string? Location { get; set; }
    public string? Ward { get; set; }
    public int? BoothNumber { get; set; }
    public DateTime ActivityDate { get; set; } = DateTime.UtcNow;
    public int? EstimatedCrowd { get; set; }
    public string? Notes { get; set; }
    public CompetitorThreatLevel ThreatLevel { get; set; } = CompetitorThreatLevel.Medium;
    public int ConstituencyId { get; set; }
    public Constituency? Constituency { get; set; }
    public string? LoggedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
