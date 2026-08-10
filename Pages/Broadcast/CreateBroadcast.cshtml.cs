using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Nirvachak_AI.Domain.Entities;
using Nirvachak_AI.Domain.Enums;
using Nirvachak_AI.Infrastructure.Data;

namespace Nirvachak_AI.Pages.Broadcast;

[Authorize(Roles = "Admin,SuperAdmin,CampaignManager,Candidate")]
public class CreateBroadcastModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _userManager;
    public CreateBroadcastModel(AppDbContext db, UserManager<AppUser> userManager) { _db = db; _userManager = userManager; }

    [BindProperty] public MessageBroadcast Broadcast { get; set; } = new();
    [BindProperty] public string? TargetSentiment { get; set; }
    [BindProperty] public string? TargetWard { get; set; }
    public SelectList? TemplateItems { get; set; }
    public int EstimatedCount { get; set; }

    public async Task OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        int? cId = user?.ConstituencyId;
        var templates = await _db.MessageTemplates.Where(t => !cId.HasValue || t.ConstituencyId == cId).ToListAsync();
        TemplateItems = new SelectList(templates, "Id", "Title");
        EstimatedCount = await _db.Voters.CountAsync(v => !v.IsDeleted && v.MobileNumber != null && (!cId.HasValue || v.ConstituencyId == cId));
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        int? cId = user?.ConstituencyId;
        var templates = await _db.MessageTemplates.Where(t => !cId.HasValue || t.ConstituencyId == cId).ToListAsync();
        TemplateItems = new SelectList(templates, "Id", "Title");
        EstimatedCount = await _db.Voters.CountAsync(v => !v.IsDeleted && v.MobileNumber != null && (!cId.HasValue || v.ConstituencyId == cId));

        if (!ModelState.IsValid) return Page();

        var voterQ = _db.Voters.Where(v => !v.IsDeleted && v.MobileNumber != null);
        if (cId.HasValue) voterQ = voterQ.Where(v => v.ConstituencyId == cId);
        if (!string.IsNullOrEmpty(TargetSentiment) && Enum.TryParse<VoterSentiment>(TargetSentiment, out var sent))
            voterQ = voterQ.Where(v => v.Sentiment == sent);
        if (!string.IsNullOrEmpty(TargetWard))
            voterQ = voterQ.Where(v => v.WardNumber == TargetWard);

        // Only voters who consented to WhatsApp
        var consentedIds = await _db.VoterConsents.Where(c => c.AllowWhatsAppMessages).Select(c => c.VoterId).ToListAsync();
        voterQ = voterQ.Where(v => consentedIds.Contains(v.Id));

        int count = await voterQ.CountAsync();

        var parts = new List<string>();
        if (!string.IsNullOrEmpty(TargetSentiment)) parts.Add($"Sentiment: {TargetSentiment}");
        if (!string.IsNullOrEmpty(TargetWard)) parts.Add($"Ward: {TargetWard}");
        if (!parts.Any()) parts.Add("All voters with WhatsApp consent");

        Broadcast.ConstituencyId = cId ?? 1;
        Broadcast.CreatedByUserId = user?.Id ?? string.Empty;
        Broadcast.CreatedByName = user?.FullName;
        Broadcast.TotalTargeted = count;
        Broadcast.TargetDescription = string.Join(", ", parts);
        Broadcast.Status = Broadcast.ScheduledAt.HasValue ? BroadcastStatus.Scheduled : BroadcastStatus.Draft;
        Broadcast.CreatedAt = DateTime.UtcNow;
        _db.MessageBroadcasts.Add(Broadcast);
        await _db.SaveChangesAsync();
        TempData["Message"] = $"Broadcast created. {count} voters targeted.";
        return RedirectToPage("/Broadcast/Index");
    }
}
