namespace Nirvachak_AI.Domain.Entities;

/// <summary>
/// Records election-day readiness checks per booth (one per booth per election day).
/// </summary>
public class BoothChecklist
{
    public int Id { get; set; }
    public int BoothNumber { get; set; }
    public int ConstituencyId { get; set; }
    public Constituency? Constituency { get; set; }

    // Readiness items
    public bool AgentPresent        { get; set; }
    public bool BannerDisplayed     { get; set; }
    public bool VoterListPrinted    { get; set; }
    public bool TransportArranged   { get; set; }
    public bool PhoneCharged        { get; set; }
    public bool BoothClean          { get; set; }

    public string? Notes            { get; set; }
    public string? SubmittedByUserId { get; set; }
    public string? SubmittedByName   { get; set; }
    public DateTime SubmittedAt      { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt       { get; set; }
}
