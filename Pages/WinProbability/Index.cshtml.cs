using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Nirvachak_AI.Domain.Entities;
using Nirvachak_AI.Domain.Enums;
using Nirvachak_AI.Infrastructure.Data;
using Nirvachak_AI.Infrastructure.Services;

namespace Nirvachak_AI.Pages.WinProbability;

[Authorize(Roles = "Admin,SuperAdmin,CampaignManager,Candidate")]
public class IndexModel : PageModel
{
    private readonly WinProbabilityService _svc;
    private readonly UserManager<AppUser>  _userManager;
    private readonly AppDbContext          _db;

    public IndexModel(WinProbabilityService svc, UserManager<AppUser> userManager, AppDbContext db)
    {
        _svc         = svc;
        _userManager = userManager;
        _db          = db;
    }

    [BindProperty(SupportsGet = true)]
    public int? SelectedConstituencyId { get; set; }

    public WinProbabilityResult? Result            { get; set; }
    public List<Constituency>   Constituencies     { get; set; } = new();
    public Constituency?        ActiveConstituency { get; set; }
    public bool                 IsSuperAdmin       { get; set; }
    public string               GeneratedAt        { get; set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToPage("/Account/Login");

        // Only SuperAdmin can select any constituency via the dropdown
        IsSuperAdmin = user.Role is UserRole.SuperAdmin;

        if (IsSuperAdmin)
            Constituencies = await _db.Constituencies.OrderBy(c => c.Name).ToListAsync();

        int? cId = IsSuperAdmin ? (SelectedConstituencyId ?? user.ConstituencyId) : user.ConstituencyId;
        if (!cId.HasValue) return Page();

        ActiveConstituency = await _db.Constituencies.FindAsync(cId.Value);
        Result             = await _svc.ComputeAsync(cId.Value);
        GeneratedAt        = DateTime.Now.ToString("dd MMM yyyy, h:mm tt");

        return Page();
    }
}
