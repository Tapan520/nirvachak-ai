using Nirvachak_AI.Domain.Enums;

namespace Nirvachak_AI.Domain.Entities;

public class BudgetPlan
{
    public int Id { get; set; }
    public int ConstituencyId { get; set; }
    public Constituency? Constituency { get; set; }
    public ExpenseCategory Category { get; set; }
    public decimal PlannedAmount { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
