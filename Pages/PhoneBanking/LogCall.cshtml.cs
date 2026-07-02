using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Nirvachak_AI.Domain.Entities;
using Nirvachak_AI.Domain.Enums;
using Nirvachak_AI.Infrastructure.Data;

namespace Nirvachak_AI.Pages.PhoneBanking;

[Authorize(Roles = "Admin,SuperAdmin,CampaignManager,Candidate,FieldWorker,BoothAgent")]
public class LogCallModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _userManager;
    public LogCallModel(AppDbContext db, UserManager<AppUser> userManager) { _db = db; _userManager = userManager; }

    [BindProperty]
    public PhoneCallLog Call { get; set; } = new();

    public Voter? SelectedVoter { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? VoterId { get; set; }

    // For voter search
    [BindProperty(SupportsGet = true)]
    public string? SearchName { get; set; }
    public List<Voter> SearchResults { get; set; } = new();

    public async Task OnGetAsync()
    {
        Call.CalledAt = DateTime.Now;
        if (VoterId.HasValue)
            SelectedVoter = await _db.Voters.FindAsync(VoterId.Value);

        if (!string.IsNullOrWhiteSpace(SearchName))
        {
            var user = await _userManager.GetUserAsync(User);
            int? cId = user?.ConstituencyId;
            var isSuperAdmin = user?.Role == UserRole.SuperAdmin;
            SearchResults = await _db.Voters
                .Where(v => !v.IsDeleted && v.MobileNumber != null &&
                    (v.Name.Contains(SearchName) || (v.MobileNumber != null && v.MobileNumber.Contains(SearchName))))
                .Where(v => isSuperAdmin || !cId.HasValue || v.ConstituencyId == cId.Value)
                .Take(10).ToListAsync();
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();
        var user = await _userManager.GetUserAsync(User);
        Call.CalledByUserId = user?.Id ?? string.Empty;
        Call.CalledByName   = user?.FullName;
        Call.ConstituencyId = user?.ConstituencyId ?? Call.ConstituencyId;
        Call.CalledAt       = DateTime.UtcNow;
        _db.PhoneCallLogs.Add(Call);

        // Update voter's last contacted date and sentiment if changed
        var voter = await _db.Voters.FindAsync(Call.VoterId);
        if (voter != null)
        {
            voter.LastContactedAt = DateTime.UtcNow;
            if (Call.SentimentAfterCall.HasValue)
                voter.Sentiment = Call.SentimentAfterCall.Value;
        }

        await _db.SaveChangesAsync();
        return RedirectToPage("./Index");
    }
}
