using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Nirvachak_AI.Domain.Entities;
using Nirvachak_AI.Domain.Enums;
using Nirvachak_AI.Infrastructure.Data;
using Nirvachak_AI.Infrastructure.Services;

namespace Nirvachak_AI.Pages.Voters;

public class DetailsModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _userManager;
    private readonly AuditService _audit;

    public DetailsModel(AppDbContext db, UserManager<AppUser> userManager, AuditService audit)
    {
        _db = db;
        _userManager = userManager;
        _audit = audit;
    }

    public Voter? Voter { get; set; }
    public List<DoorToDoorVisit> Visits { get; set; } = new();
    public VoterProfile? SurveyProfile { get; set; }
    public VoterConsent? ConsentRecord { get; set; }
    public bool SurveyCompleted { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var currentUser = await _userManager.GetUserAsync(User);
        Voter = await _db.Voters.FindAsync(id);
        if (Voter == null) return NotFound();
        if (currentUser?.Role != UserRole.SuperAdmin && Voter.ConstituencyId != currentUser?.ConstituencyId)
            return Forbid();
        Visits = await _db.DoorToDoorVisits
            .Where(v => v.VoterId == id)
            .OrderByDescending(v => v.VisitedAt)
            .ToListAsync();
        SurveyProfile  = await _db.VoterProfiles.FirstOrDefaultAsync(p => p.VoterId == id);
        ConsentRecord  = await _db.VoterConsents.FirstOrDefaultAsync(c => c.VoterId == id);
        SurveyCompleted = await _db.SurveyCompletions.AnyAsync(s => s.VoterId == id);
        return Page();
    }

    private bool IsRestrictedRole(AppUser? user)
        => user?.Role == UserRole.FieldWorker || user?.Role == UserRole.BoothAgent;

    public async Task<IActionResult> OnPostUpdateSentimentAsync(int id, VoterSentiment sentiment)
    {
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser == null) return Forbid();

        var voter = await _db.Voters.FindAsync(id);
        if (voter != null)
        {
            if (currentUser.Role != UserRole.SuperAdmin && voter.ConstituencyId != currentUser.ConstituencyId)
                return Forbid();
            var previous = voter.Sentiment;
            voter.Sentiment = sentiment;
            _audit.Track(
                currentUser.Id, currentUser.FullName,
                "UpdateSentiment", "Voter", id.ToString(),
                $"Sentiment changed from {previous} to {sentiment} for voter {voter.Name} ({voter.VoterId})",
                currentUser.ConstituencyId);
            await _db.SaveChangesAsync();
            TempData["Message"] = "Sentiment updated successfully.";
        }
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostLogVisitAsync(int id, VisitStatus visitStatus, VoterSentiment sentiment, string? notes)
    {
        var user = await _userManager.GetUserAsync(User);
        var voter = await _db.Voters.FindAsync(id);
        if (voter != null && user != null)
        {
            if (user.Role != UserRole.SuperAdmin && voter.ConstituencyId != user.ConstituencyId)
                return Forbid();
            var visit = new DoorToDoorVisit
            {
                VoterId = id,
                WorkerUserId = user.Id,
                WorkerName = user.FullName,
                VisitedAt = DateTime.UtcNow,
                Status = visitStatus,
                SentimentAfterVisit = sentiment,
                Notes = notes
            };
            _db.DoorToDoorVisits.Add(visit);
            voter.Sentiment = sentiment;
            voter.LastContactedAt = DateTime.UtcNow;
            _audit.Track(
                user.Id, user.FullName,
                "LogVisit", "Voter", id.ToString(),
                $"Visit logged for {voter.Name} ({voter.VoterId}): {visitStatus}, sentiment={sentiment}",
                user.ConstituencyId);
            await _db.SaveChangesAsync();
            TempData["Message"] = "Visit logged successfully.";
        }
        return RedirectToPage(new { id });
    }
}
