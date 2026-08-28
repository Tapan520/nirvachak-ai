using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Nirvachak_AI.Domain.Entities;
using Nirvachak_AI.Domain.Enums;
using Nirvachak_AI.Infrastructure.Data;
using Nirvachak_AI.Infrastructure.Services;

namespace Nirvachak_AI.Pages.Admin;

[Authorize(Roles = "Admin,CampaignManager,SuperAdmin")]
public class EditUserModel : PageModel
{
    private readonly UserManager<AppUser> _userManager;
    private readonly AppDbContext _db;
    private readonly AuditService _audit;

    public EditUserModel(UserManager<AppUser> userManager, AppDbContext db, AuditService audit)
    {
        _userManager = userManager;
        _db          = db;
        _audit       = audit;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    [BindProperty]
    public string UserId { get; set; } = string.Empty;

    public List<SelectListItem> ConstituencyItems { get; set; } = new();
    public List<SelectListItem> RoleItems { get; set; } = new();

    public class InputModel
    {
        [Required, MaxLength(100)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required, EmailAddress, MaxLength(200)]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [Phone, MaxLength(15)]
        [Display(Name = "Phone Number")]
        public string? PhoneNumber { get; set; }

        [MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        public string? NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match.")]
        [Display(Name = "Confirm New Password")]
        public string? ConfirmPassword { get; set; }

        [Required]
        public UserRole Role { get; set; }

        [Display(Name = "Constituency")]
        public int? ConstituencyId { get; set; }

        [Display(Name = "Assigned Booth Numbers")]
        public string? AssignedBoothNumbers { get; set; }

        [Display(Name = "Assigned Ward")]
        public string? AssignedWard { get; set; }

        [Display(Name = "Account Active")]
        public bool IsActive { get; set; } = true;
    }

    public async Task<IActionResult> OnGetAsync(string id)
    {
        var targetUser = await _userManager.FindByIdAsync(id);
        if (targetUser == null) return NotFound();

        var currentUser = await _userManager.GetUserAsync(User);
        bool isSuperAdmin = User.IsInRole(nameof(UserRole.SuperAdmin));
        bool isAdmin      = User.IsInRole(nameof(UserRole.Admin));

        if (!isSuperAdmin)
        {
            // No-one below SuperAdmin can edit a SuperAdmin account
            if (targetUser.Role == UserRole.SuperAdmin) return Forbid();

            if (!isAdmin)
            {
                // CampaignManager: only FieldWorker/BoothAgent/VoterManager in own constituency
                if (targetUser.Role != UserRole.FieldWorker && targetUser.Role != UserRole.BoothAgent && targetUser.Role != UserRole.VoterManager)
                    return Forbid();
                if (targetUser.ConstituencyId != currentUser?.ConstituencyId)
                    return Forbid();
            }
            else
            {
                // Admin: only users in own constituency, cannot touch Admin or SuperAdmin
                if (targetUser.Role == UserRole.Admin && targetUser.Id != currentUser?.Id)
                    return Forbid();
                if (targetUser.ConstituencyId != currentUser?.ConstituencyId)
                    return Forbid();
            }
        }

        UserId = id;
        Input = new InputModel
        {
            FullName             = targetUser.FullName,
            Email                = targetUser.Email ?? string.Empty,
            PhoneNumber          = targetUser.PhoneNumber,
            Role                 = targetUser.Role,
            ConstituencyId       = targetUser.ConstituencyId,
            AssignedBoothNumbers = targetUser.AssignedBoothNumbers,
            AssignedWard         = targetUser.AssignedWard,
            IsActive             = targetUser.IsActive
        };

        await LoadFormDataAsync(isSuperAdmin, isAdmin, currentUser);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var targetUser = await _userManager.FindByIdAsync(UserId);
        if (targetUser == null) return NotFound();

        var currentUser = await _userManager.GetUserAsync(User);
        bool isSuperAdmin = User.IsInRole(nameof(UserRole.SuperAdmin));
        bool isAdmin      = User.IsInRole(nameof(UserRole.Admin));

        if (!isSuperAdmin)
        {
            if (targetUser.Role == UserRole.SuperAdmin) return Forbid();

            if (!isAdmin)
            {
                if (targetUser.Role != UserRole.FieldWorker && targetUser.Role != UserRole.BoothAgent && targetUser.Role != UserRole.VoterManager)
                    return Forbid();
                if (targetUser.ConstituencyId != currentUser?.ConstituencyId)
                    return Forbid();
            }
            else
            {
                if (targetUser.Role == UserRole.Admin && targetUser.Id != currentUser?.Id)
                    return Forbid();
                if (targetUser.ConstituencyId != currentUser?.ConstituencyId)
                    return Forbid();
            }
        }

        await LoadFormDataAsync(isSuperAdmin, isAdmin, currentUser);

        // Password fields are optional — remove validation errors when left blank
        if (string.IsNullOrWhiteSpace(Input.NewPassword))
        {
            ModelState.Remove("Input.NewPassword");
            ModelState.Remove("Input.ConfirmPassword");
        }

        if (!ModelState.IsValid) return Page();

        // ── Email uniqueness check ────────────────────────────────────────
        var trimmedEmail    = Input.Email.Trim();
        var normalizedEmail = trimmedEmail.ToUpperInvariant();
        if (!string.Equals(targetUser.NormalizedEmail, normalizedEmail, StringComparison.Ordinal))
        {
            var existing = await _userManager.FindByEmailAsync(trimmedEmail);
            if (existing != null && existing.Id != targetUser.Id)
            {
                ModelState.AddModelError("Input.Email", "This email address is already in use by another account.");
                return Page();
            }
        }

        // ── Track changes for audit ───────────────────────────────────────
        var changes = new List<string>();

        if (targetUser.FullName != Input.FullName.Trim())
        {
            changes.Add($"Name: '{targetUser.FullName}' → '{Input.FullName.Trim()}'");
            targetUser.FullName = Input.FullName.Trim();
        }

        if (!string.Equals(targetUser.NormalizedEmail, normalizedEmail, StringComparison.Ordinal))
        {
            changes.Add($"Email: '{targetUser.Email}' → '{trimmedEmail}'");
            targetUser.Email              = trimmedEmail;
            targetUser.NormalizedEmail    = normalizedEmail;
            targetUser.UserName           = trimmedEmail;
            targetUser.NormalizedUserName = normalizedEmail;
        }

        if (targetUser.PhoneNumber != Input.PhoneNumber)
        {
            changes.Add("Phone updated");
            targetUser.PhoneNumber = Input.PhoneNumber;
        }

        // ── Password change (optional) ────────────────────────────────────
        if (!string.IsNullOrWhiteSpace(Input.NewPassword))
        {
            var token  = await _userManager.GeneratePasswordResetTokenAsync(targetUser);
            var result = await _userManager.ResetPasswordAsync(targetUser, token, Input.NewPassword);
            if (!result.Succeeded)
            {
                foreach (var err in result.Errors)
                    ModelState.AddModelError(string.Empty, err.Description);
                return Page();
            }
            changes.Add("Password changed");
        }

        targetUser.AssignedBoothNumbers = Input.AssignedBoothNumbers;
        targetUser.AssignedWard         = Input.AssignedWard;

        if (targetUser.IsActive != Input.IsActive)
        {
            changes.Add(Input.IsActive ? "Account re-activated" : "Account deactivated");
            targetUser.IsActive = Input.IsActive;
        }

        if (isSuperAdmin || isAdmin)
        {
            // Admin cannot elevate a role to Admin or SuperAdmin
            var targetRole = Input.Role;
            if (isAdmin && !isSuperAdmin &&
                (targetRole == UserRole.Admin || targetRole == UserRole.SuperAdmin))
            {
                targetRole = targetUser.Role; // silently keep existing role
            }

            if (targetUser.Role != targetRole)
            {
                changes.Add($"Role: '{targetUser.Role}' → '{targetRole}'");
                var existingRoles = await _userManager.GetRolesAsync(targetUser);
                await _userManager.RemoveFromRolesAsync(targetUser, existingRoles);
                await _userManager.AddToRoleAsync(targetUser, targetRole.ToString());
                targetUser.Role = targetRole;
            }
            if (isSuperAdmin && targetUser.ConstituencyId != Input.ConstituencyId)
            {
                changes.Add("Constituency updated");
                targetUser.ConstituencyId = Input.ConstituencyId;
            }
        }

        var updateResult = await _userManager.UpdateAsync(targetUser);
        if (!updateResult.Succeeded)
        {
            foreach (var err in updateResult.Errors)
                ModelState.AddModelError(string.Empty, err.Description);
            return Page();
        }

        // ── Audit log ─────────────────────────────────────────────────────
        var summary = changes.Any() ? string.Join("; ", changes) : "No changes made";
        await _audit.LogAsync(
            currentUser!.Id, currentUser.FullName,
            "UpdateUser", "AppUser", targetUser.Id,
            $"Edited '{targetUser.FullName}' ({targetUser.Email}) — {summary}",
            currentUser.ConstituencyId);

        TempData["Message"] = $"User '{targetUser.FullName}' updated successfully.";
        return RedirectToPage("/Admin/Index");
    }

    private async Task LoadFormDataAsync(bool isSuperAdmin, bool isAdmin, AppUser? currentUser)
    {
        IQueryable<Constituency> constQuery = _db.Constituencies.OrderBy(c => c.Name);
        if (!isSuperAdmin && currentUser?.ConstituencyId != null)
            constQuery = constQuery.Where(c => c.Id == currentUser.ConstituencyId);

        ConstituencyItems = await constQuery
            .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = $"{c.Name} ({c.Code})" })
            .ToListAsync();

        UserRole[] allowedRoles = isSuperAdmin
            ? Enum.GetValues<UserRole>()
            : isAdmin
                ? new[] { UserRole.CampaignManager, UserRole.Candidate, UserRole.FieldWorker, UserRole.BoothAgent, UserRole.VoterManager }
                : new[] { UserRole.FieldWorker, UserRole.BoothAgent, UserRole.VoterManager };

        RoleItems = allowedRoles
            .Select(r => new SelectListItem { Value = r.ToString(), Text = r.ToString() })
            .ToList();
    }
}
