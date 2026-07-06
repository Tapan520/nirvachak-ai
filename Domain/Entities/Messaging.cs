using Nirvachak_AI.Domain.Enums;

namespace Nirvachak_AI.Domain.Entities;

public class MessageTemplate
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string Language { get; set; } = "English";   // English, Hindi, Marathi
    public MessageCategory Category { get; set; }
    public int ConstituencyId { get; set; }
    public Constituency? Constituency { get; set; }
    public string CreatedByUserId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class MessageBroadcast
{
    public int Id { get; set; }
    public int TemplateId { get; set; }
    public MessageTemplate? Template { get; set; }
    // JSON filter: { "wards":["1","2"], "booths":[1,2], "sentiment":"Favour", "tags":["NeedsTransport"] }
    public string? TargetFilter { get; set; }
    public string? TargetDescription { get; set; }   // human-readable summary
    public int TotalTargeted { get; set; }
    public int SentCount { get; set; }
    public int FailedCount { get; set; }
    public BroadcastStatus Status { get; set; } = BroadcastStatus.Draft;
    public DateTime? ScheduledAt { get; set; }
    public DateTime? SentAt { get; set; }
    public int ConstituencyId { get; set; }
    public Constituency? Constituency { get; set; }
    public string CreatedByUserId { get; set; } = string.Empty;
    public string? CreatedByName { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
