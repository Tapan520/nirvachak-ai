using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Nirvachak_AI.Domain.Entities;
using Nirvachak_AI.Domain.Enums;
using Nirvachak_AI.Infrastructure.Data;

namespace Nirvachak_AI.Pages.Booths;

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
    public Booth Booth { get; set; } = new();

    public List<Constituency> Constituencies { get; set; } = new();
    public bool IsAdmin { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user?.Role == UserRole.FieldWorker || user?.Role == UserRole.BoothAgent)
            return Forbid();
        IsAdmin = user?.Role == UserRole.SuperAdmin;
        if (IsAdmin)
            Constituencies = await _db.Constituencies.OrderBy(c => c.Name).ToListAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user?.Role == UserRole.FieldWorker || user?.Role == UserRole.BoothAgent)
            return Forbid();
        if (!ModelState.IsValid) return Page();
        if (user?.Role != UserRole.SuperAdmin)
            Booth.ConstituencyId = user?.ConstituencyId ?? 1;
        _db.Booths.Add(Booth);
        await _db.SaveChangesAsync();
        TempData["Message"] = "Booth added successfully.";
        return RedirectToPage("/Booths/Index");
    }
}
