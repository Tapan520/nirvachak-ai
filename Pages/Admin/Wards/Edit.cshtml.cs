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
public class EditModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _userManager;

    public EditModel(AppDbContext db, UserManager<AppUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    [BindProperty]
    public int WardId { get; set; }

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

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var ward = await _db.Wards.FindAsync(id);
        if (ward == null) return NotFound();

        var user = await _userManager.GetUserAsync(User);
        bool isSuperAdmin = User.IsInRole(nameof(UserRole.SuperAdmin));
        if (!isSuperAdmin && user?.ConstituencyId != ward.ConstituencyId) return Forbid();

        WardId = id;
        Input = new InputModel
        {
            WardNumber = ward.WardNumber,
            WardName = ward.WardName,
            Description = ward.Description,
            ConstituencyId = ward.ConstituencyId
        };
        await LoadConstituenciesAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await LoadConstituenciesAsync();
        if (!ModelState.IsValid) return Page();

        var ward = await _db.Wards.FindAsync(WardId);
        if (ward == null) return NotFound();

        var user = await _userManager.GetUserAsync(User);
        bool isSuperAdmin = User.IsInRole(nameof(UserRole.SuperAdmin));
        if (!isSuperAdmin && user?.ConstituencyId != ward.ConstituencyId) return Forbid();

        ward.WardNumber = Input.WardNumber.Trim();
        ward.WardName = Input.WardName.Trim();
        ward.Description = Input.Description?.Trim();
        ward.ConstituencyId = isSuperAdmin ? Input.ConstituencyId : ward.ConstituencyId;

        await _db.SaveChangesAsync();
        TempData["Message"] = $"Ward '{ward.WardName}' updated.";
        return RedirectToPage("/Admin/Wards/Index", new { ConstituencyId = ward.ConstituencyId });
    }

    private async Task LoadConstituenciesAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        bool isSuperAdmin = User.IsInRole(nameof(UserRole.SuperAdmin));

        IQueryable<Constituency> query = _db.Constituencies.OrderBy(c => c.Name);
        if (!isSuperAdmin && user?.ConstituencyId != null)
            query = query.Where(c => c.Id == user.ConstituencyId);

        ConstituencyItems = await query
            .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = $"{c.Name} ({c.Code})" })
            .ToListAsync();
    }
}
