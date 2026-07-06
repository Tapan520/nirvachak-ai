using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Nirvachak_AI.Domain.Entities;
using Nirvachak_AI.Infrastructure.Data;

namespace Nirvachak_AI.Pages.Broadcast;

[Authorize(Roles = "Admin,SuperAdmin,CampaignManager,Candidate")]
public class EditTemplateModel : PageModel
{
    private readonly AppDbContext _db;
    public EditTemplateModel(AppDbContext db) => _db = db;

    [BindProperty] public MessageTemplate Template { get; set; } = null!;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var t = await _db.MessageTemplates.FindAsync(id);
        if (t == null) return NotFound();
        Template = t;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();
        var existing = await _db.MessageTemplates.FindAsync(Template.Id);
        if (existing == null) return NotFound();
        existing.Title = Template.Title; existing.Body = Template.Body;
        existing.Language = Template.Language; existing.Category = Template.Category;
        await _db.SaveChangesAsync();
        TempData["Message"] = "Template updated.";
        return RedirectToPage("/Broadcast/Index");
    }
}
