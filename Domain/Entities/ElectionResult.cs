using Nirvachak_AI.Domain.Enums;

namespace Nirvachak_AI.Domain.Entities;

public class ElectionResult
{
    public int Id { get; set; }
    public int BoothNumber { get; set; }
    public int RoundNumber { get; set; }
    public int CandidateVotes { get; set; }
    public int? Competitor1Votes { get; set; }
    public string? Competitor1Name { get; set; }
    public int? Competitor2Votes { get; set; }
    public string? Competitor2Name { get; set; }
    public int? Competitor3Votes { get; set; }
    public string? Competitor3Name { get; set; }
    public int? TotalVotesCast { get; set; }
    public bool IsFinal { get; set; } = false;
    public int ConstituencyId { get; set; }
    public Constituency? Constituency { get; set; }
    public string? EnteredByUserId { get; set; }
    public DateTime EnteredAt { get; set; } = DateTime.UtcNow;
}
