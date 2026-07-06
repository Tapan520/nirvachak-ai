using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Nirvachak_AI.Domain.Entities;
using Nirvachak_AI.Domain.Enums;
using Nirvachak_AI.Infrastructure.Data;

namespace Nirvachak_AI.Pages.Budget;

[Authorize(Roles = "Admin,SuperAdmin,CampaignManager,Candidate")]
public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _userManager;
    public IndexModel(AppDbContext db, UserManager<AppUser> userManager) { _db = db; _userManager = userManager; }

    public List<BudgetPlanRow> Rows { get; set; } = new();
    public List<Constituency> Constituencies { get; set; } = new();
    public bool IsAdmin { get; set; }
    public decimal TotalPlanned { get; set; }
    public decimal TotalActual { get; set; }
    public decimal ECBudgetLimit { get; set; } = 4_000_000m;
    public int ECBudgetPercent { get; set; }

    [BindProperty(SupportsGet = true)] public int? ConstituencyFilter { get; set; }
    [BindProperty] public BudgetPlan NewPlan { get; set; } = new();

    public record BudgetPlanRow(ExpenseCategory Category, decimal Planned, decimal Actual, int UsedPercent);

    public async Task OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        IsAdmin = user?.Role == UserRole.SuperAdmin;
        if (IsAdmin) Constituencies = await _db.Constituencies.OrderBy(c => c.Name).ToListAsync();

        int? cId = IsAdmin ? ConstituencyFilter : user?.ConstituencyId;

        var plans = await _db.BudgetPlans.Where(b => !cId.HasValue || b.ConstituencyId == cId).ToListAsync();

        // Load expenses client-side before grouping to avoid SQLite GroupBy+Sum decimal translation issues
        var allExpenses = await _db.Expenses
            .Where(e => !cId.HasValue || e.ConstituencyId == cId)
            .Select(e => new { e.Category, e.Amount })
            .ToListAsync();
        var actualMap = allExpenses
            .GroupBy(e => e.Category)
            .ToDictionary(g => g.Key, g => g.Sum(e => e.Amount));

        foreach (ExpenseCategory cat in Enum.GetValues<ExpenseCategory>())
        {
            var planned = plans.FirstOrDefault(p => p.Category == cat)?.PlannedAmount ?? 0;
            var actual = actualMap.GetValueOrDefault(cat, 0);
            var pct = planned > 0 ? (int)Math.Min(100, Math.Round((double)actual / (double)planned * 100)) : (actual > 0 ? 100 : 0);
            Rows.Add(new BudgetPlanRow(cat, planned, actual, pct));
        }

        TotalPlanned = Rows.Sum(r => r.Planned);
        TotalActual = Rows.Sum(r => r.Actual);
        ECBudgetPercent = ECBudgetLimit > 0 ? (int)Math.Min(100, Math.Round((double)TotalActual / (double)ECBudgetLimit * 100)) : 0;
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        bool isAdminRole = user?.Role == UserRole.SuperAdmin;

        int cId;
        if (isAdminRole && ConstituencyFilter.HasValue)
            cId = ConstituencyFilter.Value;
        else if (user?.ConstituencyId.HasValue == true)
            cId = user.ConstituencyId.Value;
        else
            cId = 1;

        var existing = await _db.BudgetPlans.FirstOrDefaultAsync(b => b.ConstituencyId == cId && b.Category == NewPlan.Category);
        if (existing != null)
        {
            existing.PlannedAmount = NewPlan.PlannedAmount;
            existing.Notes = NewPlan.Notes;
        }
        else
        {
            NewPlan.ConstituencyId = cId;
            NewPlan.CreatedAt = DateTime.UtcNow;
            _db.BudgetPlans.Add(NewPlan);
        }
        await _db.SaveChangesAsync();
        TempData["Message"] = $"Budget for {NewPlan.Category} saved.";
        return RedirectToPage(new { ConstituencyFilter = isAdminRole ? cId : (int?)null });
    }
}
