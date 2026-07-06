using Nirvachak_AI.Domain.Enums;

namespace Nirvachak_AI.Domain.Entities;

public class RapidResponseItem
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Source { get; set; }                     // WhatsApp, Local Media, Competitor, Other
    public string? AffectedWards { get; set; }              // comma-separated ward numbers
    public string? AssignedToUserId { get; set; }
    public string? AssignedToName { get; set; }
    public string? ResponseText { get; set; }
    public RapidResponseStatus Status { get; set; } = RapidResponseStatus.Detected;
    public RapidResponseThreat ThreatLevel { get; set; } = RapidResponseThreat.Medium;
    public int ConstituencyId { get; set; }
    public Constituency? Constituency { get; set; }
    public string? LoggedByUserId { get; set; }
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
