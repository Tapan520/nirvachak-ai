using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Nirvachak_AI.Domain.Entities;
using Nirvachak_AI.Domain.Enums;
using Nirvachak_AI.Infrastructure.Data;
using Nirvachak_AI.Infrastructure.Services;

namespace Nirvachak_AI.Pages.PhoneBanking;

[Authorize(Roles = "Admin,SuperAdmin,CampaignManager,Candidate,FieldWorker,BoothAgent")]
public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _userManager;
    private readonly IExotelService _exotel;

    public IndexModel(AppDbContext db, UserManager<AppUser> userManager, IExotelService exotel)
    {
        _db = db;
        _userManager = userManager;
        _exotel = exotel;
    }

    public bool IsExotelEnabled { get; set; }

    public List<PhoneCallLog> TodaysCalls { get; set; } = new();
    public List<Voter> PendingCallVoters { get; set; } = new();
    public int TotalCallsToday { get; set; }
    public int TalkedCount { get; set; }
    public int NoAnswerCount { get; set; }
    public int CallBackCount { get; set; }

    public async Task OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        var isSuperAdmin = user?.Role == UserRole.SuperAdmin;
        int? cId = user?.ConstituencyId;
        // Non-SuperAdmin users are always scoped to their own constituency
        var todayStart = DateTime.UtcNow.Date;

        IQueryable<PhoneCallLog> callQ = _db.PhoneCallLogs.Include(c => c.Voter).AsNoTracking();
        if (!isSuperAdmin && user != null) callQ = callQ.Where(c => c.CalledByUserId == user.Id);
        if (!isSuperAdmin && cId.HasValue) callQ = callQ.Where(c => c.ConstituencyId == cId.Value);

        TodaysCalls = await callQ
            .Where(c => c.CalledAt >= todayStart)
            .OrderByDescending(c => c.CalledAt).Take(50).ToListAsync();

        TotalCallsToday = TodaysCalls.Count;
        TalkedCount    = TodaysCalls.Count(c => c.Outcome == CallOutcome.Talked);
        NoAnswerCount  = TodaysCalls.Count(c => c.Outcome == CallOutcome.NoAnswer);
        CallBackCount  = TodaysCalls.Count(c => c.Outcome == CallOutcome.CallBack);

        // Voters with mobile number not yet called today
        var calledVoterIds = TodaysCalls.Select(c => c.VoterId).ToHashSet();
        IQueryable<Voter> voterQ = _db.Voters.Where(v => !v.IsDeleted && v.MobileNumber != null);
        if (!isSuperAdmin && cId.HasValue) voterQ = voterQ.Where(v => v.ConstituencyId == cId.Value);
        PendingCallVoters = await voterQ
            .Where(v => !calledVoterIds.Contains(v.Id)
                && (v.Sentiment == VoterSentiment.Floating || v.Sentiment == VoterSentiment.Unknown))
            .OrderBy(v => v.BoothNumber).ThenBy(v => v.SerialNumber)
            .Take(20).ToListAsync();

        if (user?.ConstituencyId.HasValue == true)
            IsExotelEnabled = await _exotel.IsConfiguredAsync(user.ConstituencyId.Value);
    }
}
