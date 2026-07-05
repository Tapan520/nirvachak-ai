using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Nirvachak_AI.Domain.Entities;
using Nirvachak_AI.Domain.Enums;
using Nirvachak_AI.Infrastructure.Data;
using Nirvachak_AI.Infrastructure.Services;

namespace Nirvachak_AI.Pages.Announcements;

public class AnnouncementInputModel
{
    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Body { get; set; } = string.Empty;

    public AnnouncementCategory Category { get; set; }

    public List<string> SelectedRoles { get; set; } = new();

    public DateTime? ExpiresAt { get; set; }

    public bool RequiresAcknowledgement { get; set; }

    public int? ConstituencyId { get; set; }
}

public class CreateModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _userManager;
    private readonly AuditService _audit;

    public CreateModel(AppDbContext db, UserManager<AppUser> userManager, AuditService audit)
    {
        _db = db;
        _userManager = userManager;
        _audit = audit;
    }

    [BindProperty]
    public AnnouncementInputModel Input { get; set; } = new();

    public List<UserRole> AllowedTargetRoles { get; set; } = new();
    public List<Constituency> Constituencies { get; set; } = new();
    public bool IsAdmin { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Forbid();
        if (user.Role == UserRole.BoothAgent) return Forbid();

        IsAdmin = user.Role == UserRole.SuperAdmin;
        AllowedTargetRoles = GetAllowedTargetRoles(user.Role);

        if (IsAdmin)
            Constituencies = await _db.Constituencies.OrderBy(c => c.Name).ToListAsync();

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Forbid();
        if (user.Role == UserRole.BoothAgent) return Forbid();

        IsAdmin = user.Role == UserRole.SuperAdmin;
        AllowedTargetRoles = GetAllowedTargetRoles(user.Role);

        if (IsAdmin)
            Constituencies = await _db.Constituencies.OrderBy(c => c.Name).ToListAsync();

        if (!ModelState.IsValid) return Page();

        // Compute target roles
        string targetRoles = Input.SelectedRoles.Any()
            ? string.Join(",", Input.SelectedRoles)
            : "All";

        // Critical alerts are auto-pinned; EC compliance auto-requires ack
        bool isPinned = Input.Category == AnnouncementCategory.CriticalAlert;
        bool requiresAck = Input.Category == AnnouncementCategory.ECComplianceNotice || Input.RequiresAcknowledgement;

        int? constituencyId = IsAdmin ? Input.ConstituencyId : user.ConstituencyId;

        var announcement = new Announcement
        {
            Title = Input.Title,
            Body = Input.Body,
            Category = Input.Category,
            CreatedByUserId = user.Id,
            CreatedByName = user.FullName,
            ConstituencyId = constituencyId,
            TargetRoles = targetRoles,
            ExpiresAt = Input.ExpiresAt.HasValue ? Input.ExpiresAt.Value.ToUniversalTime() : null,
            RequiresAcknowledgement = requiresAck,
            IsPinned = isPinned
        };

        _db.Announcements.Add(announcement);
        _audit.Track(user.Id, user.FullName,
            "PostAnnouncement", "Announcement", null,
            $"[{announcement.CategoryLabel}] '{announcement.Title}' ? {targetRoles}",
            constituencyId);
        await _db.SaveChangesAsync();

        TempData["Message"] = "Announcement posted successfully.";
        return RedirectToPage("/Announcements/Index");
    }

    public static List<UserRole> GetAllowedTargetRoles(UserRole posterRole) => posterRole switch
    {
        UserRole.SuperAdmin      => Enum.GetValues<UserRole>().ToList(),
        UserRole.Admin           => Enum.GetValues<UserRole>().Where(r => r != UserRole.SuperAdmin).ToList(),
        UserRole.Candidate       => new() { UserRole.CampaignManager, UserRole.FieldWorker, UserRole.BoothAgent },
        UserRole.CampaignManager => new() { UserRole.FieldWorker, UserRole.BoothAgent },
        UserRole.FieldWorker     => new() { UserRole.CampaignManager },
        UserRole.BoothAgent      => new() { UserRole.CampaignManager },
        _                        => new()
    };
}
