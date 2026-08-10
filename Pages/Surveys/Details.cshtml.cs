using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Nirvachak_AI.Domain.Entities;
using Nirvachak_AI.Domain.Enums;
using Nirvachak_AI.Infrastructure.Data;
using SurveyEntity = Nirvachak_AI.Domain.Entities.Survey;

namespace Nirvachak_AI.Pages.Surveys;

[Microsoft.AspNetCore.Authorization.Authorize]
public class DetailsModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _userManager;

    public DetailsModel(AppDbContext db, UserManager<AppUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    private static readonly UserRole[] ManageRoles = [UserRole.SuperAdmin, UserRole.Admin, UserRole.CampaignManager, UserRole.Candidate];

    public SurveyEntity Survey { get; set; } = null!;
    public Dictionary<int, int> RatingBreakdown { get; set; } = new();
    public double AvgRating { get; set; }
    public bool CanManage { get; set; }

    [BindProperty]
    public SurveyResponse NewResponse { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var survey = await _db.Surveys
            .Include(s => s.Constituency)
            .Include(s => s.Responses.OrderByDescending(r => r.RespondedAt))
            .FirstOrDefaultAsync(s => s.Id == id);

        if (survey == null) return NotFound();
        Survey = survey;
        var user = await _userManager.GetUserAsync(User);
        CanManage = user != null && ManageRoles.Contains(user.Role);
        BuildStats();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        var survey = await _db.Surveys
            .Include(s => s.Constituency)
            .Include(s => s.Responses.OrderByDescending(r => r.RespondedAt))
            .FirstOrDefaultAsync(s => s.Id == id);

        if (survey == null) return NotFound();
        Survey = survey;
        var user = await _userManager.GetUserAsync(User);
        CanManage = user != null && ManageRoles.Contains(user.Role);

        if (NewResponse.Rating < 1 || NewResponse.Rating > 5)
            ModelState.AddModelError("NewResponse.Rating", "Rating must be between 1 and 5.");

        if (!ModelState.IsValid)
        {
            BuildStats();
            return Page();
        }

        NewResponse.SurveyId = id;
        NewResponse.RespondedAt = DateTime.UtcNow;
        _db.SurveyResponses.Add(NewResponse);
        await _db.SaveChangesAsync();
        TempData["Message"] = "Response recorded successfully.";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostDeleteResponseAsync(int id, int responseId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null || !ManageRoles.Contains(user.Role)) return Forbid();
        var response = await _db.SurveyResponses.Include(r => r.Survey).FirstOrDefaultAsync(r => r.Id == responseId);
        if (response != null)
        {
            if (user.Role != UserRole.SuperAdmin && response.Survey?.ConstituencyId != user.ConstituencyId)
                return Forbid();
            _db.SurveyResponses.Remove(response);
            await _db.SaveChangesAsync();
            TempData["Message"] = "Response deleted.";
        }
        return RedirectToPage(new { id });
    }

    private void BuildStats()
    {
        for (int i = 1; i <= 5; i++)
            RatingBreakdown[i] = Survey.Responses.Count(r => r.Rating == i);
        AvgRating = Survey.Responses.Any()
            ? Math.Round(Survey.Responses.Average(r => r.Rating), 1)
            : 0;
    }
}
