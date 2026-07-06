using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Nirvachak_AI.Domain.Entities;
using Nirvachak_AI.Domain.Enums;
using Nirvachak_AI.Infrastructure.Data;
using Nirvachak_AI.Infrastructure.Services;

namespace Nirvachak_AI.Pages.Voters;

[Authorize(Roles = "Admin,SuperAdmin,CampaignManager,Candidate,FieldWorker,BoothAgent")]
public class CreateModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _userManager;
    private readonly AuditService _audit;

    public CreateModel(AppDbContext db, UserManager<AppUser> userManager, AuditService audit)
    {
        _db = db;
        _userManager = userManager;
        _audit = audit;
    }

    [BindProperty]
    public Voter Voter { get; set; } = new();

    [BindProperty]
    public int? SelectedConstituencyId { get; set; }

    public List<Constituency> Constituencies { get; set; } = new();
    public List<Booth> Booths { get; set; } = new();
    public List<Ward> Wards { get; set; } = new();
    public bool IsAdmin { get; set; }
    public bool IsRestrictedRole { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Forbid();

        IsAdmin = user.Role == UserRole.SuperAdmin;
        IsRestrictedRole = user.Role == UserRole.FieldWorker || user.Role == UserRole.BoothAgent;
        if (IsAdmin)
            Constituencies = await _db.Constituencies.OrderBy(c => c.Name).ToListAsync();

        await LoadDropdownsAsync(IsAdmin ? null : user.ConstituencyId, user);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Forbid();

        IsAdmin = user.Role == UserRole.SuperAdmin;
        IsRestrictedRole = user.Role == UserRole.FieldWorker || user.Role == UserRole.BoothAgent;
        if (IsAdmin)
            Constituencies = await _db.Constituencies.OrderBy(c => c.Name).ToListAsync();

        int? cId = IsAdmin ? SelectedConstituencyId : user.ConstituencyId;
        await LoadDropdownsAsync(cId, user);

        // Manual validation for required string fields
        if (string.IsNullOrWhiteSpace(Voter.VoterId))
            ModelState.AddModelError("Voter.VoterId", "EPIC / Voter ID is required.");
        if (string.IsNullOrWhiteSpace(Voter.Name))
            ModelState.AddModelError("Voter.Name", "Full Name is required.");
        if (string.IsNullOrWhiteSpace(Voter.Address))
            ModelState.AddModelError("Voter.Address", "Address is required.");
        if (string.IsNullOrWhiteSpace(Voter.Gender))
            ModelState.AddModelError("Voter.Gender", "Gender is required.");
        if (Voter.Age < 18 || Voter.Age > 120)
            ModelState.AddModelError("Voter.Age", "Age must be between 18 and 120.");
        if (Voter.BoothNumber <= 0)
            ModelState.AddModelError("Voter.BoothNumber", "Please select a valid booth.");
        if (IsAdmin && !cId.HasValue)
            ModelState.AddModelError("SelectedConstituencyId", "Constituency is required.");

        // Restricted roles can only add voters to their assigned booths
        if (IsRestrictedRole)
        {
            var assignedBooths = ParseAssignedBooths(user.AssignedBoothNumbers);
            if (assignedBooths.Any() && !assignedBooths.Contains(Voter.BoothNumber))
                ModelState.AddModelError("Voter.BoothNumber", "You can only add voters to your assigned booths.");
        }

        if (!ModelState.IsValid) return Page();

        // Check for duplicate EPIC number
        if (await _db.Voters.AnyAsync(v => v.VoterId == Voter.VoterId.Trim() && !v.IsDeleted))
        {
            ModelState.AddModelError("Voter.VoterId", "A voter with this EPIC number already exists.");
            return Page();
        }

        Voter.VoterId = Voter.VoterId.Trim().ToUpper();
        Voter.ConstituencyId = cId ?? 1;
        Voter.ImportedAt = DateTime.UtcNow;
        Voter.IsDeleted = false;
        Voter.ElectionDayStatus = ElectionDayStatus.NotVoted;

        _db.Voters.Add(Voter);
        _audit.Track(user.Id, user.FullName,
            "AddVoter", "Voter", null,
            $"Manually added voter '{Voter.Name}' (EPIC: {Voter.VoterId}), Booth {Voter.BoothNumber}",
            Voter.ConstituencyId);
        await _db.SaveChangesAsync();

        TempData["Message"] = $"Voter '{Voter.Name}' added successfully.";
        return RedirectToPage("/Voters/Details", new { id = Voter.Id });
    }

    private async Task LoadDropdownsAsync(int? cId, AppUser user)
    {
        if (!cId.HasValue) return;

        var boothQuery = _db.Booths.Where(b => b.ConstituencyId == cId.Value);

        // Restricted roles only see their assigned booths
        if (user.Role == UserRole.FieldWorker || user.Role == UserRole.BoothAgent)
        {
            var assignedBooths = ParseAssignedBooths(user.AssignedBoothNumbers);
            if (assignedBooths.Any())
                boothQuery = boothQuery.Where(b => assignedBooths.Contains(b.BoothNumber));
        }

        Booths = await boothQuery.OrderBy(b => b.BoothNumber).ToListAsync();
        Wards = await _db.Wards
            .Where(w => w.ConstituencyId == cId.Value)
            .OrderBy(w => w.WardNumber).ToListAsync();
    }

    private static List<int> ParseAssignedBooths(string? assignedBoothNumbers) =>
        (assignedBoothNumbers ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => int.TryParse(s.Trim(), out var n) ? (int?)n : null)
            .Where(n => n.HasValue).Select(n => n!.Value).ToList();
}
