using Microsoft.AspNetCore.Identity;
using Nirvachak_AI.Domain.Enums;

namespace Nirvachak_AI.Domain.Entities;

public class AppUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.FieldWorker;
    public int? ConstituencyId { get; set; }
    public Constituency? Constituency { get; set; }
    public string? AssignedBoothNumbers { get; set; }
    public string? AssignedWard { get; set; }
    // Optional candidate profile — used to render a party-branded strip
    // on the Voter Slip when this user prints (Panchayat / party-agent use case).
    public string? CandidateName { get; set; }
    public string? CandidatePartyOrSymbol { get; set; }
    public string? CandidateSlogan { get; set; }
    public string? CandidatePhotoUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
