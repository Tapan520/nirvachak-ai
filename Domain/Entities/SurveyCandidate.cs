namespace Nirvachak_AI.Domain.Entities;

public class SurveyCandidate
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? PartyAffiliation { get; set; }
    public string? PhotoUrl { get; set; }
    public string? Notes { get; set; }
    public int ConstituencyId { get; set; }
    public Constituency? Constituency { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
