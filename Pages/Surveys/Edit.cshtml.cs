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
public class EditModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _userManager;

    public EditModel(AppDbContext db, UserManager<AppUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    [BindProperty]
    public SurveyEntity Survey { get; set; } = null!;

    [BindProperty]
    public int? SelectedConstituencyId { get; set; }

    public List<Constituency> Constituencies { get; set; } = new();
    public bool IsAdmin { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        IsAdmin = user?.Role == UserRole.SuperAdmin;

        var survey = await _db.Surveys.FindAsync(id);
        if (survey == null) return NotFound();

        if (user?.Role != UserRole.SuperAdmin && survey.ConstituencyId != user?.ConstituencyId)
            return Forbid();

        Survey = survey;
        SelectedConstituencyId = survey.ConstituencyId;

        if (IsAdmin)
            Constituencies = await _db.Constituencies.OrderBy(c => c.Name).ToListAsync();

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        var user = await _userManager.GetUserAsync(User);
        IsAdmin = user?.Role == UserRole.SuperAdmin;

        var existing = await _db.Surveys.FindAsync(Survey.Id);
        if (existing == null) return NotFound();

        if (user?.Role != UserRole.SuperAdmin && existing.ConstituencyId != user?.ConstituencyId)
            return Forbid();

        existing.Title       = Survey.Title;
        existing.Description = Survey.Description;
        existing.Category    = Survey.Category;
        existing.IsActive    = Survey.IsActive;

        if (IsAdmin && SelectedConstituencyId.HasValue)
            existing.ConstituencyId = SelectedConstituencyId.Value;

        await _db.SaveChangesAsync();
        TempData["Message"] = "Survey updated successfully.";
        return RedirectToPage("/Surveys/Index");
    }
}
