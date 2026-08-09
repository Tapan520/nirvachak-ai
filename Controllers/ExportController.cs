using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nirvachak_AI.Domain.Entities;
using Nirvachak_AI.Domain.Enums;
using Nirvachak_AI.Infrastructure.Data;

namespace Nirvachak_AI.Controllers;

/// <summary>
/// Provides Excel / CSV export endpoints for voters, expenses and election-day reports.
/// </summary>
[Authorize]
[Route("api/export")]
public class ExportController : ControllerBase
{
    private readonly AppDbContext _db;

    public ExportController(AppDbContext db) => _db = db;

    // ?? Voters ???????????????????????????????????????????????????

    /// <summary>Export voter list for a constituency to Excel.</summary>
    [HttpGet("voters/{constituencyId:int}")]
    public async Task<IActionResult> ExportVoters(int constituencyId,
        string? ward = null, int? booth = null, string? sentiment = null)
    {
        var query = _db.Voters
            .Where(v => v.ConstituencyId == constituencyId && !v.IsDeleted);

        if (!string.IsNullOrEmpty(ward))
            query = query.Where(v => v.WardNumber == ward);
        if (booth.HasValue)
            query = query.Where(v => v.BoothNumber == booth.Value);
        if (!string.IsNullOrEmpty(sentiment) && Enum.TryParse<VoterSentiment>(sentiment, true, out var s))
            query = query.Where(v => v.Sentiment == s);

        var voters = await query
            .OrderBy(v => v.BoothNumber).ThenBy(v => v.SerialNumber)
            .ToListAsync();

        var constituency = await _db.Constituencies.FindAsync(constituencyId);

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Voters");

        // Header row
        var headers = new[]
        {
            "Sr#", "Voter ID", "Name", "Local Name", "Father/Husband",
            "Age", "Gender", "Mobile", "Address", "Booth#", "Ward",
            "Panna#", "Serial#", "Sentiment", "Last Contacted"
        };
        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cell(1, i + 1).Value = headers[i];
            ws.Cell(1, i + 1).Style.Font.Bold = true;
            ws.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#1a73e8");
            ws.Cell(1, i + 1).Style.Font.FontColor = XLColor.White;
        }

        // Data rows
        int row = 2;
        foreach (var v in voters)
        {
            ws.Cell(row, 1).Value  = row - 1;
            ws.Cell(row, 2).Value  = v.VoterId;
            ws.Cell(row, 3).Value  = v.Name;
            ws.Cell(row, 4).Value  = v.NameLocal ?? "";
            ws.Cell(row, 5).Value  = v.FatherHusbandName ?? "";
            ws.Cell(row, 6).Value  = v.Age;
            ws.Cell(row, 7).Value  = v.Gender;
            ws.Cell(row, 8).Value  = v.MobileNumber ?? "";
            ws.Cell(row, 9).Value  = v.Address;
            ws.Cell(row, 10).Value = v.BoothNumber;
            ws.Cell(row, 11).Value = v.WardNumber ?? "";
            ws.Cell(row, 12).Value = v.PannaNumber ?? "";
            ws.Cell(row, 13).Value = v.SerialNumber;
            ws.Cell(row, 14).Value = v.Sentiment.ToString();
            ws.Cell(row, 15).Value = v.LastContactedAt?.ToLocalTime().ToString("dd-MMM-yyyy HH:mm") ?? "Never";
            row++;
        }

        ws.Columns().AdjustToContents();

        var ms = new MemoryStream();
        wb.SaveAs(ms);
        ms.Position = 0;

