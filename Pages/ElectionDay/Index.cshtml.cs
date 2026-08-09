using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Nirvachak_AI.Domain.Entities;
using Nirvachak_AI.Domain.Enums;
using Nirvachak_AI.Hubs;
using Nirvachak_AI.Infrastructure.Data;
using Nirvachak_AI.Infrastructure.Services;

namespace Nirvachak_AI.Pages.ElectionDay;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly ElectionDayService _electionDayService;
    private readonly UserManager<AppUser> _userManager;
    private readonly IHubContext<ElectionDayHub> _hub;
    private readonly IExotelService _exotel;

    public IndexModel(AppDbContext db, ElectionDayService electionDayService,
        UserManager<AppUser> userManager, IHubContext<ElectionDayHub> hub,
        IExotelService exotel)
    {
        _db                 = db;
        _electionDayService = electionDayService;
        _userManager        = userManager;
        _hub                = hub;
        _exotel             = exotel;
    }

    public List<BoothTurnoutDto> BoothTurnout { get; set; } = new();
    public List<Voter> TodayVoters { get; set; } = new();
    public List<Voter> NotYetVotedFavour { get; set; } = new();
    public List<Constituency> Constituencies { get; set; } = new();
    public bool IsAdmin { get; set; }
    public bool ExotelConfigured { get; set; }
    public int TotalVoters { get; set; }
    public int TotalVoted { get; set; }
    public double OverallPercent { get; set; }
    public int ConstituencyId { get; set; }
    public int SmsBlastCount { get; set; }

    [Microsoft.AspNetCore.Mvc.BindProperty(SupportsGet = true)]
    public int? ConstituencyFilter { get; set; }

    public async Task OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        IsAdmin = user?.Role == UserRole.SuperAdmin;
        if (IsAdmin)
            Constituencies = await _db.Constituencies.OrderBy(c => c.Name).ToListAsync();

        ConstituencyId = IsAdmin
            ? (ConstituencyFilter ?? Constituencies.FirstOrDefault()?.Id ?? 1)
            : (user?.ConstituencyId ?? 1);
        BoothTurnout = await _electionDayService.GetLiveTurnoutAsync(ConstituencyId);
        var (total, voted, pct) = await _electionDayService.GetConstituencyTurnoutAsync(ConstituencyId);
        TotalVoters = total;
        TotalVoted = voted;
        OverallPercent = pct;
        TodayVoters = await _db.Voters
            .Where(v => v.ConstituencyId == ConstituencyId && !v.IsDeleted)
            .OrderBy(v => v.BoothNumber).ThenBy(v => v.SerialNumber)
            .Take(500).ToListAsync();

        // Priority chase list: Favour voters who haven't voted yet
        NotYetVotedFavour = await _db.Voters
            .Where(v => v.ConstituencyId == ConstituencyId && !v.IsDeleted
                     && v.ElectionDayStatus == ElectionDayStatus.NotVoted
                     && v.Sentiment == VoterSentiment.Favour)
            .OrderBy(v => v.BoothNumber).ThenBy(v => v.SerialNumber)
            .Take(200).ToListAsync();

        ExotelConfigured = await _exotel.IsConfiguredAsync(ConstituencyId);
        SmsBlastCount    = NotYetVotedFavour.Count(v => !string.IsNullOrEmpty(v.MobileNumber));
    }

    public async Task<IActionResult> OnPostMarkVotedAsync(int voterId)
    {
        if (voterId <= 0)
        {
            TempData["Error"] = "Please select a voter.";
            return RedirectToPage();
        }
        var user = await _userManager.GetUserAsync(User);
        var cId = user?.ConstituencyId ?? 1;
        var voter = await _db.Voters.FindAsync(voterId);
        if (voter != null)
        {
            if (voter.ElectionDayStatus == ElectionDayStatus.Voted)
            {
                TempData["Error"] = $"Voter {voter.Name} has already been marked as voted.";
                return RedirectToPage();
            }
            await _electionDayService.MarkVotedAsync(voterId);
            var booth = await _db.Booths.FirstOrDefaultAsync(b => b.BoothNumber == voter.BoothNumber && b.ConstituencyId == cId);
            if (booth != null)
                await ElectionDayHub.BroadcastTurnoutUpdate(_hub, cId, booth.BoothNumber, booth.VotedCount, booth.TotalVoters);
        TempData["Message"] = $"Voter {voter.Name} marked as voted.";
        }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostSmsBlastAsync(string? customMessage)
    {
        var user    = await _userManager.GetUserAsync(User);
        var isAdmin = user?.Role == UserRole.SuperAdmin;
        var cId     = isAdmin ? (ConstituencyFilter ?? user?.ConstituencyId ?? 1)
                              : (user?.ConstituencyId ?? 1);

        var constituency = await _db.Constituencies.FindAsync(cId);
        var defaultMsg   = $"Dear voter, polling is underway for {constituency?.Name ?? "your constituency"}. " +
                           "Please exercise your right to vote today. Your vote matters!";
        var message = string.IsNullOrWhiteSpace(customMessage) ? defaultMsg : customMessage.Trim();

        // Only send to Favour voters who haven't voted and have a mobile number
        var targets = await _db.Voters
            .Where(v => v.ConstituencyId == cId && !v.IsDeleted
                     && v.ElectionDayStatus == ElectionDayStatus.NotVoted
                     && v.Sentiment == VoterSentiment.Favour
                     && v.MobileNumber != null && v.MobileNumber != "")
            .OrderBy(v => v.BoothNumber)
            .Take(500)
            .ToListAsync();

        int sent = 0, failed = 0;
        foreach (var voter in targets)
        {
            var (success, _, _) = await _exotel.SendSmsAsync(cId, voter.MobileNumber!, message);
            if (success) sent++; else failed++;
        }

        TempData["Message"] = $"📱 SMS Blast complete: {sent} sent, {failed} failed.";
        return RedirectToPage(new { ConstituencyFilter });
    }
}
