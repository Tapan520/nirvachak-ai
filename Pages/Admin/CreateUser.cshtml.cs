using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nirvachak_AI.Domain.Entities;
using Nirvachak_AI.Domain.Enums;
using Nirvachak_AI.Infrastructure.Data;

namespace Nirvachak_AI.Pages.Admin;

[Authorize(Roles = "Admin,CampaignManager,SuperAdmin")]
public class CreateUserModel : PageModel
{
    private readonly UserManager<AppUser> _userManager;
    private readonly AppDbContext _db;

    public CreateUserModel(UserManager<AppUser> userManager, AppDbContext db)
    {
        _userManager = userManager;
        _db = db;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();
    public List<SelectListItem> ConstituencyItems { get; set; } = new();
    public List<SelectListItem> RoleItems { get; set; } = new();
    public bool IsSuperAdmin { get; set; }

    public class InputModel
    {
        [Required]
        public string FullName { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        public string? PhoneNumber { get; set; }

        [Required]
        public UserRole Role { get; set; }

        public int? ConstituencyId { get; set; }
        public string? AssignedBoothNumbers { get; set; }
        public string? AssignedWard { get; set; }

        [Required, MinLength(6)]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
    }

    public async Task OnGetAsync()
    {
        await LoadFormDataAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await LoadFormDataAsync();
        if (!ModelState.IsValid) return Page();

        bool isSuperAdmin = User.IsInRole(nameof(UserRole.SuperAdmin));
        bool isAdmin      = User.IsInRole(nameof(UserRole.Admin));
        var currentUser   = await _userManager.GetUserAsync(User);

        if (isSuperAdmin)
        {
            // SuperAdmin can create any role; ConstituencyId comes from form
        }
        else if (isAdmin)
        {
            // Admin cannot create SuperAdmin or another Admin
            if (Input.Role == UserRole.SuperAdmin || Input.Role == UserRole.Admin)
            {
                ModelState.AddModelError("", "Admins can only create CampaignManager, Candidate, FieldWorker or BoothAgent users.");
                return Page();
            }
            Input.ConstituencyId = currentUser?.ConstituencyId;
        }
        else
        {
            // CampaignManager can only create FieldWorker or BoothAgent
            if (Input.Role != UserRole.FieldWorker && Input.Role != UserRole.BoothAgent)
            {
                ModelState.AddModelError("", "You can only create FieldWorker or BoothAgent users.");
                return Page();
            }
            Input.ConstituencyId = currentUser?.ConstituencyId;
        }

        var user = new AppUser
        {
            UserName = Input.Email,
            Email = Input.Email,
            FullName = Input.FullName,
            PhoneNumber = Input.PhoneNumber,
            Role = Input.Role,
            ConstituencyId = Input.ConstituencyId,
            AssignedBoothNumbers = Input.AssignedBoothNumbers,
            AssignedWard = Input.AssignedWard,
            EmailConfirmed = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, Input.Password);
        if (!result.Succeeded)
        {
            foreach (var e in result.Errors)
                ModelState.AddModelError("", e.Description);
            return Page();
        }
        await _userManager.AddToRoleAsync(user, Input.Role.ToString());
        TempData["Message"] = $"User '{Input.FullName}' created successfully.";
        return RedirectToPage("/Admin/Index");
    }

    private async Task LoadFormDataAsync()
    {
        bool isSuperAdmin = User.IsInRole(nameof(UserRole.SuperAdmin));
        IsSuperAdmin = isSuperAdmin;
        bool isAdmin      = User.IsInRole(nameof(UserRole.Admin));
        var currentUser   = await _userManager.GetUserAsync(User);

        IQueryable<Constituency> constQuery = _db.Constituencies.OrderBy(c => c.Name);
        if (!isSuperAdmin && !isAdmin && currentUser?.ConstituencyId != null)
            constQuery = constQuery.Where(c => c.Id == currentUser.ConstituencyId);
        else if (isAdmin && currentUser?.ConstituencyId != null)
            constQuery = constQuery.Where(c => c.Id == currentUser.ConstituencyId);

        ConstituencyItems = constQuery
            .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = $"{c.Name} ({c.Code})" })
            .ToList();

        UserRole[] allowedRoles = isSuperAdmin
            ? Enum.GetValues<UserRole>()                                                               // SuperAdmin: all roles
            : isAdmin
                ? new[] { UserRole.CampaignManager, UserRole.Candidate, UserRole.FieldWorker, UserRole.BoothAgent } // Admin: no Admin/SuperAdmin
                : new[] { UserRole.FieldWorker, UserRole.BoothAgent };                                 // Manager: ground level only

        RoleItems = allowedRoles.Select(r => new SelectListItem { Value = r.ToString(), Text = r.ToString() }).ToList();
    }
}

