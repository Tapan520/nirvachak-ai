using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Nirvachak_AI.Domain.Entities;
using Nirvachak_AI.Domain.Enums;
using Nirvachak_AI.Infrastructure.Data;

namespace Nirvachak_AI.Pages.Voters;

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
    public Voter? Voter { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        Voter = await _db.Voters.FindAsync(id);
        if (Voter == null) return NotFound();
        if (user?.Role != UserRole.SuperAdmin && Voter.ConstituencyId != user?.ConstituencyId)
            return Forbid();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (!ModelState.IsValid) return Page();
        var existing = await _db.Voters.FindAsync(Voter!.Id);
        if (existing == null) return NotFound();
        if (user?.Role != UserRole.SuperAdmin && existing.ConstituencyId != user?.ConstituencyId)
            return Forbid();

        existing.Name = Voter.Name;
        existing.NameLocal = Voter.NameLocal;
        existing.FatherHusbandName = Voter.FatherHusbandName;
        existing.Age = Voter.Age;
        existing.Gender = Voter.Gender;
        existing.MobileNumber = Voter.MobileNumber;
        existing.Address = Voter.Address;
        existing.BoothNumber = Voter.BoothNumber;
        existing.WardNumber = Voter.WardNumber;
        existing.PannaNumber = Voter.PannaNumber;
        existing.SerialNumber = Voter.SerialNumber;
        existing.Sentiment = Voter.Sentiment;
        existing.Notes = Voter.Notes;
        existing.HouseholdId = string.IsNullOrWhiteSpace(Voter.HouseholdId) ? null : Voter.HouseholdId.Trim();

        await _db.SaveChangesAsync();
        TempData["Message"] = "Voter updated successfully.";
        return RedirectToPage("/Voters/Details", new { id = Voter.Id });
    }
}
