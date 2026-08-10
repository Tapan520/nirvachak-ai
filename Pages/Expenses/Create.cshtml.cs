using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Nirvachak_AI.Domain.Entities;
using Nirvachak_AI.Domain.Enums;
using Nirvachak_AI.Infrastructure.Data;

namespace Nirvachak_AI.Pages.Expenses;

public class CreateModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _userManager;

    public CreateModel(AppDbContext db, UserManager<AppUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    [BindProperty]
    public Expense Expense { get; set; } = new() { ExpenseDate = DateTime.Today, IsECCompliant = true };

    [BindProperty]
    public int? SelectedConstituencyId { get; set; }

    [BindProperty]
    public IFormFile? ReceiptPhoto { get; set; }

    public SelectList? ConstituencyList { get; set; }
    public bool IsAdmin { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user?.Role == UserRole.FieldWorker || user?.Role == UserRole.BoothAgent)
            return Forbid();
        IsAdmin = user?.Role == UserRole.SuperAdmin;
        if (IsAdmin)
        {
            var constituencies = await _db.Constituencies.OrderBy(c => c.Name).ToListAsync();
            ConstituencyList = new SelectList(constituencies, "Id", "Name");
        }
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user?.Role == UserRole.FieldWorker || user?.Role == UserRole.BoothAgent)
            return Forbid();
        var isAdmin = user?.Role == UserRole.SuperAdmin;
        IsAdmin = isAdmin;
        if (IsAdmin)
        {
            var constituencies = await _db.Constituencies.OrderBy(c => c.Name).ToListAsync();
            ConstituencyList = new SelectList(constituencies, "Id", "Name");
        }
        if (!ModelState.IsValid) return Page();

        if (isAdmin && SelectedConstituencyId.HasValue)
            Expense.ConstituencyId = SelectedConstituencyId.Value;
        else
            Expense.ConstituencyId = user?.ConstituencyId ?? 1;

        Expense.ApprovedByUserId = user?.Id;
        Expense.ApprovedByName = user?.FullName;
        Expense.CreatedAt = DateTime.UtcNow;

        // Handle receipt photo upload
        if (ReceiptPhoto != null && ReceiptPhoto.Length > 0)
        {
            var allowedTypes = new[] { "image/jpeg", "image/png", "image/webp", "application/pdf" };
            if (!allowedTypes.Contains(ReceiptPhoto.ContentType.ToLower()))
            {
                ModelState.AddModelError("ReceiptPhoto", "Only JPG, PNG, WebP, or PDF files are allowed.");
                return Page();
            }
            if (ReceiptPhoto.Length > 5 * 1024 * 1024)
            {
                ModelState.AddModelError("ReceiptPhoto", "File size must be under 5 MB.");
                return Page();
            }
            var uploadsDir = Path.Combine("wwwroot", "uploads", "receipts");
            Directory.CreateDirectory(uploadsDir);
            var ext      = Path.GetExtension(ReceiptPhoto.FileName);
            var fileName = $"receipt_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}{ext}";
            var filePath = Path.Combine(uploadsDir, fileName);
            using var stream = System.IO.File.Create(filePath);
            await ReceiptPhoto.CopyToAsync(stream);
            Expense.ReceiptPhotoPath = $"/uploads/receipts/{fileName}";
        }

        _db.Expenses.Add(Expense);
        await _db.SaveChangesAsync();
        TempData["Message"] = "Expense recorded.";
        return RedirectToPage("/Expenses/Index");
    }
}

