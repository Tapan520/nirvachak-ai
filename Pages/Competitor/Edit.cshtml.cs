using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Nirvachak_AI.Domain.Entities;
using Nirvachak_AI.Infrastructure.Data;

namespace Nirvachak_AI.Pages.Competitor;

[Authorize(Roles = "Admin,CampaignManager,Candidate")]
public class EditModel : PageModel
{
    private readonly AppDbContext _db;
    public EditModel(AppDbContext db) => _db = db;

    [BindProperty]
    public CompetitorActivity Activity { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var a = await _db.CompetitorActivities.FindAsync(id);
        if (a == null) return NotFound();
        Activity = a;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();
        _db.CompetitorActivities.Update(Activity);
        await _db.SaveChangesAsync();
        return RedirectToPage("./Index");
    }
}
