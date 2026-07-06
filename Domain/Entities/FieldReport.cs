using Nirvachak_AI.Domain.Enums;

namespace Nirvachak_AI.Domain.Entities;

public class FieldReport
{
    public int Id { get; set; }
    public string WorkerUserId { get; set; } = string.Empty;
    public string WorkerName { get; set; } = string.Empty;
    public DateTime ReportDate { get; set; } = DateTime.UtcNow.Date;
    public int ContactsMade { get; set; }
    public int FavourContacts { get; set; }
    public int FloatingContacts { get; set; }
    public int AgainstContacts { get; set; }
    public int IssuesLogged { get; set; }
    public string? Highlights { get; set; }
    public string? Challenges { get; set; }
    public string? PlannedForTomorrow { get; set; }
    public FieldReportStatus Status { get; set; } = FieldReportStatus.Submitted;
    public string? ReviewerNotes { get; set; }
    public string? ReviewedByUserId { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public int ConstituencyId { get; set; }
    public Constituency? Constituency { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
