using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Nirvachak_AI.Domain.Entities;
using Nirvachak_AI.Domain.Enums;
using Nirvachak_AI.Infrastructure.Data;

namespace Nirvachak_AI.Pages.Announcements;

public class AnnouncementViewModel
{
    public Announcement Announcement { get; set; } = null!;
    public bool IsAcknowledged { get; set; }
    public int AcknowledgementCount { get; set; }
}

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _userManager;

    public IndexModel(AppDbContext db, UserManager<AppUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    [BindProperty(SupportsGet = true)]
    public string? Category { get; set; }

    public List<AnnouncementViewModel> Announcements { get; set; } = new();
    public List<AnnouncementViewModel> PinnedAlerts { get; set; } = new();
    public UserRole CurrentUserRole { get; set; }
    public bool CanCreate { get; set; }
    public int UnacknowledgedCount { get; set; }

    public async Task OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return;

        CurrentUserRole = user.Role;
        CanCreate = user.Role != UserRole.BoothAgent;

        var now = DateTime.UtcNow;
        var roleStr = user.Role.ToString();

        var query = _db.Announcements
            .Include(a => a.Acknowledgements)
            .Where(a => a.IsActive && (a.ExpiresAt == null || a.ExpiresAt > now));

        // Non-admin sees only their constituency
        if (user.Role != UserRole.SuperAdmin)
            query = query.Where(a => a.ConstituencyId == null || a.ConstituencyId == user.ConstituencyId);

        // Must target this role OR be authored by this user
        query = query.Where(a =>
            a.TargetRoles == "All" ||
            a.TargetRoles.Contains(roleStr) ||
            a.CreatedByUserId == user.Id);

        if (!string.IsNullOrEmpty(Category) && Enum.TryParse<AnnouncementCategory>(Category, out var cat))
            query = query.Where(a => a.Category == cat);

        var list = await query
            .OrderByDescending(a => a.IsPinned)
            .ThenByDescending(a => a.CreatedAt)
            .ToListAsync();

        var mapped = list.Select(a => new AnnouncementViewModel
        {
            Announcement = a,
            IsAcknowledged = a.Acknowledgements.Any(x => x.UserId == user.Id),
            AcknowledgementCount = a.Acknowledgements.Count
        }).ToList();

        PinnedAlerts = mapped.Where(v => v.Announcement.IsPinned).ToList();
        Announcements = mapped.Where(v => !v.Announcement.IsPinned).ToList();

        UnacknowledgedCount = mapped.Count(v =>
            v.Announcement.RequiresAcknowledgement && !v.IsAcknowledged);
    }

    public async Task<IActionResult> OnPostAcknowledgeAsync(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Forbid();

        var already = await _db.AnnouncementAcknowledgements
            .AnyAsync(x => x.AnnouncementId == id && x.UserId == user.Id);

        if (!already)
        {
            _db.AnnouncementAcknowledgements.Add(new AnnouncementAcknowledgement
            {
                AnnouncementId = id,
                UserId = user.Id,
                UserName = user.FullName
            });
            await _db.SaveChangesAsync();
        }

        TempData["Message"] = "Acknowledged successfully.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeactivateAsync(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Forbid();

        var announcement = await _db.Announcements.FindAsync(id);
        if (announcement == null) return NotFound();

        if (user.Role != UserRole.SuperAdmin && announcement.CreatedByUserId != user.Id)
            return Forbid();

        announcement.IsActive = false;
        await _db.SaveChangesAsync();

        TempData["Message"] = "Announcement removed.";
        return RedirectToPage();
    }
}
