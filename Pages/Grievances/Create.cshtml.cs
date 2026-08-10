using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Nirvachak_AI.Domain.Entities;
using Nirvachak_AI.Domain.Enums;
using Nirvachak_AI.Infrastructure.Data;

namespace Nirvachak_AI.Pages.Grievances;

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
    public Grievance Grievance { get; set; } = new();

    [BindProperty]
    public int? SelectedConstituencyId { get; set; }

    public SelectList? ConstituencyList { get; set; }
    public bool IsAdmin { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user?.Role == UserRole.FieldWorker || user?.Role == UserRole.BoothAgent)
            return Forbid();
        IsAdmin = user?.Role == UserRole.SuperAdmin;
        if (IsAdmin)
        {
            var constituencies = await _db.Constituencies.OrderBy(c => c.Name).ToListAsync();
            ConstituencyList = new SelectList(constituencies, "Id", "Name");
        }
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user?.Role == UserRole.FieldWorker || user?.Role == UserRole.BoothAgent)
            return Forbid();
        var isAdmin = user?.Role == UserRole.SuperAdmin;
        IsAdmin = isAdmin;
        if (IsAdmin)
        {
            var constituencies = await _db.Constituencies.OrderBy(c => c.Name).ToListAsync();
            ConstituencyList = new SelectList(constituencies, "Id", "Name");
        }
        if (!ModelState.IsValid) return Page();

        if (isAdmin && SelectedConstituencyId.HasValue)
            Grievance.ConstituencyId = SelectedConstituencyId.Value;
        else
            Grievance.ConstituencyId = user?.ConstituencyId ?? 1;

        Grievance.ReportedAt = DateTime.UtcNow;
        _db.Grievances.Add(Grievance);
        await _db.SaveChangesAsync();
        TempData["Message"] = "Grievance submitted successfully.";
        return RedirectToPage("/Grievances/Index");
    }
}

