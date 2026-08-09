using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Nirvachak_AI.Domain.Entities;
using Nirvachak_AI.Domain.Enums;
using Nirvachak_AI.Infrastructure.Data;
using Nirvachak_AI.Infrastructure.Services;

namespace Nirvachak_AI.Pages.Admin;

[Authorize(Roles = "Admin,CampaignManager,SuperAdmin")]
public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _userManager;
    private readonly AuditService _audit;

    public IndexModel(AppDbContext db, UserManager<AppUser> userManager, AuditService audit)
    {
        _db = db;
        _userManager = userManager;
        _audit = audit;
    }

    public List<AppUser> Users { get; set; } = new();
    public List<AuditLog> AuditLogs { get; set; } = new();

    [BindProperty(SupportsGet = true)] public string? AuditUserFilter   { get; set; }
    [BindProperty(SupportsGet = true)] public string? AuditActionFilter { get; set; }
    [BindProperty(SupportsGet = true)] public DateTime? AuditDateFrom   { get; set; }
    [BindProperty(SupportsGet = true)] public DateTime? AuditDateTo     { get; set; }
    public List<string> AuditUsers   { get; set; } = new();
    public List<string> AuditActions { get; set; } = new();

    public async Task OnGetAsync()
    {
        var currentUser = await _userManager.GetUserAsync(User);
        bool isSuperAdmin = User.IsInRole(nameof(UserRole.SuperAdmin));
        bool isAdmin      = User.IsInRole(nameof(UserRole.Admin));

        IQueryable<AppUser> query = _db.Users.Include(u => u.Constituency).OrderBy(u => u.FullName);

        if (isSuperAdmin)
        {
            // SuperAdmin sees every user across all constituencies
        }
        else if (isAdmin)
        {
            // Admin sees all roles in their own constituency except SuperAdmin
            query = query.Where(u =>
                u.ConstituencyId == currentUser!.ConstituencyId &&
                u.Role != UserRole.SuperAdmin);
        }
        else
        {
            // CampaignManager sees only FieldWorker and BoothAgent in their constituency
            query = query.Where(u =>
                u.ConstituencyId == currentUser!.ConstituencyId &&
                (u.Role == UserRole.FieldWorker || u.Role == UserRole.BoothAgent));
        }

        Users = await query.ToListAsync();

        // Gap fix: scope audit logs to own constituency for non-SuperAdmin
        IQueryable<AuditLog> auditQ = _db.AuditLogs.OrderByDescending(a => a.CreatedAt);
        if (!isSuperAdmin && currentUser?.ConstituencyId != null)
            auditQ = auditQ.Where(a => a.ConstituencyId == currentUser.ConstituencyId);
        if (!string.IsNullOrEmpty(AuditUserFilter))   auditQ = auditQ.Where(a => a.UserName == AuditUserFilter);
        if (!string.IsNullOrEmpty(AuditActionFilter)) auditQ = auditQ.Where(a => a.Action == AuditActionFilter);
        if (AuditDateFrom.HasValue) auditQ = auditQ.Where(a => a.CreatedAt >= AuditDateFrom.Value.ToUniversalTime());
        if (AuditDateTo.HasValue)   auditQ = auditQ.Where(a => a.CreatedAt <= AuditDateTo.Value.ToUniversalTime().AddDays(1));

        AuditLogs    = await auditQ.Take(50).ToListAsync();

        IQueryable<AuditLog> scopedAudit = _db.AuditLogs;
        if (!isSuperAdmin && currentUser?.ConstituencyId != null)
            scopedAudit = scopedAudit.Where(a => a.ConstituencyId == currentUser.ConstituencyId);
        AuditUsers   = await scopedAudit.Select(a => a.UserName).Distinct().OrderBy(x => x).ToListAsync();
        AuditActions = await scopedAudit.Select(a => a.Action).Distinct().OrderBy(x => x).ToListAsync();
    }

    public async Task<IActionResult> OnPostDeleteLogAsync(int logId)
    {
        bool isSuperAdmin = User.IsInRole(nameof(UserRole.SuperAdmin));
        bool isAdmin      = User.IsInRole(nameof(UserRole.Admin));
        if (!isSuperAdmin && !isAdmin) return Forbid();

        var currentUser = await _userManager.GetUserAsync(User);
        var log = await _db.AuditLogs.FindAsync(logId);
        if (log != null)
        {
            // Admin can only delete logs that belong to their constituency
            if (!isSuperAdmin && log.ConstituencyId != currentUser?.ConstituencyId)
                return Forbid();

            _db.AuditLogs.Remove(log);
            await _db.SaveChangesAsync();
            await _audit.LogAsync(
                currentUser!.Id, currentUser.FullName,
                "DeleteAuditLog", "AuditLog", logId.ToString(),
                $"Deleted audit log entry #{logId}",
                currentUser.ConstituencyId);
            TempData["Message"] = "Audit log entry deleted.";
        }
        return RedirectToPage(new
        {
            AuditUserFilter,
            AuditActionFilter,
            AuditDateFrom = AuditDateFrom?.ToString("yyyy-MM-dd"),
            AuditDateTo   = AuditDateTo?.ToString("yyyy-MM-dd")
        });
    }

    public async Task<IActionResult> OnPostDeleteAllLogsAsync()
    {
        bool isSuperAdmin = User.IsInRole(nameof(UserRole.SuperAdmin));
        bool isAdmin      = User.IsInRole(nameof(UserRole.Admin));
        if (!isSuperAdmin && !isAdmin) return Forbid();

        var currentUser = await _userManager.GetUserAsync(User);

        IQueryable<AuditLog> auditQ = _db.AuditLogs;
        // Admin can only delete logs scoped to their own constituency
        if (!isSuperAdmin && currentUser?.ConstituencyId != null)
            auditQ = auditQ.Where(a => a.ConstituencyId == currentUser.ConstituencyId);
        if (!string.IsNullOrEmpty(AuditUserFilter))   auditQ = auditQ.Where(a => a.UserName == AuditUserFilter);
        if (!string.IsNullOrEmpty(AuditActionFilter)) auditQ = auditQ.Where(a => a.Action == AuditActionFilter);
        if (AuditDateFrom.HasValue) auditQ = auditQ.Where(a => a.CreatedAt >= AuditDateFrom.Value.ToUniversalTime());
        if (AuditDateTo.HasValue)   auditQ = auditQ.Where(a => a.CreatedAt <= AuditDateTo.Value.ToUniversalTime().AddDays(1));

        var logsToDelete = await auditQ.ToListAsync();
        int count = logsToDelete.Count;
        _db.AuditLogs.RemoveRange(logsToDelete);
        await _db.SaveChangesAsync();

        await _audit.LogAsync(
            currentUser!.Id, currentUser.FullName,
            "DeleteAuditLogs", "AuditLog", null,
            $"Bulk deleted {count} audit log entries",
            currentUser.ConstituencyId);

        TempData["Message"] = $"{count} audit log record(s) deleted.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostToggleAsync(string userId)
    {
        var currentUser = await _userManager.GetUserAsync(User);
        bool isAdmin = User.IsInRole(nameof(UserRole.Admin));

        bool isSuperAdmin = User.IsInRole(nameof(UserRole.SuperAdmin));

        var user = await _userManager.FindByIdAsync(userId);
        if (user != null)
        {
            // No-one can toggle a SuperAdmin account
            if (user.Role == UserRole.SuperAdmin) return Forbid();
            if (!isSuperAdmin && !isAdmin && (user.Role != UserRole.FieldWorker && user.Role != UserRole.BoothAgent))
                return Forbid();
            if (!isSuperAdmin && user.ConstituencyId != currentUser?.ConstituencyId)
                return Forbid();

            user.IsActive = !user.IsActive;
            await _userManager.UpdateAsync(user);

            await _audit.LogAsync(
                currentUser!.Id, currentUser.FullName,
                user.IsActive ? "EnableUser" : "DisableUser", "AppUser", userId,
                $"User '{user.FullName}' ({user.Email}) {(user.IsActive ? "enabled" : "disabled")}",
                currentUser.ConstituencyId);

            TempData["Message"] = $"User {user.FullName} has been {(user.IsActive ? "enabled" : "disabled")}.";
        }
        return RedirectToPage();
    }
}
