using Nirvachak_AI.Domain.Enums;

namespace Nirvachak_AI.Domain.Entities;

public class BoothShiftAssignment
{
    public int Id { get; set; }
    public int VolunteerId { get; set; }
    public Volunteer? Volunteer { get; set; }
    public int BoothNumber { get; set; }
    public DateTime ShiftStart { get; set; }
    public DateTime ShiftEnd { get; set; }
    public ShiftRole Role { get; set; } = ShiftRole.BoothAgent;
    public bool IsConfirmed { get; set; } = false;
    public string? Notes { get; set; }
    public int ConstituencyId { get; set; }
    public Constituency? Constituency { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
