using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nirvachak_AI.Domain.Entities;
using Nirvachak_AI.Domain.Enums;
using Nirvachak_AI.Infrastructure.Data;
using Nirvachak_AI.Infrastructure.Services;

namespace Nirvachak_AI.Pages.Voters;

public class ImportModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly VoterImportService _importService;
    private readonly UserManager<AppUser> _userManager;

    public ImportModel(AppDbContext db, VoterImportService importService, UserManager<AppUser> userManager)
    {
        _db = db;
        _importService = importService;
        _userManager = userManager;
    }

    [BindProperty]
    public int SelectedConstituencyId { get; set; }
    public List<SelectListItem> ConstituencyItems { get; set; } = new();
    public ImportResult? Result { get; set; }


    private async Task<bool> IsRestrictedRoleAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        return user?.Role == UserRole.FieldWorker || user?.Role == UserRole.BoothAgent;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        if (await IsRestrictedRoleAsync()) return Forbid();
        await LoadConstituenciesAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(IFormFile? csvFile)
    {
        if (await IsRestrictedRoleAsync()) return Forbid();
        await LoadConstituenciesAsync();
        if (csvFile == null || csvFile.Length == 0)
        {
            ModelState.AddModelError("", "Please select a CSV file.");
            return Page();
        }
        using var stream = csvFile.OpenReadStream();
        Result = await _importService.ImportFromCsvAsync(stream, SelectedConstituencyId);
        return Page();
    }

    private async Task LoadConstituenciesAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        var query = _db.Constituencies.AsQueryable();
        if (user?.Role != UserRole.SuperAdmin && user?.ConstituencyId.HasValue == true)
            query = query.Where(c => c.Id == user.ConstituencyId);
        ConstituencyItems = query
            .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = $"{c.Name} ({c.Code})" })
            .ToList();
    }
}
