using Nirvachak_AI.Domain.Enums;

namespace Nirvachak_AI.Domain.Entities;

public class Voter
{
    public int Id { get; set; }
    public string VoterId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? NameLocal { get; set; }
    public string? FatherHusbandName { get; set; }
    public int Age { get; set; }
    public string Gender { get; set; } = string.Empty;
    public string? MobileNumber { get; set; }
    public string Address { get; set; } = string.Empty;
    public int BoothNumber { get; set; }
    public string? WardNumber { get; set; }
    public string? PannaNumber { get; set; }
    public int SerialNumber { get; set; }
    public int ConstituencyId { get; set; }
    public Constituency? Constituency { get; set; }
    public VoterSentiment Sentiment { get; set; } = VoterSentiment.Unknown;
    public ElectionDayStatus ElectionDayStatus { get; set; } = ElectionDayStatus.NotVoted;
    public string? Notes { get; set; }
    public string? PhotoPath { get; set; }
    public string? HouseholdId { get; set; }
    public bool IsDeleted { get; set; } = false;
    public bool IsSelfRegistered { get; set; } = false;
    public DateTime ImportedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastContactedAt { get; set; }
    public ICollection<DoorToDoorVisit> Visits { get; set; } = new List<DoorToDoorVisit>();
}
