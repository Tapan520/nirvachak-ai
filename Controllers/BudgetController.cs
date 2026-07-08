using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nirvachak_AI.Domain.Enums;
using Nirvachak_AI.Infrastructure.Data;
using Nirvachak_AI.Models.Api;

namespace Nirvachak_AI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class BudgetController : ApiBaseController
{
    private readonly AppDbContext _db;
    public BudgetController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetBudget()
    {
        var cId = GetConstituencyId();
        var isSA = GetUserRole() == nameof(UserRole.SuperAdmin);

        var plans = await _db.BudgetPlans
            .Where(b => !isSA ? (cId.HasValue && b.ConstituencyId == cId.Value) : true)
            .AsNoTracking()
            .ToListAsync();

        var spent = await _db.Expenses
            .Where(e => !isSA ? (cId.HasValue && e.ConstituencyId == cId.Value) : true)
            .GroupBy(e => e.Category)
            .Select(g => new { Category = g.Key, Total = g.Sum(e => e.Amount) })
            .ToDictionaryAsync(g => g.Category.ToString(), g => g.Total);

        var totalSpent = spent.Values.Sum();
        var items = plans.Select(p => {
            var spentAmt = spent.TryGetValue(p.Category.ToString(), out var s) ? s : 0;
            var utilPct  = p.PlannedAmount > 0 ? (double)(spentAmt / p.PlannedAmount) * 100 : 0;
            return new BudgetItem(p.Id, p.Category.ToString(),
                p.PlannedAmount, spentAmt, p.PlannedAmount - spentAmt,
                Math.Round(utilPct, 1), p.Notes);
        }).ToList();

        return Ok(items);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateBudgetItemRequest req)
    {
        var cId = GetConstituencyId() ?? 1;
        if (!Enum.TryParse<ExpenseCategory>(req.Category, out var cat))
            return BadRequest("Invalid category.");

        var existing = await _db.BudgetPlans
            .FirstOrDefaultAsync(b => b.ConstituencyId == cId && b.Category == cat);
        if (existing != null)
        {
            existing.PlannedAmount = req.PlannedAmount;
            existing.Notes = req.Notes;
        }
        else
        {
            _db.BudgetPlans.Add(new Domain.Entities.BudgetPlan
            {
                ConstituencyId = cId, Category = cat,
                PlannedAmount  = req.PlannedAmount, Notes = req.Notes,
            });
        }
        await _db.SaveChangesAsync();
        return Ok(new ApiResult(true, "Budget saved."));
    }
}
