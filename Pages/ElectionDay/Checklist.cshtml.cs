using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Nirvachak_AI.Domain.Entities;
using Nirvachak_AI.Domain.Enums;
using Nirvachak_AI.Infrastructure.Data;

namespace Nirvachak_AI.Pages.ElectionDay;

public class ChecklistModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _userManager;

    public ChecklistModel(AppDbContext db, UserManager<AppUser> userManager)
    {
        _db          = db;
        _userManager = userManager;
    }

    public List<Booth> Booths { get; set; } = new();
    public Dictionary<int, BoothChecklist> Checklists { get; set; } = new();
    public int ConstituencyId { get; set; }
    public int ReadyCount  { get; set; }
    public int TotalBooths { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? ConstituencyFilter { get; set; }
    public List<Constituency> Constituencies { get; set; } = new();
    public bool IsAdmin { get; set; }

    public async Task OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        IsAdmin = user?.Role == UserRole.SuperAdmin;

        if (IsAdmin)
            Constituencies = await _db.Constituencies.OrderBy(c => c.Name).ToListAsync();

        ConstituencyId = IsAdmin
            ? (ConstituencyFilter ?? Constituencies.FirstOrDefault()?.Id ?? 0)
            : (user?.ConstituencyId ?? 0);

        if (ConstituencyId == 0) return;

        Booths = await _db.Booths
            .Where(b => b.ConstituencyId == ConstituencyId)
            .OrderBy(b => b.BoothNumber).ToListAsync();

        var lists = await _db.BoothChecklists
            .Where(c => c.ConstituencyId == ConstituencyId)
            .ToListAsync();
        Checklists = lists.ToDictionary(c => c.BoothNumber);

        TotalBooths = Booths.Count;
        ReadyCount  = Checklists.Values.Count(c =>
            c.AgentPresent && c.BannerDisplayed && c.VoterListPrinted &&
            c.TransportArranged && c.PhoneCharged && c.BoothClean);
    }

    public async Task<IActionResult> OnPostSaveAsync(
        int boothNumber, bool agentPresent, bool bannerDisplayed,
        bool voterListPrinted, bool transportArranged, bool phoneCharged,
        bool boothClean, string? notes)
    {
        var user = await _userManager.GetUserAsync(User);
        var cId  = user?.Role == UserRole.SuperAdmin
            ? (ConstituencyFilter ?? user.ConstituencyId ?? 0)
            : (user?.ConstituencyId ?? 0);

        if (cId == 0) return RedirectToPage();

        var existing = await _db.BoothChecklists
            .FirstOrDefaultAsync(c => c.ConstituencyId == cId && c.BoothNumber == boothNumber);

        if (existing == null)
        {
            _db.BoothChecklists.Add(new BoothChecklist
            {
                BoothNumber       = boothNumber,
                ConstituencyId    = cId,
                AgentPresent      = agentPresent,
                BannerDisplayed   = bannerDisplayed,
                VoterListPrinted  = voterListPrinted,
                TransportArranged = transportArranged,
                PhoneCharged      = phoneCharged,
                BoothClean        = boothClean,
                Notes             = notes,
                SubmittedByUserId = user?.Id,
                SubmittedByName   = user?.FullName,
                SubmittedAt       = DateTime.UtcNow
            });
        }
        else
        {
            existing.AgentPresent      = agentPresent;
            existing.BannerDisplayed   = bannerDisplayed;
            existing.VoterListPrinted  = voterListPrinted;
            existing.TransportArranged = transportArranged;
            existing.PhoneCharged      = phoneCharged;
            existing.BoothClean        = boothClean;
            existing.Notes             = notes;
            existing.SubmittedByUserId = user?.Id;
            existing.SubmittedByName   = user?.FullName;
            existing.UpdatedAt         = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        TempData["Message"] = $"Booth {boothNumber} checklist saved successfully.";
        return RedirectToPage(new { ConstituencyFilter = cId });
    }
}
