namespace Nirvachak_AI.Domain.Entities;

/// <summary>
/// Records the last known GPS location of a volunteer/field worker.
/// Updated from the mobile app when they submit a visit check-in.
/// </summary>
public class VolunteerLocation
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public int? ConstituencyId { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double? AccuracyMeters { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
