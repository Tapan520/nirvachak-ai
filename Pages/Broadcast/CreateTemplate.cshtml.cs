using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Nirvachak_AI.Domain.Entities;
using Nirvachak_AI.Domain.Enums;
using Nirvachak_AI.Infrastructure.Data;

namespace Nirvachak_AI.Pages.Broadcast;

[Authorize(Roles = "Admin,SuperAdmin,CampaignManager,Candidate")]
public class CreateTemplateModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _userManager;
    public CreateTemplateModel(AppDbContext db, UserManager<AppUser> userManager) { _db = db; _userManager = userManager; }

    [BindProperty] public MessageTemplate Template { get; set; } = new();

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();
        var user = await _userManager.GetUserAsync(User);
        Template.ConstituencyId = user?.ConstituencyId ?? Template.ConstituencyId;
        Template.CreatedByUserId = user?.Id ?? string.Empty;
        Template.CreatedAt = DateTime.UtcNow;
        _db.MessageTemplates.Add(Template);
        await _db.SaveChangesAsync();
        TempData["Message"] = "Template saved.";
        return RedirectToPage("/Broadcast/Index");
    }
}
