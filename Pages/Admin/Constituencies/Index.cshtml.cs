using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Nirvachak_AI.Domain.Entities;
using Nirvachak_AI.Infrastructure.Data;

namespace Nirvachak_AI.Pages.Admin.Constituencies;

[Authorize(Roles = "SuperAdmin")]
public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    public IndexModel(AppDbContext db) => _db = db;

    public List<Constituency> Constituencies { get; set; } = new();
    public Dictionary<int, int> VoterCounts { get; set; } = new();
    public Dictionary<int, int> WardCounts { get; set; } = new();

    public async Task OnGetAsync()
    {
        Constituencies = await _db.Constituencies.OrderBy(c => c.Name).ToListAsync();
        VoterCounts = await _db.Voters.Where(v => !v.IsDeleted)
            .GroupBy(v => v.ConstituencyId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count);
        WardCounts = await _db.Wards
            .GroupBy(w => w.ConstituencyId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count);
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var constituency = await _db.Constituencies.FindAsync(id);
        if (constituency != null)
        {
            var hasUsers = await _db.Users.AnyAsync(u => u.ConstituencyId == id);
            var hasVoters = await _db.Voters.AnyAsync(v => v.ConstituencyId == id && !v.IsDeleted);
            if (hasUsers || hasVoters)
            {
                TempData["Error"] = "Cannot delete constituency with assigned users or voters.";
                return RedirectToPage();
            }
            _db.Constituencies.Remove(constituency);
            await _db.SaveChangesAsync();
            TempData["Message"] = $"Constituency '{constituency.Name}' deleted.";
        }
        return RedirectToPage();
    }
}
