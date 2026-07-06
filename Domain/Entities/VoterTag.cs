namespace Nirvachak_AI.Domain.Entities;

public class VoterTag
{
    public int Id { get; set; }
    public int VoterId { get; set; }
    public Voter? Voter { get; set; }
    public string Tag { get; set; } = string.Empty;
    public string? AddedByUserId { get; set; }
    public int ConstituencyId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
