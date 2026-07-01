using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Nirvachak_AI.Domain.Entities;
using Nirvachak_AI.Domain.Enums;
using Nirvachak_AI.Infrastructure.Data;

namespace Nirvachak_AI.Pages.Admin.Constituencies;

[Authorize(Roles = "SuperAdmin")]
public class EditModel : PageModel
{
    private readonly AppDbContext _db;
    public EditModel(AppDbContext db) => _db = db;

    [BindProperty]
    public InputModel Input { get; set; } = new();

    [BindProperty]
    public int ConstituencyId { get; set; }

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
        public DateTime ElectionDate { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var c = await _db.Constituencies.FindAsync(id);
        if (c == null) return NotFound();
        ConstituencyId = id;
        Input = new InputModel
        {
            Name = c.Name,
            Code = c.Code,
            ElectionType = c.ElectionType,
            State = c.State,
            District = c.District,
            CandidateName = c.CandidateName,
            PartyName = c.PartyName,
            PartySymbol = c.PartySymbol,
            ElectionDate = c.ElectionDate
        };
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        var c = await _db.Constituencies.FindAsync(ConstituencyId);
        if (c == null) return NotFound();

        c.Name = Input.Name.Trim();
        c.Code = Input.Code.Trim().ToUpper();
        c.ElectionType = Input.ElectionType;
        c.State = Input.State.Trim();
        c.District = Input.District.Trim();
        c.CandidateName = Input.CandidateName?.Trim();
        c.PartyName = Input.PartyName?.Trim();
        c.PartySymbol = Input.PartySymbol?.Trim();
        c.ElectionDate = Input.ElectionDate;

        await _db.SaveChangesAsync();
        TempData["Message"] = $"Constituency '{c.Name}' updated successfully.";
        return RedirectToPage("/Admin/Constituencies/Index");
    }
}
