using System.Text;
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
    public bool IsSuperAdmin { get; set; }
    public List<FailedVoterRow> FailedRows { get; set; } = new();


    private async Task<bool> IsRestrictedRoleAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        return user?.Role == UserRole.FieldWorker || user?.Role == UserRole.BoothAgent;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        if (await IsRestrictedRoleAsync()) return Forbid();
        await LoadConstituenciesAsync();
        // Restore failed rows from session (stored after last import)
        var json = HttpContext.Session.GetString("FailedRows");
        if (!string.IsNullOrEmpty(json))
            FailedRows = System.Text.Json.JsonSerializer.Deserialize<List<FailedVoterRow>>(json) ?? new();
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
        FailedRows = Result.FailedRows;
        // Store failed rows in Session (server-side — no cookie size limit)
        if (FailedRows.Any())
            HttpContext.Session.SetString("FailedRows", System.Text.Json.JsonSerializer.Serialize(FailedRows));
        else
            HttpContext.Session.Remove("FailedRows");
        return Page();
    }

    public async Task<IActionResult> OnGetDownloadFailedAsync()
    {
        if (await IsRestrictedRoleAsync()) return Forbid();
        List<FailedVoterRow> rows = new();
        var json = HttpContext.Session.GetString("FailedRows");
        if (!string.IsNullOrEmpty(json))
            rows = System.Text.Json.JsonSerializer.Deserialize<List<FailedVoterRow>>(json) ?? new();

        var sb = new StringBuilder();
        sb.AppendLine("VoterId,Name,NameLocal,FatherHusbandName,Age,Gender,MobileNumber,Address,BoothNumber,WardNumber,PannaNumber,SerialNumber,FailReason");
        foreach (var r in rows)
        {
            sb.AppendLine($"{Csv(r.VoterId)},{Csv(r.Name)},{Csv(r.NameLocal)},{Csv(r.FatherHusbandName)}," +
                          $"{r.Age},{Csv(r.Gender)},{Csv(r.MobileNumber)},{Csv(r.Address)}," +
                          $"{r.BoothNumber},{Csv(r.WardNumber)},{Csv(r.PannaNumber)},{r.SerialNumber},{Csv(r.FailReason)}");
        }
        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        return File(bytes, "text/csv", $"failed_voters_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
    }

    private static string Csv(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"\"")}\""; 
        return value;
    }

    private async Task LoadConstituenciesAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        IsSuperAdmin = user?.Role == UserRole.SuperAdmin;
        var query = _db.Constituencies.AsQueryable();
        if (!IsSuperAdmin && user?.ConstituencyId.HasValue == true)
        {
            query = query.Where(c => c.Id == user.ConstituencyId);
            SelectedConstituencyId = user.ConstituencyId.Value;
        }
        ConstituencyItems = query
            .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = $"{c.Name} ({c.Code})" })
            .ToList();
    }
}
