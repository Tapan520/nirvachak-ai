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

[Authorize(Roles = "Admin,CampaignManager")]
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
        bool isAdmin = User.IsInRole(nameof(UserRole.Admin));

        IQueryable<AppUser> query = _db.Users.Include(u => u.Constituency).OrderBy(u => u.FullName);

        if (!isAdmin)
        {
            // Manager sees only FieldWorker and BoothAgent in their constituency
            query = query.Where(u =>
                u.ConstituencyId == currentUser!.ConstituencyId &&
                (u.Role == UserRole.FieldWorker || u.Role == UserRole.BoothAgent));
        }

        Users = await query.ToListAsync();

        IQueryable<AuditLog> auditQ = _db.AuditLogs.OrderByDescending(a => a.CreatedAt);
        if (!string.IsNullOrEmpty(AuditUserFilter))   auditQ = auditQ.Where(a => a.UserName == AuditUserFilter);
        if (!string.IsNullOrEmpty(AuditActionFilter)) auditQ = auditQ.Where(a => a.Action == AuditActionFilter);
        if (AuditDateFrom.HasValue) auditQ = auditQ.Where(a => a.CreatedAt >= AuditDateFrom.Value.ToUniversalTime());
        if (AuditDateTo.HasValue)   auditQ = auditQ.Where(a => a.CreatedAt <= AuditDateTo.Value.ToUniversalTime().AddDays(1));

        AuditLogs   = await auditQ.Take(50).ToListAsync();
        AuditUsers  = await _db.AuditLogs.Select(a => a.UserName).Distinct().OrderBy(x => x).ToListAsync();
        AuditActions = await _db.AuditLogs.Select(a => a.Action).Distinct().OrderBy(x => x).ToListAsync();
    }

    public async Task<IActionResult> OnPostToggleAsync(string userId)
    {
        var currentUser = await _userManager.GetUserAsync(User);
        bool isAdmin = User.IsInRole(nameof(UserRole.Admin));

        var user = await _userManager.FindByIdAsync(userId);
        if (user != null)
        {
            if (!isAdmin && (user.Role != UserRole.FieldWorker && user.Role != UserRole.BoothAgent))
                return Forbid();
            if (!isAdmin && user.ConstituencyId != currentUser?.ConstituencyId)
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
