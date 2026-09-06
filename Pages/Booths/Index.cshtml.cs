using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nirvachak_AI.Domain.Entities;
using Nirvachak_AI.Domain.Enums;
using Nirvachak_AI.Infrastructure.Data;

namespace Nirvachak_AI.Pages.Booths;

[Microsoft.AspNetCore.Authorization.Authorize]
public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _userManager;

    public IndexModel(AppDbContext db, UserManager<AppUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public class BoothCardVm
    {
        public Booth Booth { get; set; } = null!;
        public int TotalVoters { get; set; }
        public int MaleVoters { get; set; }
        public int FemaleVoters { get; set; }
        public int OtherVoters { get; set; }
        public int VotedCount { get; set; }
    }

    public List<BoothCardVm> Booths { get; set; } = new();
    public List<Constituency> Constituencies { get; set; } = new();
    public bool IsAdmin { get; set; }
    public bool CanManage { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? ConstituencyFilter { get; set; }

    public async Task OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        IsAdmin = user?.Role == UserRole.SuperAdmin;
        var isRestricted = user?.Role == UserRole.FieldWorker || user?.Role == UserRole.BoothAgent;
        CanManage = !isRestricted;

        if (IsAdmin)
            Constituencies = await _db.Constituencies.OrderBy(c => c.Name).ToListAsync();

        IQueryable<Booth> query = _db.Booths.OrderBy(b => b.BoothNumber);
        if (IsAdmin)
        {
            if (ConstituencyFilter.HasValue)
                query = query.Where(b => b.ConstituencyId == ConstituencyFilter);
        }
        else if (user?.ConstituencyId.HasValue == true)
            query = query.Where(b => b.ConstituencyId == user.ConstituencyId);

        // Restricted roles: only show their assigned booth(s)
        if (isRestricted)
        {
            var assignedBooths = (user?.AssignedBoothNumbers ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => int.TryParse(s.Trim(), out var n) ? (int?)n : null)
                .Where(n => n.HasValue).Select(n => n!.Value).ToList();
            if (assignedBooths.Any())
                query = query.Where(b => assignedBooths.Contains(b.BoothNumber));
        }

        var booths = await query.ToListAsync();

        // Live counts from Voters table (source of truth) — grouped per (ConstituencyId, BoothNumber)
        // so counts stay correct even if the stored Booth.TotalVoters/... are stale.
        var boothKeys = booths.Select(b => new { b.ConstituencyId, b.BoothNumber }).ToList();
        var constituencyIds = boothKeys.Select(k => k.ConstituencyId).Distinct().ToList();
        var boothNumbers    = boothKeys.Select(k => k.BoothNumber).Distinct().ToList();

        var stats = await _db.Voters
            .Where(v => !v.IsDeleted
                        && constituencyIds.Contains(v.ConstituencyId)
                        && boothNumbers.Contains(v.BoothNumber))
            .GroupBy(v => new { v.ConstituencyId, v.BoothNumber })
            .Select(g => new
            {
                g.Key.ConstituencyId,
                g.Key.BoothNumber,
                Total  = g.Count(),
                Male   = g.Count(v => v.Gender == "M"),
                Female = g.Count(v => v.Gender == "F"),
                Other  = g.Count(v => v.Gender != "M" && v.Gender != "F"),
                Voted  = g.Count(v => v.ElectionDayStatus == ElectionDayStatus.Voted)
            })
            .ToListAsync();

        var statsLookup = stats.ToDictionary(s => (s.ConstituencyId, s.BoothNumber));

        Booths = booths.Select(b =>
        {
            statsLookup.TryGetValue((b.ConstituencyId, b.BoothNumber), out var s);
            return new BoothCardVm
            {
                Booth        = b,
                TotalVoters  = s?.Total  ?? 0,
                MaleVoters   = s?.Male   ?? 0,
                FemaleVoters = s?.Female ?? 0,
                OtherVoters  = s?.Other  ?? 0,
                VotedCount   = s?.Voted  ?? 0
            };
        }).ToList();
    }
}
