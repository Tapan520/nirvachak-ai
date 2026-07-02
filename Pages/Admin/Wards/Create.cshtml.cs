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

namespace Nirvachak_AI.Pages.Admin.Wards;

[Authorize(Roles = "Admin,CampaignManager,SuperAdmin")]
public class CreateModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _userManager;

    public CreateModel(AppDbContext db, UserManager<AppUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public int? ConstituencyId { get; set; }

    public List<SelectListItem> ConstituencyItems { get; set; } = new();

    public class InputModel
    {
        [Required, StringLength(20)]
        public string WardNumber { get; set; } = string.Empty;

        [Required, StringLength(100)]
        public string WardName { get; set; } = string.Empty;

        [StringLength(250)]
        public string? Description { get; set; }

        [Required]
        public int ConstituencyId { get; set; }
    }

    public async Task OnGetAsync()
    {
        await LoadConstituenciesAsync();
        if (ConstituencyId.HasValue)
            Input.ConstituencyId = ConstituencyId.Value;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await LoadConstituenciesAsync();
        if (!ModelState.IsValid) return Page();

        var user = await _userManager.GetUserAsync(User);
        bool isAdmin = User.IsInRole(nameof(UserRole.Admin)) || User.IsInRole(nameof(UserRole.SuperAdmin));
        if (!isAdmin && user?.ConstituencyId != Input.ConstituencyId)
        {
            ModelState.AddModelError("", "You can only add wards to your assigned constituency.");
            return Page();
        }

        var ward = new Ward
        {
            WardNumber = Input.WardNumber.Trim(),
            WardName = Input.WardName.Trim(),
            Description = Input.Description?.Trim(),
            ConstituencyId = Input.ConstituencyId,
            CreatedAt = DateTime.UtcNow
        };
        _db.Wards.Add(ward);
        await _db.SaveChangesAsync();
        TempData["Message"] = $"Ward '{ward.WardName}' created successfully.";
        return RedirectToPage("/Admin/Wards/Index", new { ConstituencyId = ward.ConstituencyId });
    }

    private async Task LoadConstituenciesAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        bool isAdmin = User.IsInRole(nameof(UserRole.Admin)) || User.IsInRole(nameof(UserRole.SuperAdmin));

        IQueryable<Constituency> query = _db.Constituencies.OrderBy(c => c.Name);
        if (!isAdmin && user?.ConstituencyId != null)
            query = query.Where(c => c.Id == user.ConstituencyId);

        ConstituencyItems = await query
            .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = $"{c.Name} ({c.Code})" })
            .ToListAsync();
    }
}
