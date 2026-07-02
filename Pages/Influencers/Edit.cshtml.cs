using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Nirvachak_AI.Domain.Entities;
using Nirvachak_AI.Infrastructure.Data;

namespace Nirvachak_AI.Pages.Influencers;

[Authorize(Roles = "Admin,SuperAdmin,CampaignManager,Candidate")]
public class EditModel : PageModel
{
    private readonly AppDbContext _db;
    public EditModel(AppDbContext db) => _db = db;

    [BindProperty]
    public Influencer Influencer { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var inf = await _db.Influencers.FindAsync(id);
        if (inf == null) return NotFound();
        Influencer = inf;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();
        _db.Influencers.Update(Influencer);
        await _db.SaveChangesAsync();
        return RedirectToPage("./Index");
    }
}
