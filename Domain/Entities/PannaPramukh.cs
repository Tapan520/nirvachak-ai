namespace Nirvachak_AI.Domain.Entities;

public class PannaPramukh
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Address { get; set; }
    public int BoothNumber { get; set; }
    public string PannaNumber { get; set; } = string.Empty;    // Panna page (e.g. "3A")
    public int ConstituencyId { get; set; }
    public Constituency? Constituency { get; set; }
    public int TotalVotersAssigned { get; set; }
    public int VotersContacted { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
