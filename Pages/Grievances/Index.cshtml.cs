using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Nirvachak_AI.Domain.Entities;
using Nirvachak_AI.Domain.Enums;
using Nirvachak_AI.Infrastructure.Data;

namespace Nirvachak_AI.Pages.Grievances;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _userManager;

    public IndexModel(AppDbContext db, UserManager<AppUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    [BindProperty(SupportsGet = true)]
    public int? SelectedConstituencyId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SelectedWard { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? SelectedBoothNumber { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? PriorityFilter { get; set; }

    public List<Constituency> Constituencies { get; set; } = new();
    public List<Ward> Wards { get; set; } = new();
    public List<Booth> Booths { get; set; } = new();
    public List<Grievance> Grievances { get; set; } = new();
    public int OpenCount { get; set; }
    public int InProgressCount { get; set; }
    public int ResolvedCount { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user?.Role == UserRole.FieldWorker || user?.Role == UserRole.BoothAgent)
            return Forbid();
        var isAdmin = user?.Role == UserRole.Admin;

        if (isAdmin)
            Constituencies = await _db.Constituencies.OrderBy(c => c.Name).ToListAsync();

        int? cId = isAdmin ? SelectedConstituencyId : user?.ConstituencyId;

        // Load wards for drill-down
        if (cId.HasValue)
            Wards = await _db.Wards.Where(w => w.ConstituencyId == cId.Value).OrderBy(w => w.WardNumber).ToListAsync();

        // Load booths for drill-down
        if (cId.HasValue)
        {
            var boothQuery = _db.Booths.Where(b => b.ConstituencyId == cId.Value);
            if (!string.IsNullOrEmpty(SelectedWard))
                boothQuery = boothQuery.Where(b => b.WardNumber == SelectedWard);
            Booths = await boothQuery.OrderBy(b => b.BoothNumber).ToListAsync();
        }

        IQueryable<Grievance> query = _db.Grievances
            .Include(g => g.Constituency)
            .OrderByDescending(g => g.ReportedAt);

        if (isAdmin)
        {
            if (SelectedConstituencyId.HasValue)
                query = query.Where(g => g.ConstituencyId == SelectedConstituencyId);
        }
        else if (user?.ConstituencyId.HasValue == true)
        {
            query = query.Where(g => g.ConstituencyId == user.ConstituencyId);
        }

        if (!string.IsNullOrEmpty(SelectedWard))
            query = query.Where(g => g.Ward == SelectedWard);
        if (SelectedBoothNumber.HasValue)
            query = query.Where(g => g.BoothNumber == SelectedBoothNumber.Value);
        if (!string.IsNullOrEmpty(PriorityFilter) && Enum.TryParse<GrievancePriority>(PriorityFilter, out var prio))
            query = query.Where(g => g.Priority == prio);

        Grievances = await query.ToListAsync();
        OpenCount = Grievances.Count(g => g.Status == GrievanceStatus.Open);
        InProgressCount = Grievances.Count(g => g.Status == GrievanceStatus.InProgress);
        ResolvedCount = Grievances.Count(g => g.Status == GrievanceStatus.Resolved);
        return Page();
    }
}

