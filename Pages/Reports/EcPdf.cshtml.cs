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
public class EcPdfModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _userManager;

    public EcPdfModel(AppDbContext db, UserManager<AppUser> userManager)
    {
        _db          = db;
        _userManager = userManager;
    }

    [BindProperty(SupportsGet = true)] public int?      SelectedConstituencyId { get; set; }
    [BindProperty(SupportsGet = true)] public DateTime? DateFrom               { get; set; }
    [BindProperty(SupportsGet = true)] public DateTime? DateTo                 { get; set; }

    public string           ConstituencyName { get; set; } = "";
    public string?          CandidateName    { get; set; }
    public string?          PartyName        { get; set; }
    public DateTime?        ElectionDate     { get; set; }
    public List<Expense>    Expenses         { get; set; } = new();
    public decimal          TotalAmount      { get; set; }
    public decimal          ECBudgetLimit    { get; set; } = 4_000_000m;
    public int              ECBudgetPercent  { get; set; }
    public Dictionary<string, decimal> CategoryTotals { get; set; } = new();
    public DateTime         GeneratedAt      { get; set; }
    public bool             IsAdmin          { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user?.Role == UserRole.FieldWorker || user?.Role == UserRole.BoothAgent)
            return Forbid();

        IsAdmin     = user?.Role == UserRole.SuperAdmin;
        GeneratedAt = DateTime.Now;

        IQueryable<Expense> query = _db.Expenses
            .Include(e => e.Constituency)
            .OrderBy(e => e.ExpenseDate);

        Constituency? constituency = null;
        if (IsAdmin && SelectedConstituencyId.HasValue)
        {
            query        = query.Where(e => e.ConstituencyId == SelectedConstituencyId);
            constituency = await _db.Constituencies.FindAsync(SelectedConstituencyId.Value);
        }
        else if (user?.ConstituencyId.HasValue == true)
        {
            query        = query.Where(e => e.ConstituencyId == user.ConstituencyId);
            constituency = await _db.Constituencies.FindAsync(user.ConstituencyId.Value);
        }

        if (constituency != null)
        {
            ConstituencyName = constituency.Name;
            CandidateName    = constituency.CandidateName;
            PartyName        = constituency.PartyName;
            ElectionDate     = constituency.ElectionDate;
        }

        if (DateFrom.HasValue) query = query.Where(e => e.ExpenseDate >= DateFrom.Value);
        if (DateTo.HasValue)   query = query.Where(e => e.ExpenseDate <= DateTo.Value);

        Expenses    = await query.ToListAsync();
        TotalAmount = Expenses.Sum(e => e.Amount);
        ECBudgetPercent = ECBudgetLimit > 0
            ? (int)Math.Min(100, Math.Round((double)TotalAmount / (double)ECBudgetLimit * 100)) : 0;
        CategoryTotals = Expenses
            .GroupBy(e => e.Category.ToString())
            .ToDictionary(g => g.Key, g => g.Sum(e => e.Amount));

        return Page();
    }
}
