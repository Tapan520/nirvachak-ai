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
public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _userManager;

    public IndexModel(AppDbContext db, UserManager<AppUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public List<Ward> Wards { get; set; } = new();
    public List<SelectListItem> ConstituencyItems { get; set; } = new();
    public string? SelectedConstituencyName { get; set; }

    [Microsoft.AspNetCore.Mvc.BindProperty(SupportsGet = true)]
    public int? ConstituencyId { get; set; }

    public async Task OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        bool isSuperAdmin = User.IsInRole(nameof(UserRole.SuperAdmin));

        if (!isSuperAdmin && user?.ConstituencyId != null)
            ConstituencyId = user.ConstituencyId;

        IQueryable<Constituency> constQuery = _db.Constituencies.OrderBy(c => c.Name);
        if (!isSuperAdmin && user?.ConstituencyId != null)
            constQuery = constQuery.Where(c => c.Id == user.ConstituencyId);

        ConstituencyItems = await constQuery
            .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = $"{c.Name} ({c.Code})" })
            .ToListAsync();

        IQueryable<Ward> wardQuery = _db.Wards.Include(w => w.Constituency).OrderBy(w => w.ConstituencyId).ThenBy(w => w.WardNumber);

        if (!isSuperAdmin && user?.ConstituencyId != null)
            wardQuery = wardQuery.Where(w => w.ConstituencyId == user.ConstituencyId);
        else if (ConstituencyId.HasValue)
            wardQuery = wardQuery.Where(w => w.ConstituencyId == ConstituencyId);

        Wards = await wardQuery.ToListAsync();

        if (ConstituencyId.HasValue)
            SelectedConstituencyName = ConstituencyItems.FirstOrDefault(c => c.Value == ConstituencyId.ToString())?.Text;
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var ward = await _db.Wards.FindAsync(id);
        if (ward != null)
        {
            var user = await _userManager.GetUserAsync(User);
            bool isSuperAdmin = User.IsInRole(nameof(UserRole.SuperAdmin));
            if (!isSuperAdmin && user?.ConstituencyId != ward.ConstituencyId)
                return Forbid();

            _db.Wards.Remove(ward);
            await _db.SaveChangesAsync();
            TempData["Message"] = $"Ward '{ward.WardName}' deleted.";
        }
        return RedirectToPage(new { ConstituencyId });
    }
}
