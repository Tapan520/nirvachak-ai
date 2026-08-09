using Nirvachak_AI.Domain.Enums;

namespace Nirvachak_AI.Domain.Entities;

public class DoorToDoorVisit
{
    public int Id { get; set; }
    public int VoterId { get; set; }
    public Voter? Voter { get; set; }
    public string WorkerUserId { get; set; } = string.Empty;
    public string WorkerName { get; set; } = string.Empty;
    public DateTime VisitedAt { get; set; } = DateTime.UtcNow;
    public VisitStatus Status { get; set; }
    public VoterSentiment SentimentAfterVisit { get; set; }
    public string? Notes { get; set; }
    public string? IssuesRaised { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}

