using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Nirvachak_AI.Domain.Entities;
using Nirvachak_AI.Domain.Enums;
using Nirvachak_AI.Infrastructure.Data;
using Nirvachak_AI.Infrastructure.Services;

namespace Nirvachak_AI.Pages.Predictions;

[Authorize(Roles = "Admin,SuperAdmin,CampaignManager,Candidate")]
public class IndexModel : PageModel
{
    private readonly PredictiveAnalyticsService _svc;
    private readonly UserManager<AppUser>       _userManager;
    private readonly AppDbContext               _db;

    public IndexModel(
        PredictiveAnalyticsService svc,
        UserManager<AppUser>       userManager,
        AppDbContext               db)
    {
        _svc         = svc;
        _userManager = userManager;
        _db          = db;
    }

    // ?? Filter ?????????????????????????????????????????????????????
    [BindProperty(SupportsGet = true)]
    public int? SelectedConstituencyId { get; set; }

    // ?? Output ?????????????????????????????????????????????????????
    public PredictionSummary        Summary          { get; set; } = new();
    public List<Constituency>       Constituencies   { get; set; } = new();
    public Constituency?            SelectedConstituency { get; set; }
    public bool                     IsAdmin          { get; set; }
    public string                   GeneratedAt      { get; set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToPage("/Account/Login");

        IsAdmin = user.Role == UserRole.Admin || user.Role == UserRole.SuperAdmin;

        if (IsAdmin)
            Constituencies = await _db.Constituencies.OrderBy(c => c.Name).ToListAsync();

        int? cId = IsAdmin ? (SelectedConstituencyId ?? user.ConstituencyId) : user.ConstituencyId;

        if (!cId.HasValue)
            return Page(); // show empty state

        SelectedConstituency = await _db.Constituencies.FindAsync(cId.Value);
        Summary              = await _svc.GetPredictionsAsync(cId.Value);
        GeneratedAt          = DateTime.Now.ToString("dd MMM yyyy, h:mm tt");

        return Page();
    }
}
