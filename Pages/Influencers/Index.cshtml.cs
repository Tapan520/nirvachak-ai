using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Nirvachak_AI.Domain.Entities;
using Nirvachak_AI.Domain.Enums;
using Nirvachak_AI.Infrastructure.Data;

namespace Nirvachak_AI.Pages.Influencers;

[Authorize(Roles = "Admin,SuperAdmin,CampaignManager,Candidate")]
public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _userManager;
    public IndexModel(AppDbContext db, UserManager<AppUser> userManager) { _db = db; _userManager = userManager; }

    public List<Influencer> Influencers { get; set; } = new();
    public int TotalEstimatedReach { get; set; }
    public int FavourCount { get; set; }
    public int UnknownCount { get; set; }

    [BindProperty(SupportsGet = true)]
    public InfluencerAlignment? FilterAlignment { get; set; }

    public async Task OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        var isSuperAdmin = user?.Role == UserRole.SuperAdmin;
        int? cId = user?.ConstituencyId;

        IQueryable<Influencer> q = _db.Influencers.Where(i => i.IsActive).AsNoTracking();
        if (!isSuperAdmin && cId.HasValue)
            q = q.Where(i => i.ConstituencyId == cId.Value);
        else if (!isSuperAdmin && !cId.HasValue)
            q = q.Where(i => false);
        if (FilterAlignment.HasValue) q = q.Where(i => i.Alignment == FilterAlignment.Value);

        Influencers = await q.OrderByDescending(i => i.EstimatedFollowers).ToListAsync();
        TotalEstimatedReach = Influencers.Sum(i => i.EstimatedFollowers ?? 0);
        FavourCount  = Influencers.Count(i => i.Alignment == InfluencerAlignment.Favour);
        UnknownCount = Influencers.Count(i => i.Alignment == InfluencerAlignment.Unknown);
    }
}