        var fileName = $"Voters_{constituency?.Name ?? constituencyId.ToString()}_{DateTime.Now:yyyyMMdd}.xlsx";
        return File(ms, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    // ?? Expenses ?????????????????????????????????????????????????

    /// <summary>Export expense report to Excel (EC-compliance ready).</summary>
    [HttpGet("expenses/{constituencyId:int}")]
    public async Task<IActionResult> ExportExpenses(int constituencyId,
        DateTime? from = null, DateTime? to = null)
    {
        var query = _db.Expenses.Where(e => e.ConstituencyId == constituencyId);
        if (from.HasValue) query = query.Where(e => e.ExpenseDate >= from.Value);
        if (to.HasValue)   query = query.Where(e => e.ExpenseDate <= to.Value);

        var expenses = await query.OrderBy(e => e.ExpenseDate).ToListAsync();
        var constituency = await _db.Constituencies.FindAsync(constituencyId);
        var total = expenses.Sum(e => e.Amount);

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Expenses");

        // Title
        ws.Cell(1, 1).Value = $"EC Expense Report — {constituency?.Name}";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 14;
        ws.Range(1, 1, 1, 9).Merge();

        ws.Cell(2, 1).Value = $"Generated: {DateTime.Now:dd-MMM-yyyy HH:mm}";
        ws.Range(2, 1, 2, 9).Merge();

        // Headers
        var headers = new[]
        {
            "Date", "Category", "Description", "Amount (?)",
            "Payee", "Voucher#", "EC Compliant", "Approved By", "Notes"
        };
        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cell(4, i + 1).Value = headers[i];
            ws.Cell(4, i + 1).Style.Font.Bold = true;
            ws.Cell(4, i + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#1a73e8");
            ws.Cell(4, i + 1).Style.Font.FontColor = XLColor.White;
        }

        int row = 5;
        foreach (var e in expenses)
        {
            ws.Cell(row, 1).Value = e.ExpenseDate.ToString("dd-MMM-yyyy");
            ws.Cell(row, 2).Value = e.Category.ToString();
            ws.Cell(row, 3).Value = e.Description;
            ws.Cell(row, 4).Value = (double)e.Amount;
            ws.Cell(row, 4).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row, 5).Value = e.PayeeName ?? "";
            ws.Cell(row, 6).Value = e.VoucherNumber ?? "";
            ws.Cell(row, 7).Value = e.IsECCompliant ? "Yes" : "No";
            if (!e.IsECCompliant)
                ws.Cell(row, 7).Style.Font.FontColor = XLColor.Red;
            ws.Cell(row, 8).Value = e.ApprovedByName ?? "";
            ws.Cell(row, 9).Value = e.Notes ?? "";
            row++;
        }

        // Total row
        ws.Cell(row, 3).Value = "TOTAL";
        ws.Cell(row, 3).Style.Font.Bold = true;
        ws.Cell(row, 4).Value = (double)total;
        ws.Cell(row, 4).Style.Font.Bold = true;
        ws.Cell(row, 4).Style.NumberFormat.Format = "#,##0.00";

        ws.Columns().AdjustToContents();

        var ms = new MemoryStream();
        wb.SaveAs(ms);
        ms.Position = 0;

        var fileName = $"Expenses_{constituency?.Name ?? constituencyId.ToString()}_{DateTime.Now:yyyyMMdd}.xlsx";
        return File(ms, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    // ?? Election Day Summary ?????????????????????????????????????

    /// <summary>Export booth-wise election day turnout to Excel.</summary>
    [HttpGet("electionday/{constituencyId:int}")]
    public async Task<IActionResult> ExportElectionDay(int constituencyId)
    {
        var booths = await _db.Booths
            .Where(b => b.ConstituencyId == constituencyId)
            .OrderBy(b => b.BoothNumber)
            .ToListAsync();

        var constituency = await _db.Constituencies.FindAsync(constituencyId);

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Election Day");

        ws.Cell(1, 1).Value = $"Election Day Report — {constituency?.Name}";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 14;
        ws.Range(1, 1, 1, 7).Merge();

        var headers = new[]
        { "Booth#", "Booth Name", "Total Voters", "Voted", "Turnout %", "Agent", "Ward" };

        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cell(3, i + 1).Value = headers[i];
            ws.Cell(3, i + 1).Style.Font.Bold = true;
            ws.Cell(3, i + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#1a73e8");
            ws.Cell(3, i + 1).Style.Font.FontColor = XLColor.White;
        }

        int row = 4;
        foreach (var b in booths)
        {
            var pct = b.TotalVoters > 0
                ? Math.Round((double)b.VotedCount / b.TotalVoters * 100, 1)
                : 0;

            ws.Cell(row, 1).Value = b.BoothNumber;
            ws.Cell(row, 2).Value = b.BoothName;
            ws.Cell(row, 3).Value = b.TotalVoters;
            ws.Cell(row, 4).Value = b.VotedCount;
            ws.Cell(row, 5).Value = pct;
            ws.Cell(row, 5).Style.NumberFormat.Format = "0.0\"%\"";
            ws.Cell(row, 6).Value = b.AssignedAgentName ?? "Unassigned";
            ws.Cell(row, 7).Value = b.WardNumber ?? "";

            // Colour-code turnout
            var fillColor = pct >= 75 ? XLColor.FromHtml("#d4edda")
                          : pct >= 50 ? XLColor.FromHtml("#fff3cd")
                          : XLColor.FromHtml("#f8d7da");
            ws.Row(row).Style.Fill.BackgroundColor = fillColor;
            row++;
        }

        ws.Columns().AdjustToContents();

        var ms = new MemoryStream();
        wb.SaveAs(ms);
        ms.Position = 0;

        var fileName = $"ElectionDay_{constituency?.Name ?? constituencyId.ToString()}_{DateTime.Now:yyyyMMdd}.xlsx";
        return File(ms, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    // ?? Volunteers ???????????????????????????????????????????????

    /// <summary>Export volunteer list to Excel.</summary>
    [HttpGet("volunteers/{constituencyId:int}")]
    public async Task<IActionResult> ExportVolunteers(int constituencyId)
    {
        var volunteers = await _db.Volunteers
            .Where(v => v.ConstituencyId == constituencyId)
            .OrderBy(v => v.Name)
            .ToListAsync();

        var constituency = await _db.Constituencies.FindAsync(constituencyId);

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Volunteers");

        var headers = new[]
        { "Name", "Phone", "Email", "Task", "Area", "Booths", "Active", "Notes" };

        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cell(1, i + 1).Value = headers[i];
            ws.Cell(1, i + 1).Style.Font.Bold = true;
            ws.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#1a73e8");
            ws.Cell(1, i + 1).Style.Font.FontColor = XLColor.White;
        }

        int row = 2;
        foreach (var v in volunteers)
        {
            ws.Cell(row, 1).Value = v.Name;
            ws.Cell(row, 2).Value = v.Phone;
            ws.Cell(row, 3).Value = v.Email ?? "";
            ws.Cell(row, 4).Value = v.Task.ToString();
            ws.Cell(row, 5).Value = v.AssignedArea ?? "";
            ws.Cell(row, 6).Value = v.AssignedBoothNumbers ?? "";
            ws.Cell(row, 7).Value = v.IsActive ? "Yes" : "No";
            ws.Cell(row, 8).Value = v.Notes ?? "";
            row++;
        }

        ws.Columns().AdjustToContents();

        var ms = new MemoryStream();
        wb.SaveAs(ms);
        ms.Position = 0;

        var fileName = $"Volunteers_{constituency?.Name ?? constituencyId.ToString()}_{DateTime.Now:yyyyMMdd}.xlsx";
        return File(ms, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }
}
