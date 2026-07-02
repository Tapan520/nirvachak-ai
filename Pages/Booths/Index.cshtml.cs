using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nirvachak_AI.Domain.Entities;
using Nirvachak_AI.Domain.Enums;
using Nirvachak_AI.Infrastructure.Data;

namespace Nirvachak_AI.Pages.Booths;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _userManager;

    public IndexModel(AppDbContext db, UserManager<AppUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public List<Booth> Booths { get; set; } = new();
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

        Booths = await query.ToListAsync();
    }
}
