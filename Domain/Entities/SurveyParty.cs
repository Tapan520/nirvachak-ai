namespace Nirvachak_AI.Domain.Entities;

public class SurveyParty
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Symbol { get; set; }
    public string? Notes { get; set; }
    public int ConstituencyId { get; set; }
    public Constituency? Constituency { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
