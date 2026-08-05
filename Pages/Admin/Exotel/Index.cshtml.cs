using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Nirvachak_AI.Domain.Entities;
using Nirvachak_AI.Domain.Enums;
using Nirvachak_AI.Infrastructure.Data;

namespace Nirvachak_AI.Pages.Admin.Exotel;

[Authorize(Roles = "Admin,SuperAdmin")]
public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _userManager;

    public IndexModel(AppDbContext db, UserManager<AppUser> userManager)
    {
        _db          = db;
        _userManager = userManager;
    }

    public ExotelConfig? Config { get; set; }
    public List<SelectListItem> ConstituencyOptions { get; set; } = new();

    [BindProperty]
    public ExotelConfigInput Input { get; set; } = new();

    [TempData] public string? SuccessMessage { get; set; }
    [TempData] public string? ErrorMessage   { get; set; }

    public async Task OnGetAsync()
    {
        await LoadAsync();
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        var isSuperAdmin = user?.Role == UserRole.SuperAdmin;

        // Determine which constituency this config is for
        int? targetConstituencyId = isSuperAdmin
            ? (Input.ConstituencyId > 0 ? Input.ConstituencyId : (int?)null)
            : user?.ConstituencyId;

        var existing = await _db.ExotelConfigs
            .FirstOrDefaultAsync(e => e.ConstituencyId == targetConstituencyId);

        if (existing == null)
        {
            existing = new ExotelConfig { ConstituencyId = targetConstituencyId };
            _db.ExotelConfigs.Add(existing);
        }

        existing.ApiKey      = Input.ApiKey.Trim();
        existing.ApiToken    = Input.ApiToken.Trim();
        existing.AccountSid  = Input.AccountSid.Trim();
        existing.Subdomain   = string.IsNullOrWhiteSpace(Input.Subdomain) ? "api.exotel.com" : Input.Subdomain.Trim();
        existing.ExoPhone    = Input.ExoPhone.Trim();
        existing.SmsSenderId = string.IsNullOrWhiteSpace(Input.SmsSenderId) ? null : Input.SmsSenderId.Trim();
        existing.IsEnabled   = Input.IsEnabled;
        existing.UpdatedAt   = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        SuccessMessage = "Exotel configuration saved successfully.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        var isSuperAdmin = user?.Role == UserRole.SuperAdmin;

        int? targetConstituencyId = isSuperAdmin
            ? (Input.ConstituencyId > 0 ? Input.ConstituencyId : (int?)null)
            : user?.ConstituencyId;

        var existing = await _db.ExotelConfigs
            .FirstOrDefaultAsync(e => e.ConstituencyId == targetConstituencyId);

        if (existing != null)
        {
            _db.ExotelConfigs.Remove(existing);
            await _db.SaveChangesAsync();
            SuccessMessage = "Exotel configuration removed.";
        }

        return RedirectToPage();
    }

    private async Task LoadAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        var isSuperAdmin = user?.Role == UserRole.SuperAdmin;

        if (isSuperAdmin)
        {
            ConstituencyOptions = await _db.Constituencies
                .OrderBy(c => c.Name)
                .Select(c => new SelectListItem(c.Name, c.Id.ToString()))
                .ToListAsync();
            ConstituencyOptions.Insert(0, new SelectListItem("Global (all constituencies)", "0"));
        }

        int? constituencyId = isSuperAdmin
            ? (Input.ConstituencyId > 0 ? Input.ConstituencyId : (int?)null)
            : user?.ConstituencyId;

        Config = await _db.ExotelConfigs
            .FirstOrDefaultAsync(e => e.ConstituencyId == constituencyId);

        if (Config != null)
        {
            Input.ApiKey       = Config.ApiKey;
            Input.ApiToken     = Config.ApiToken;
            Input.AccountSid   = Config.AccountSid;
            Input.Subdomain    = Config.Subdomain;
            Input.ExoPhone     = Config.ExoPhone;
            Input.SmsSenderId  = Config.SmsSenderId ?? string.Empty;
            Input.IsEnabled    = Config.IsEnabled;
            Input.ConstituencyId = Config.ConstituencyId ?? 0;
        }
    }
}

public class ExotelConfigInput
{
    public string ApiKey      { get; set; } = string.Empty;
    public string ApiToken    { get; set; } = string.Empty;
    public string AccountSid  { get; set; } = string.Empty;
    public string Subdomain   { get; set; } = "api.exotel.com";
    public string ExoPhone    { get; set; } = string.Empty;
    public string SmsSenderId { get; set; } = string.Empty;
    public bool   IsEnabled   { get; set; } = true;
    public int    ConstituencyId { get; set; }
}
