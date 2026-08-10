using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Nirvachak_AI.Domain.Entities;
using Nirvachak_AI.Domain.Enums;
using Nirvachak_AI.Infrastructure.Data;
using Nirvachak_AI.Infrastructure.Services;

namespace Nirvachak_AI.Pages.VoterSlips;

[Microsoft.AspNetCore.Authorization.Authorize]
public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _userManager;
    private readonly VoterSlipService _slipService;
    private readonly Dictionary<int, string> _qrCache = new();

    public IndexModel(AppDbContext db, UserManager<AppUser> userManager, VoterSlipService slipService)
    {
        _db = db;
        _userManager = userManager;
        _slipService = slipService;
    }

    public List<Voter> Voters { get; set; } = new();
    public List<int> BoothNumbers { get; set; } = new();
    public List<Constituency> Constituencies { get; set; } = new();
    public bool IsAdmin { get; set; }

    [Microsoft.AspNetCore.Mvc.BindProperty(SupportsGet = true)]
    public int? ConstituencyFilter { get; set; }

    [Microsoft.AspNetCore.Mvc.BindProperty(SupportsGet = true)]
    public int? BoothFilter { get; set; }

    public async Task OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        IsAdmin = user?.Role == UserRole.SuperAdmin;

        if (IsAdmin)
            Constituencies = await _db.Constituencies.OrderBy(c => c.Name).ToListAsync();

        IQueryable<Voter> query = _db.Voters.Where(v => !v.IsDeleted);
        if (IsAdmin)
        {
            if (ConstituencyFilter.HasValue)
                query = query.Where(v => v.ConstituencyId == ConstituencyFilter);
        }
        else if (user?.ConstituencyId.HasValue == true)
            query = query.Where(v => v.ConstituencyId == user.ConstituencyId);

        if (BoothFilter.HasValue)
            query = query.Where(v => v.BoothNumber == BoothFilter);

        Voters = await query
            .OrderBy(v => v.BoothNumber).ThenBy(v => v.SerialNumber)
            .Take(200)
            .ToListAsync();

        IQueryable<Voter> allQuery = _db.Voters.Where(v => !v.IsDeleted);
        if (IsAdmin)
        {
            if (ConstituencyFilter.HasValue)
                allQuery = allQuery.Where(v => v.ConstituencyId == ConstituencyFilter);
        }
        else if (user?.ConstituencyId.HasValue == true)
            allQuery = allQuery.Where(v => v.ConstituencyId == user.ConstituencyId);
        BoothNumbers = await allQuery.Select(v => v.BoothNumber).Distinct().OrderBy(n => n).ToListAsync();

        foreach (var v in Voters)
            _qrCache[v.Id] = _slipService.GenerateQrCodeBase64(v);
    }

    public string GetQrCode(int voterId) => _qrCache.TryGetValue(voterId, out var qr) ? qr : "";
}
