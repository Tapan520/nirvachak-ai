using Nirvachak_AI.Domain.Enums;

namespace Nirvachak_AI.Domain.Entities;

public class PhoneCallLog
{
    public int Id { get; set; }
    public int VoterId { get; set; }
    public Voter? Voter { get; set; }
    public string CalledByUserId { get; set; } = string.Empty;
    public string? CalledByName { get; set; }
    public DateTime CalledAt { get; set; } = DateTime.UtcNow;
    public CallOutcome Outcome { get; set; } = CallOutcome.NoAnswer;
    public int DurationSeconds { get; set; }
    public string? Notes { get; set; }
    public VoterSentiment? SentimentAfterCall { get; set; }
    public int ConstituencyId { get; set; }
    public Constituency? Constituency { get; set; }
}
