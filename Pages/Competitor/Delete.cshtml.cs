using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Nirvachak_AI.Domain.Entities;
using Nirvachak_AI.Infrastructure.Data;

namespace Nirvachak_AI.Pages.Competitor;

[Authorize(Roles = "Admin,CampaignManager")]
public class DeleteModel : PageModel
{
    private readonly AppDbContext _db;
    public DeleteModel(AppDbContext db) => _db = db;

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
        var a = await _db.CompetitorActivities.FindAsync(Activity.Id);
        if (a != null)
        {
            _db.CompetitorActivities.Remove(a);
            await _db.SaveChangesAsync();
        }
        return RedirectToPage("./Index");
    }
}
