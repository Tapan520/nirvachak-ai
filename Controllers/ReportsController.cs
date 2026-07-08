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
public class ReportsController : ApiBaseController
{
    private readonly AppDbContext _db;
    public ReportsController(AppDbContext db) => _db = db;

    [HttpGet("expenses")]
    public async Task<IActionResult> ExpenseReport(
        [FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var cId  = GetConstituencyId();
        var isSA = GetUserRole() == nameof(UserRole.SuperAdmin);
        const decimal ecLimit = 4_000_000m;

        var q = _db.Expenses.AsNoTracking().AsQueryable();
        if (!isSA && cId.HasValue) q = q.Where(e => e.ConstituencyId == cId.Value);
        else if (!isSA) return Ok(new ExpenseReportResponse(0, ecLimit, 0, new(), new()));
        if (from.HasValue) q = q.Where(e => e.ExpenseDate >= from.Value);
        if (to.HasValue)   q = q.Where(e => e.ExpenseDate <= to.Value);

        var expenses = await q.OrderByDescending(e => e.ExpenseDate).ToListAsync();
        var total    = expenses.Sum(e => e.Amount);
        var pct      = ecLimit > 0 ? (int)Math.Min(100, Math.Round((double)(total / ecLimit) * 100)) : 0;

        var catTotals = expenses
            .GroupBy(e => e.Category.ToString())
            .Select(g => new CategoryTotal(g.Key, g.Sum(e => e.Amount),
                total > 0 ? Math.Round((double)(g.Sum(e => e.Amount) / total) * 100, 1) : 0))
            .OrderByDescending(c => c.Amount)
            .ToList();

        var items = expenses.Select(e => new ExpenseListItem(
            e.Id, e.Description, e.Category.ToString(), e.Amount,
            e.ExpenseDate, e.PayeeName, e.VoucherNumber,
            e.IsECCompliant, e.ApprovedByName)).ToList();

        return Ok(new ExpenseReportResponse(total, ecLimit, pct, catTotals, items));
    }
}
