using Nirvachak_AI.Domain.Enums;

namespace Nirvachak_AI.Domain.Entities;

public class Expense
{
    public int Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public ExpenseCategory Category { get; set; }
    public decimal Amount { get; set; }
    public DateTime ExpenseDate { get; set; }
    public string? VoucherNumber { get; set; }
    public string? PayeeName { get; set; }
    public string? ApprovedByUserId { get; set; }
    public string? ApprovedByName { get; set; }
    public string? Notes { get; set; }
    public bool IsECCompliant { get; set; } = true;
    public string? ReceiptPhotoPath { get; set; }
    public int ConstituencyId { get; set; }
    public Constituency? Constituency { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
