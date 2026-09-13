using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Nirvachak_AI.Domain.Entities;
using Nirvachak_AI.Domain.Enums;
using Nirvachak_AI.Infrastructure.Data;

namespace Nirvachak_AI.Pages.Admin.CandidatesParties;

[Authorize(Roles = "Admin,CampaignManager,SuperAdmin")]
public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _userManager;
    private readonly IWebHostEnvironment _env;

    public IndexModel(AppDbContext db, UserManager<AppUser> userManager, IWebHostEnvironment env)
    {
        _db = db;
        _userManager = userManager;
        _env = env;
    }

    public List<SurveyCandidate> Candidates { get; set; } = new();
    public List<SurveyParty> Parties { get; set; } = new();
    public List<Constituency> Constituencies { get; set; } = new();
    public bool IsAdmin { get; set; }

    [BindProperty(SupportsGet = true)] public int? ConstituencyFilter { get; set; }

    // Candidate bind props
    [BindProperty] public string CandidateName { get; set; } = string.Empty;
    [BindProperty] public string? CandidatePartyAffiliation { get; set; }
    [BindProperty] public string? CandidateNotes { get; set; }
    [BindProperty] public int CandidateConstituencyId { get; set; }
    [BindProperty] public IFormFile? CandidatePhoto { get; set; }
    [BindProperty] public int CandidateDisplayOrder { get; set; }

    // Party bind props
    [BindProperty] public string PartyName { get; set; } = string.Empty;
    [BindProperty] public string? PartySymbol { get; set; }
    [BindProperty] public string? PartyNotes { get; set; }
    [BindProperty] public int PartyConstituencyId { get; set; }

    public async Task OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        IsAdmin = user?.Role == UserRole.SuperAdmin;

        if (IsAdmin)
            Constituencies = await _db.Constituencies.OrderBy(c => c.Name).ToListAsync();

        int? cId = IsAdmin ? ConstituencyFilter : user?.ConstituencyId;

        var cQuery = _db.SurveyCandidates.Include(c => c.Constituency).AsQueryable();
        var pQuery = _db.SurveyParties.Include(p => p.Constituency).AsQueryable();

        if (cId.HasValue)
        {
            cQuery = cQuery.Where(c => c.ConstituencyId == cId.Value);
            pQuery = pQuery.Where(p => p.ConstituencyId == cId.Value);
        }

        Candidates = await cQuery.OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name).ToListAsync();
        Parties    = await pQuery.OrderBy(p => p.Name).ToListAsync();
    }

    public async Task<IActionResult> OnPostAddCandidateAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        int cId = user?.Role == UserRole.SuperAdmin ? CandidateConstituencyId : (user?.ConstituencyId ?? 0);

        var candidate = new SurveyCandidate
        {
            Name               = CandidateName.Trim(),
            PartyAffiliation   = CandidatePartyAffiliation?.Trim(),
            Notes              = CandidateNotes?.Trim(),
            ConstituencyId     = cId,
            DisplayOrder       = CandidateDisplayOrder,
            IsActive           = true
        };
        _db.SurveyCandidates.Add(candidate);
        await _db.SaveChangesAsync();

        if (CandidatePhoto is { Length: > 0 })
            candidate.PhotoUrl = await SavePhotoAsync(CandidatePhoto, cId, candidate.Id);

        await _db.SaveChangesAsync();
        TempData["Message"] = $"Candidate '{CandidateName}' added.";
        return RedirectToPage(new { ConstituencyFilter });
    }

    public async Task<IActionResult> OnPostUploadPhotoAsync(int id, IFormFile photo)
    {
        var c = await _db.SurveyCandidates.FindAsync(id);
        if (c == null) return RedirectToPage(new { ConstituencyFilter });

        if (photo is { Length: > 0 })
            c.PhotoUrl = await SavePhotoAsync(photo, c.ConstituencyId, c.Id);

        await _db.SaveChangesAsync();
        TempData["Message"] = $"Photo updated for '{c.Name}'.";
        return RedirectToPage(new { ConstituencyFilter });
    }

    public async Task<IActionResult> OnPostRemovePhotoAsync(int id)
    {
        var c = await _db.SurveyCandidates.FindAsync(id);
        if (c == null) return RedirectToPage(new { ConstituencyFilter });

        TryDeletePhoto(c.PhotoUrl);
        c.PhotoUrl = null;
        await _db.SaveChangesAsync();
        TempData["Message"] = $"Photo removed for '{c.Name}'.";
        return RedirectToPage(new { ConstituencyFilter });
    }

    public async Task<IActionResult> OnPostSetOrderAsync(int id, int order)
    {
        var c = await _db.SurveyCandidates.FindAsync(id);
        if (c != null) { c.DisplayOrder = order; await _db.SaveChangesAsync(); }
        return RedirectToPage(new { ConstituencyFilter });
    }

    public async Task<IActionResult> OnPostAddPartyAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        int cId = user?.Role == UserRole.SuperAdmin ? PartyConstituencyId : (user?.ConstituencyId ?? 0);

        _db.SurveyParties.Add(new SurveyParty
        {
            Name           = PartyName.Trim(),
            Symbol         = PartySymbol?.Trim(),
            Notes          = PartyNotes?.Trim(),
            ConstituencyId = cId,
            IsActive       = true
        });
        await _db.SaveChangesAsync();
        TempData["Message"] = $"Party '{PartyName}' added.";
        return RedirectToPage(new { ConstituencyFilter });
    }

    public async Task<IActionResult> OnPostToggleCandidateAsync(int id)
    {
        var c = await _db.SurveyCandidates.FindAsync(id);
        if (c != null) { c.IsActive = !c.IsActive; await _db.SaveChangesAsync(); }
        return RedirectToPage(new { ConstituencyFilter });
    }

    public async Task<IActionResult> OnPostTogglePartyAsync(int id)
    {
        var p = await _db.SurveyParties.FindAsync(id);
        if (p != null) { p.IsActive = !p.IsActive; await _db.SaveChangesAsync(); }
        return RedirectToPage(new { ConstituencyFilter });
    }

    public async Task<IActionResult> OnPostDeleteCandidateAsync(int id)
    {
        var c = await _db.SurveyCandidates.FindAsync(id);
        if (c != null)
        {
            TryDeletePhoto(c.PhotoUrl);
            _db.SurveyCandidates.Remove(c);
            await _db.SaveChangesAsync();
        }
        TempData["Message"] = "Candidate deleted.";
        return RedirectToPage(new { ConstituencyFilter });
    }

    public async Task<IActionResult> OnPostDeletePartyAsync(int id)
    {
        var p = await _db.SurveyParties.FindAsync(id);
        if (p != null) { _db.SurveyParties.Remove(p); await _db.SaveChangesAsync(); }
        TempData["Message"] = "Party deleted.";
        return RedirectToPage(new { ConstituencyFilter });
    }

    // ?? helpers ?????????????????????????????????????????????????????????????

    private async Task<string?> SavePhotoAsync(IFormFile file, int constituencyId, int candidateId)
    {
        const long maxBytes = 500 * 1024; // 500 KB
        var allowed = new[] { ".jpg", ".jpeg", ".png" };
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowed.Contains(ext) || file.Length > maxBytes)
        {
            TempData["Error"] = "Photo must be JPG/PNG and under 500 KB.";
            return null;
        }

        var relDir = Path.Combine("uploads", "candidates", constituencyId.ToString());
        var absDir = Path.Combine(_env.WebRootPath, relDir);
        Directory.CreateDirectory(absDir);

        var fileName = $"{candidateId}{ext}";
        var absPath = Path.Combine(absDir, fileName);

        await using (var stream = System.IO.File.Create(absPath))
            await file.CopyToAsync(stream);

        return "/" + relDir.Replace('\\', '/') + "/" + fileName;
    }

    private void TryDeletePhoto(string? photoUrl)
    {
        if (string.IsNullOrEmpty(photoUrl)) return;
        var rel = photoUrl.TrimStart('/');
        var abs = Path.Combine(_env.WebRootPath, rel.Replace('/', Path.DirectorySeparatorChar));
        if (System.IO.File.Exists(abs))
        {
            try { System.IO.File.Delete(abs); } catch { /* ignore */ }
        }
    }
}
