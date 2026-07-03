using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Nirvachak_AI.Domain.Entities;
using Nirvachak_AI.Domain.Enums;
using Nirvachak_AI.Infrastructure.Data;
using SurveyEntity = Nirvachak_AI.Domain.Entities.Survey;

namespace Nirvachak_AI.Pages.Surveys;

[Authorize(Roles = "Admin,SuperAdmin,CampaignManager,Candidate")]
public class CreateModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _userManager;

    public CreateModel(AppDbContext db, UserManager<AppUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    [BindProperty]
    public SurveyEntity Survey { get; set; } = new();

    [BindProperty]
    public int? SelectedConstituencyId { get; set; }

    public List<Constituency> Constituencies { get; set; } = new();
    public bool IsAdmin { get; set; }

    public async Task OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        IsAdmin = user?.Role == UserRole.SuperAdmin;
        if (IsAdmin)
            Constituencies = await _db.Constituencies.OrderBy(c => c.Name).ToListAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();
        var user = await _userManager.GetUserAsync(User);
        var isSuperAdmin = user?.Role == UserRole.SuperAdmin;

        Survey.ConstituencyId = isSuperAdmin && SelectedConstituencyId.HasValue
            ? SelectedConstituencyId.Value
            : user?.ConstituencyId ?? 1;

        Survey.CreatedAt = DateTime.UtcNow;
        Survey.IsActive = true;

        _db.Surveys.Add(Survey);
        await _db.SaveChangesAsync();
        TempData["Message"] = "Survey created successfully.";
        return RedirectToPage("/Surveys/Index");
    }
}
