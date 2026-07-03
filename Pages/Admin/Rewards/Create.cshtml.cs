using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Nirvachak_AI.Domain.Entities;
using Nirvachak_AI.Domain.Enums;
using Nirvachak_AI.Infrastructure.Data;

namespace Nirvachak_AI.Pages.Admin.Rewards;

[Authorize(Roles = "Admin,CampaignManager,SuperAdmin")]
public class CreateModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _userManager;

    public CreateModel(AppDbContext db, UserManager<AppUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    [BindProperty, Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [BindProperty]
    public string? Description { get; set; }

    [BindProperty]
    public string? PartnerBrand { get; set; }

    [BindProperty, Required, MaxLength(8)]
    public string CouponCodePrefix { get; set; } = "NIRV";

    [BindProperty, Required]
    [DataType(DataType.Date)]
    public DateTime ExpiryDate { get; set; } = DateTime.Now.AddMonths(3);

    [BindProperty, Range(1, 50000)]
    public int CouponCount { get; set; } = 200;

    [BindProperty]
    public int? SelectedConstituencyId { get; set; }

    public List<Constituency> Constituencies { get; set; } = new();
    public bool IsAdmin { get; set; }

    public async Task OnGetAsync()
    {
        IsAdmin = User.IsInRole(nameof(UserRole.SuperAdmin));
        if (IsAdmin)
            Constituencies = await _db.Constituencies.OrderBy(c => c.Name).ToListAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var currentUser = await _userManager.GetUserAsync(User);
        bool isSuperAdmin = User.IsInRole(nameof(UserRole.SuperAdmin));
        IsAdmin = isSuperAdmin;
        if (IsAdmin)
            Constituencies = await _db.Constituencies.OrderBy(c => c.Name).ToListAsync();

        if (!ModelState.IsValid) return Page();

        // Resolve constituency: SuperAdmin picks from dropdown, everyone else uses their own
        int cId;
        if (isSuperAdmin && SelectedConstituencyId.HasValue)
            cId = SelectedConstituencyId.Value;
        else
            cId = currentUser?.ConstituencyId ?? (await _db.Constituencies.Select(c => c.Id).FirstAsync());

        var prefix = CouponCodePrefix.ToUpper().Trim();
        var config = new RewardConfig
        {
            Title            = Title,
            Description      = Description,
            PartnerBrand     = PartnerBrand,
            CouponCodePrefix = prefix,
            ExpiryDate       = ExpiryDate.ToUniversalTime(),
            IsActive         = true,
            ConstituencyId   = cId
        };

        // Bulk-generate unique coupon codes
        var rnd   = new Random();
        var codes = new HashSet<string>();
        while (codes.Count < CouponCount)
            codes.Add($"{prefix}{rnd.Next(100000, 999999)}");

        config.Coupons = codes.Select(code => new CouponPool { CouponCode = code }).ToList();

        _db.RewardConfigs.Add(config);
        await _db.SaveChangesAsync();

        TempData["Message"] = $"Reward '{config.Title}' created with {CouponCount} coupons.";
        return RedirectToPage("/Admin/Rewards/Index");
    }
}
