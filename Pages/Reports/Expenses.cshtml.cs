using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Nirvachak_AI.Domain.Entities;
using Nirvachak_AI.Domain.Enums;
using Nirvachak_AI.Infrastructure.Data;

namespace Nirvachak_AI.Pages.Reports;

[Authorize(Roles = "Admin,SuperAdmin,CampaignManager,Candidate")]
public class ExpensesModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _userManager;

    public ExpensesModel(AppDbContext db, UserManager<AppUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    [BindProperty(SupportsGet = true)] public int?      SelectedConstituencyId { get; set; }
    [BindProperty(SupportsGet = true)] public DateTime? DateFrom               { get; set; }
    [BindProperty(SupportsGet = true)] public DateTime? DateTo                 { get; set; }

    public string           ConstituencyName { get; set; } = "";
    public List<Expense>    Expenses         { get; set; } = new();
    public decimal          TotalAmount      { get; set; }
    public decimal          ECBudgetLimit    { get; set; } = 4_000_000m;
    public int              ECBudgetPercent  { get; set; }
    public Dictionary<string, decimal> CategoryTotals { get; set; } = new();
    public List<Constituency> Constituencies { get; set; } = new();
    public bool             IsAdmin          { get; set; }
    public DateTime         GeneratedAt      { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user?.Role == UserRole.FieldWorker || user?.Role == UserRole.BoothAgent)
            return Forbid();

        IsAdmin = user?.Role == UserRole.SuperAdmin;
        GeneratedAt = DateTime.Now;

        if (IsAdmin)
            Constituencies = await _db.Constituencies.OrderBy(c => c.Name).ToListAsync();

        IQueryable<Expense> query = _db.Expenses
            .Include(e => e.Constituency)
            .OrderByDescending(e => e.ExpenseDate);

        if (IsAdmin)
        {
            if (SelectedConstituencyId.HasValue)
            {
                query = query.Where(e => e.ConstituencyId == SelectedConstituencyId);
                ConstituencyName = (await _db.Constituencies.FindAsync(SelectedConstituencyId.Value))?.Name ?? "";
            }
        }
        else if (user?.ConstituencyId.HasValue == true)
        {
            query = query.Where(e => e.ConstituencyId == user.ConstituencyId);
            ConstituencyName = (await _db.Constituencies.FindAsync(user.ConstituencyId.Value))?.Name ?? "";
        }

        if (DateFrom.HasValue) query = query.Where(e => e.ExpenseDate >= DateFrom.Value);
        if (DateTo.HasValue)   query = query.Where(e => e.ExpenseDate <= DateTo.Value);

        Expenses       = await query.ToListAsync();
        TotalAmount    = Expenses.Sum(e => e.Amount);
        ECBudgetPercent = ECBudgetLimit > 0
            ? (int)Math.Min(100, Math.Round((double)TotalAmount / (double)ECBudgetLimit * 100)) : 0;
        CategoryTotals = Expenses
            .GroupBy(e => e.Category.ToString())
            .ToDictionary(g => g.Key, g => g.Sum(e => e.Amount));

        return Page();
    }
}
