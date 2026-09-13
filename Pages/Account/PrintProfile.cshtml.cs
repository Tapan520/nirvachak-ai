using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Nirvachak_AI.Domain.Entities;

namespace Nirvachak_AI.Pages.Account;

[Authorize]
public class PrintProfileModel : PageModel
{
    private readonly UserManager<AppUser> _userManager;
    private readonly IWebHostEnvironment _env;

    public PrintProfileModel(UserManager<AppUser> userManager, IWebHostEnvironment env)
    {
        _userManager = userManager;
        _env = env;
    }

    [BindProperty] public InputModel Input { get; set; } = new();
    public string? CandidatePhotoUrl { get; set; }
    public string FullName { get; set; } = string.Empty;

    public class InputModel
    {
        [MaxLength(100), Display(Name = "Candidate Name")]
        public string? CandidateName { get; set; }

        [MaxLength(100), Display(Name = "Party / Symbol")]
        public string? CandidatePartyOrSymbol { get; set; }

        [MaxLength(200), Display(Name = "Candidate Slogan")]
        public string? CandidateSlogan { get; set; }

        [Display(Name = "Candidate Photo")]
        public IFormFile? CandidatePhoto { get; set; }
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var u = await _userManager.GetUserAsync(User);
        if (u == null) return Challenge();
        FullName          = u.FullName;
        CandidatePhotoUrl = u.CandidatePhotoUrl;
        Input = new InputModel
        {
            CandidateName          = u.CandidateName,
            CandidatePartyOrSymbol = u.CandidatePartyOrSymbol,
            CandidateSlogan        = u.CandidateSlogan
        };
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var u = await _userManager.GetUserAsync(User);
        if (u == null) return Challenge();
        FullName          = u.FullName;
        CandidatePhotoUrl = u.CandidatePhotoUrl;

        if (!ModelState.IsValid) return Page();

        u.CandidateName          = Input.CandidateName?.Trim();
        u.CandidatePartyOrSymbol = Input.CandidatePartyOrSymbol?.Trim();
        u.CandidateSlogan        = Input.CandidateSlogan?.Trim();

        if (Input.CandidatePhoto is { Length: > 0 })
        {
            var saved = await SaveAsync(Input.CandidatePhoto, u.Id);
            if (saved != null)
            {
                if (!string.IsNullOrEmpty(u.CandidatePhotoUrl) &&
                    !string.Equals(u.CandidatePhotoUrl, saved, StringComparison.OrdinalIgnoreCase))
                {
                    TryDelete(u.CandidatePhotoUrl);
                }
                u.CandidatePhotoUrl = saved;
                CandidatePhotoUrl   = saved;
            }
        }

        await _userManager.UpdateAsync(u);
        TempData["Message"] = "Print profile updated.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRemovePhotoAsync()
    {
        var u = await _userManager.GetUserAsync(User);
        if (u == null) return Challenge();
        if (!string.IsNullOrEmpty(u.CandidatePhotoUrl))
        {
            TryDelete(u.CandidatePhotoUrl);
            u.CandidatePhotoUrl = null;
            await _userManager.UpdateAsync(u);
            TempData["Message"] = "Candidate photo removed.";
        }
        return RedirectToPage();
    }

    private async Task<string?> SaveAsync(IFormFile file, string userId)
    {
        const long maxBytes = 500 * 1024;
        var allowed = new[] { ".jpg", ".jpeg", ".png" };
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowed.Contains(ext) || file.Length > maxBytes)
        {
            TempData["Error"] = "Photo must be JPG/PNG under 500 KB.";
            return null;
        }

        var relDir = Path.Combine("uploads", "managers");
        var absDir = Path.Combine(_env.WebRootPath, relDir);
        Directory.CreateDirectory(absDir);

        var fileName = $"{userId}{ext}";
        var absPath = Path.Combine(absDir, fileName);
        await using var stream = System.IO.File.Create(absPath);
        await file.CopyToAsync(stream);
        return "/" + relDir.Replace('\\', '/') + "/" + fileName;
    }

    private void TryDelete(string? url)
    {
        if (string.IsNullOrEmpty(url)) return;
        var abs = Path.Combine(_env.WebRootPath, url.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        if (System.IO.File.Exists(abs))
        {
            try { System.IO.File.Delete(abs); } catch { /* ignore */ }
        }
    }
}
