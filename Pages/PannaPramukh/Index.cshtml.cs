using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Nirvachak_AI.Domain.Entities;
using Nirvachak_AI.Domain.Enums;
using Nirvachak_AI.Infrastructure.Data;

namespace Nirvachak_AI.Pages.PannaPramukh;

[Authorize(Roles = "Admin,SuperAdmin,CampaignManager,Candidate")]
public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _userManager;
    public IndexModel(AppDbContext db, UserManager<AppUser> userManager) { _db = db; _userManager = userManager; }

    public List<Domain.Entities.PannaPramukh> PannaPramukhs { get; set; } = new();
    public List<Constituency> Constituencies { get; set; } = new();
    public bool IsAdmin { get; set; }
    public int TotalAssigned { get; set; }
    public int TotalContacted { get; set; }
    public int CoveragePercent { get; set; }

    [BindProperty(SupportsGet = true)] public int? ConstituencyFilter { get; set; }
    [BindProperty(SupportsGet = true)] public int? BoothFilter { get; set; }

    public async Task OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        IsAdmin = user?.Role == UserRole.SuperAdmin;
        if (IsAdmin) Constituencies = await _db.Constituencies.OrderBy(c => c.Name).ToListAsync();

        var q = _db.PannaPramukhs.AsQueryable();
        if (IsAdmin)
        {
            if (ConstituencyFilter.HasValue) q = q.Where(p => p.ConstituencyId == ConstituencyFilter);
        }
        else if (user?.ConstituencyId.HasValue == true)
            q = q.Where(p => p.ConstituencyId == user.ConstituencyId);

        if (BoothFilter.HasValue) q = q.Where(p => p.BoothNumber == BoothFilter);

        PannaPramukhs = await q.OrderBy(p => p.BoothNumber).ThenBy(p => p.PannaNumber).ToListAsync();
        TotalAssigned  = PannaPramukhs.Sum(p => p.TotalVotersAssigned);
        TotalContacted = PannaPramukhs.Sum(p => p.VotersContacted);
        CoveragePercent = TotalAssigned > 0 ? (int)Math.Round((double)TotalContacted / TotalAssigned * 100) : 0;
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var item = await _db.PannaPramukhs.FindAsync(id);
        if (item != null) { _db.PannaPramukhs.Remove(item); await _db.SaveChangesAsync(); }
        TempData["Message"] = "Panna Pramukh deleted.";
        return RedirectToPage();
    }
}
