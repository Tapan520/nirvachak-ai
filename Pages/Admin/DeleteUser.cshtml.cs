using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Nirvachak_AI.Domain.Entities;
using Nirvachak_AI.Domain.Enums;

namespace Nirvachak_AI.Pages.Admin;

[Authorize(Roles = "Admin,SuperAdmin")]
public class DeleteUserModel : PageModel
{
    private readonly UserManager<AppUser> _userManager;
    private readonly IWebHostEnvironment _env;
    public DeleteUserModel(UserManager<AppUser> userManager, IWebHostEnvironment env)
    {
        _userManager = userManager;
        _env = env;
    }

    public AppUser? TargetUser { get; set; }

    public async Task<IActionResult> OnGetAsync(string id)
    {
        var currentUser = await _userManager.GetUserAsync(User);
        bool isSuperAdmin = User.IsInRole(nameof(UserRole.SuperAdmin));
        TargetUser = await _userManager.FindByIdAsync(id);
        if (TargetUser == null) return NotFound();
        if (TargetUser.Role == UserRole.SuperAdmin) return Forbid();
        if (!isSuperAdmin && TargetUser.ConstituencyId != currentUser?.ConstituencyId) return Forbid();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string id)
    {
        var currentUser = await _userManager.GetUserAsync(User);
        bool isSuperAdmin = User.IsInRole(nameof(UserRole.SuperAdmin));
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();
        if (user.Role == UserRole.SuperAdmin) return Forbid();
        if (!isSuperAdmin && user.ConstituencyId != currentUser?.ConstituencyId) return Forbid();
        if (user.Id == currentUser?.Id)
        {
            TempData["Error"] = "You cannot delete your own account.";
            return RedirectToPage("/Admin/Index");
        }

        var result = await _userManager.DeleteAsync(user);
        if (result.Succeeded)
        {
            // Clean up any candidate photo from disk
            if (!string.IsNullOrEmpty(user.CandidatePhotoUrl))
            {
                var rel = user.CandidatePhotoUrl.TrimStart('/');
                var abs = Path.Combine(_env.WebRootPath, rel.Replace('/', Path.DirectorySeparatorChar));
                if (System.IO.File.Exists(abs))
                {
                    try { System.IO.File.Delete(abs); } catch { /* ignore */ }
                }
            }
            TempData["Message"] = $"User '{user.FullName}' deleted.";
        }
        else
            TempData["Error"] = "Error deleting user: " + string.Join(", ", result.Errors.Select(e => e.Description));

        return RedirectToPage("/Admin/Index");
    }
}
