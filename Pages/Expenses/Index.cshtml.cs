using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Nirvachak_AI.Domain.Entities;
using Nirvachak_AI.Domain.Enums;
using Nirvachak_AI.Infrastructure.Data;

namespace Nirvachak_AI.Pages.Expenses;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _userManager;

    public IndexModel(AppDbContext db, UserManager<AppUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    [BindProperty(SupportsGet = true)]
    public int? SelectedConstituencyId { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? DateFrom { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? DateTo { get; set; }

    public List<Constituency> Constituencies { get; set; } = new();
    public List<Expense> Expenses { get; set; } = new();
    public decimal TotalAmount { get; set; }
    public decimal ECBudgetLimit { get; set; } = 4_000_000m;
    public int     ECBudgetPercent { get; set; }
    public Dictionary<string, decimal> CategoryTotals { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user?.Role == UserRole.FieldWorker || user?.Role == UserRole.BoothAgent)
            return Forbid();
        var isAdmin = user?.Role == UserRole.SuperAdmin;

        if (isAdmin)
            Constituencies = await _db.Constituencies.OrderBy(c => c.Name).ToListAsync();

        IQueryable<Expense> query = _db.Expenses
            .Include(e => e.Constituency)
            .OrderByDescending(e => e.ExpenseDate);

        if (isAdmin)
        {
            if (SelectedConstituencyId.HasValue)
                query = query.Where(e => e.ConstituencyId == SelectedConstituencyId);
        }
        else if (user?.ConstituencyId.HasValue == true)
        {
            query = query.Where(e => e.ConstituencyId == user.ConstituencyId);
        }

        if (DateFrom.HasValue)
            query = query.Where(e => e.ExpenseDate >= DateFrom.Value);
        if (DateTo.HasValue)
            query = query.Where(e => e.ExpenseDate <= DateTo.Value);

        Expenses = await query.ToListAsync();
        TotalAmount = Expenses.Sum(e => e.Amount);
        ECBudgetPercent = ECBudgetLimit > 0
            ? (int)Math.Min(100, Math.Round((double)TotalAmount / (double)ECBudgetLimit * 100)) : 0;
        CategoryTotals = Expenses
            .GroupBy(e => e.Category.ToString())
            .ToDictionary(g => g.Key, g => g.Sum(e => e.Amount));
        return Page();
    }

    public async Task<IActionResult> OnGetExportCsvAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user?.Role == UserRole.FieldWorker || user?.Role == UserRole.BoothAgent)
            return Forbid();
        var isAdmin = user?.Role == UserRole.SuperAdmin;

        IQueryable<Expense> query = _db.Expenses
            .Include(e => e.Constituency)
            .OrderByDescending(e => e.ExpenseDate);

        if (isAdmin)
        {
            if (SelectedConstituencyId.HasValue)
                query = query.Where(e => e.ConstituencyId == SelectedConstituencyId);
        }
        else if (user?.ConstituencyId.HasValue == true)
            query = query.Where(e => e.ConstituencyId == user.ConstituencyId);

        if (DateFrom.HasValue) query = query.Where(e => e.ExpenseDate >= DateFrom.Value);
        if (DateTo.HasValue)   query = query.Where(e => e.ExpenseDate <= DateTo.Value);

        var rows = await query.ToListAsync();

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Date,Description,Category,Constituency,Payee,Amount,EC Compliant,Voucher");
        foreach (var e in rows)
        {
            sb.AppendLine(string.Join(",",
                e.ExpenseDate.ToString("yyyy-MM-dd"),
                $"\"{e.Description?.Replace("\"", "\"\"")}\"",
                e.Category.ToString(),
                $"\"{e.Constituency?.Name ?? ""}\"",
                $"\"{e.PayeeName?.Replace("\"", "\"\"") ?? ""}\"",
                e.Amount.ToString("F2"),
                e.IsECCompliant ? "Yes" : "No",
                e.VoucherNumber ?? ""));
        }

        var bytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
        return File(bytes, "text/csv", $"expenses_{DateTime.Today:yyyyMMdd}.csv");
    }
}

