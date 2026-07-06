using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Nirvachak_AI.Domain.Entities;
using Nirvachak_AI.Domain.Enums;
using Nirvachak_AI.Infrastructure.Data;

namespace Nirvachak_AI.Pages.Broadcast;

[Authorize(Roles = "Admin,SuperAdmin,CampaignManager,Candidate")]
public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _userManager;
    public IndexModel(AppDbContext db, UserManager<AppUser> userManager) { _db = db; _userManager = userManager; }

    public List<MessageBroadcast> Broadcasts { get; set; } = new();
    public List<MessageTemplate> Templates { get; set; } = new();
    public List<Constituency> Constituencies { get; set; } = new();
    public bool IsAdmin { get; set; }

    [BindProperty(SupportsGet = true)] public int? ConstituencyFilter { get; set; }

    public async Task OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        IsAdmin = user?.Role == UserRole.Admin || user?.Role == UserRole.SuperAdmin;
        if (IsAdmin) Constituencies = await _db.Constituencies.OrderBy(c => c.Name).ToListAsync();

        int? cId = IsAdmin ? ConstituencyFilter : user?.ConstituencyId;

        var bQ = _db.MessageBroadcasts.Include(b => b.Template).AsQueryable();
        if (cId.HasValue) bQ = bQ.Where(b => b.ConstituencyId == cId);
        Broadcasts = await bQ.OrderByDescending(b => b.CreatedAt).ToListAsync();

        var tQ = _db.MessageTemplates.AsQueryable();
        if (cId.HasValue) tQ = tQ.Where(t => t.ConstituencyId == cId);
        Templates = await tQ.OrderBy(t => t.Title).ToListAsync();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var b = await _db.MessageBroadcasts.FindAsync(id);
        if (b != null) { _db.MessageBroadcasts.Remove(b); await _db.SaveChangesAsync(); }
        TempData["Message"] = "Broadcast removed.";
        return RedirectToPage();
    }
}
