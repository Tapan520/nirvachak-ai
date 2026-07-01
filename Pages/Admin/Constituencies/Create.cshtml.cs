using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Nirvachak_AI.Domain.Entities;
using Nirvachak_AI.Domain.Enums;
using Nirvachak_AI.Infrastructure.Data;

namespace Nirvachak_AI.Pages.Admin.Constituencies;

[Authorize(Roles = "SuperAdmin")]
public class CreateModel : PageModel
{
    private readonly AppDbContext _db;
    public CreateModel(AppDbContext db) => _db = db;

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required, StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required, StringLength(20)]
        public string Code { get; set; } = string.Empty;

        [Required]
        public ElectionType ElectionType { get; set; }

        [Required, StringLength(50)]
        public string State { get; set; } = string.Empty;

        [Required, StringLength(50)]
        public string District { get; set; } = string.Empty;

        public string? CandidateName { get; set; }
        public string? PartyName { get; set; }
        public string? PartySymbol { get; set; }

        [Required]
        public DateTime ElectionDate { get; set; } = DateTime.Today.AddMonths(6);
    }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        var constituency = new Constituency
        {
            Name = Input.Name.Trim(),
            Code = Input.Code.Trim().ToUpper(),
            ElectionType = Input.ElectionType,
            State = Input.State.Trim(),
            District = Input.District.Trim(),
            CandidateName = Input.CandidateName?.Trim(),
            PartyName = Input.PartyName?.Trim(),
            PartySymbol = Input.PartySymbol?.Trim(),
            ElectionDate = Input.ElectionDate,
            CreatedAt = DateTime.UtcNow
        };

        _db.Constituencies.Add(constituency);
        await _db.SaveChangesAsync();
        TempData["Message"] = $"Constituency '{constituency.Name}' created successfully.";
        return RedirectToPage("/Admin/Constituencies/Index");
    }
}
