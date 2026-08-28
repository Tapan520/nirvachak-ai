using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Nirvachak_AI.Domain.Entities;
using Nirvachak_AI.Domain.Enums;
using Nirvachak_AI.Infrastructure.Data;
using Nirvachak_AI.Infrastructure.Services;
using Nirvachak_AI.Pages.Announcements;

namespace Nirvachak_AI.Pages.Dashboard;

[Microsoft.AspNetCore.Authorization.Authorize]
public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _userManager;
    private readonly WinProbabilityService _winProbability;

    public IndexModel(AppDbContext db, UserManager<AppUser> userManager, WinProbabilityService winProbability)
    {
        _db             = db;
        _userManager    = userManager;
        _winProbability = winProbability;
    }

    // -- Drill-down filters ---------------------------------------
    [BindProperty(SupportsGet = true)]
    public int? SelectedConstituencyId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SelectedWard { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? SelectedBoothNumber { get; set; }

    // -- Role / identity ------------------------------------------
    public bool IsAdmin       { get; set; }
    public bool IsFieldWorker { get; set; }  // FieldWorker or BoothAgent
    public UserRole CurrentRole { get; set; }
    public string? CurrentUserName { get; set; }

    // -- Dropdown sources -----------------------------------------
    public List<Constituency> Constituencies { get; set; } = new();
    public List<Ward>         Wards          { get; set; } = new();
    public List<Booth>        BoothOptions   { get; set; } = new();
    public string?            ActiveConstituencyName { get; set; }

    // -- Full-dashboard stats (Admin/Manager/Candidate) -----------
    public int TotalVoters        { get; set; }
    public int FavourVoters       { get; set; }
    public int TotalBooths        { get; set; }
    public int OpenGrievances     { get; set; }
    public int TotalVolunteers    { get; set; }
    public int ActiveVolunteers   { get; set; }
    public decimal TotalExpenses  { get; set; }
    public int ECCompliantExpenses { get; set; }
    public Dictionary<string, int> SentimentBreakdown { get; set; } = new();
    public Dictionary<string, int> ReligionBreakdown  { get; set; } = new();
    public Dictionary<string, int> CasteBreakdown     { get; set; } = new();
    public int ProfiledVoterCount { get; set; }

    // Contact coverage
    public int ContactedVoters        { get; set; }
    public int NeverContactedVoters   { get; set; }
    public int ContactCoveragePercent { get; set; }

    // EC budget
    public decimal ECBudgetLimit   { get; set; } = 4_000_000m;
    public int     ECBudgetPercent { get; set; }

    // Survey
    public int SurveyCompletedCount { get; set; }
    public int SurveyPendingCount   { get; set; }

    public List<Booth>         BoothSummary        { get; set; } = new();
    public List<CampaignEvent> UpcomingEvents       { get; set; } = new();

    // -- Field-worker focused stats -------------------------------
    public int   MyAssignedVoters    { get; set; }
    public int   MyContactedToday    { get; set; }
    public int   MyTotalContacted    { get; set; }
    public int   MyFavourVoters      { get; set; }
    public int   MyPendingVoters     { get; set; }   // not yet contacted
    public List<Voter>         MyNextVoters         { get; set; } = new();  // up to 10 uncontacted Favour voters
    public List<DoorToDoorVisit> MyTodayVisits      { get; set; } = new();
    public List<CampaignEvent>   MyUpcomingEvents   { get; set; } = new();

    // -- Win Probability (Admin/Manager/Candidate) ---------------
    public WinProbabilityResult? WinProbability { get; set; }

    // -- Announcements (all roles) --------------------------------
    public List<AnnouncementViewModel> CriticalAlerts       { get; set; } = new();
    public List<AnnouncementViewModel> RecentAnnouncements  { get; set; } = new();
    public int UnacknowledgedCount { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);

        // VoterManager only has access to the Voters section — redirect them directly
        if (user?.Role == UserRole.VoterManager)
            return RedirectToPage("/Voters/Index");

        IsAdmin       = user?.Role == UserRole.SuperAdmin;
        IsFieldWorker = user?.Role is UserRole.FieldWorker or UserRole.BoothAgent;
        CurrentRole   = user?.Role ?? UserRole.FieldWorker;
        CurrentUserName = user?.FullName;

        int? cId = IsAdmin ? SelectedConstituencyId : user?.ConstituencyId;

        if (IsAdmin)
            Constituencies = await _db.Constituencies.OrderBy(c => c.Name).ToListAsync();

        if (cId.HasValue)
            ActiveConstituencyName = (await _db.Constituencies.FindAsync(cId.Value))?.Name;

        if (cId.HasValue)
            Wards = await _db.Wards
                .Where(w => w.ConstituencyId == cId.Value)
                .OrderBy(w => w.WardNumber).ToListAsync();

        if (cId.HasValue)
        {
            var boothQ = _db.Booths.Where(b => b.ConstituencyId == cId.Value);
            if (!string.IsNullOrEmpty(SelectedWard))
                boothQ = boothQ.Where(b => b.WardNumber == SelectedWard);
            BoothOptions = await boothQ.OrderBy(b => b.BoothNumber).ToListAsync();
        }

        // -- Load announcements (all roles) -----------------------
        await LoadAnnouncementsAsync(user);

        // -- Field-worker view ------------------------------------
        if (IsFieldWorker && user != null)
        {
            await LoadFieldWorkerStatsAsync(user, cId);
            return Page(); // skip full-dashboard queries
        }

        // -- Full dashboard (Admin / Manager / Candidate) ---------
        IQueryable<Voter>        voters     = _db.Voters.Where(v => !v.IsDeleted);
        IQueryable<Booth>        booths     = _db.Booths;
        IQueryable<Grievance>    grievances = _db.Grievances;
        IQueryable<Volunteer>    volunteers = _db.Volunteers;
        IQueryable<Expense>      expenses   = _db.Expenses;
        IQueryable<CampaignEvent> events    = _db.CampaignEvents;

        if (cId.HasValue)
        {
            voters     = voters.Where(v => v.ConstituencyId == cId);
            booths     = booths.Where(b => b.ConstituencyId == cId);
            grievances = grievances.Where(g => g.ConstituencyId == cId);
            volunteers = volunteers.Where(v => v.ConstituencyId == cId);
            expenses   = expenses.Where(e => e.ConstituencyId == cId);
            events     = events.Where(e => e.ConstituencyId == cId);
        }

        if (!string.IsNullOrEmpty(SelectedWard))
        {
            voters     = voters.Where(v => v.WardNumber == SelectedWard);
            booths     = booths.Where(b => b.WardNumber == SelectedWard);
            grievances = grievances.Where(g => g.Ward == SelectedWard);
        }

        if (SelectedBoothNumber.HasValue)
        {
            voters     = voters.Where(v => v.BoothNumber == SelectedBoothNumber.Value);
            booths     = booths.Where(b => b.BoothNumber == SelectedBoothNumber.Value);
            grievances = grievances.Where(g => g.BoothNumber == SelectedBoothNumber.Value);
        }

        TotalVoters         = await voters.CountAsync();
        FavourVoters        = await voters.CountAsync(v => v.Sentiment == VoterSentiment.Favour);
        TotalBooths         = await booths.CountAsync();
        OpenGrievances      = await grievances.CountAsync(g => g.Status == GrievanceStatus.Open);
        TotalVolunteers     = await volunteers.CountAsync();
        ActiveVolunteers    = await volunteers.CountAsync(v => v.IsActive);
        TotalExpenses       = (decimal)(await expenses.SumAsync(e => (double?)e.Amount) ?? 0);
        ECCompliantExpenses = await expenses.CountAsync(e => e.IsECCompliant);

        ContactedVoters       = await voters.CountAsync(v => v.LastContactedAt != null);
        NeverContactedVoters  = TotalVoters - ContactedVoters;
        ContactCoveragePercent = TotalVoters > 0
            ? (int)Math.Round((double)ContactedVoters / TotalVoters * 100) : 0;

        ECBudgetPercent = ECBudgetLimit > 0
            ? (int)Math.Min(100, Math.Round((double)TotalExpenses / (double)ECBudgetLimit * 100)) : 0;

        BoothSummary = await booths.OrderBy(b => b.BoothNumber).Take(8).ToListAsync();

        UpcomingEvents = await events
            .Where(e => e.ScheduledAt >= DateTime.Now && !e.IsCompleted)
            .OrderBy(e => e.ScheduledAt).Take(5).ToListAsync();

        var sentimentCounts = await voters
            .GroupBy(v => v.Sentiment)
            .Select(g => new { Sentiment = g.Key, Count = g.Count() })
            .ToListAsync();

        foreach (VoterSentiment s in Enum.GetValues<VoterSentiment>())
        {
            var found = sentimentCounts.FirstOrDefault(x => x.Sentiment == s);
            SentimentBreakdown[s.ToString()] = found?.Count ?? 0;
        }

        var voterIds = await voters.Select(v => v.Id).ToListAsync();

        // Use chunked Contains to stay within SQLite IN-clause limits
        ProfiledVoterCount   = await _db.VoterProfiles
            .CountAsync(p => voters.Select(v => v.Id).Contains(p.VoterId));
        SurveyCompletedCount = await _db.SurveyCompletions
            .CountAsync(s => voters.Select(v => v.Id).Contains(s.VoterId));
        SurveyPendingCount   = TotalVoters - SurveyCompletedCount;

        var profiles = voterIds.Count == 0
            ? new List<VoterProfile>()
            : await _db.VoterProfiles
                .Where(p => voterIds.Contains(p.VoterId))
                .AsNoTracking().ToListAsync();

        ReligionBreakdown = profiles
            .Where(p => !string.IsNullOrEmpty(p.Religion))
            .GroupBy(p => p.Religion!).OrderByDescending(g => g.Count())
            .ToDictionary(g => g.Key, g => g.Count());

        CasteBreakdown = profiles
            .Where(p => !string.IsNullOrEmpty(p.CasteCategory))
            .GroupBy(p => p.CasteCategory!).OrderByDescending(g => g.Count())
            .ToDictionary(g => g.Key, g => g.Count());

        // -- Win Probability --------------------------------------
        if (cId.HasValue)
            WinProbability = await _winProbability.ComputeAsync(cId.Value);

        return Page();
    }

    // ------------------------------------------------------------
    private async Task LoadFieldWorkerStatsAsync(AppUser user, int? cId)
    {
        // Resolve assigned booths/ward
        var assignedBooths = (user.AssignedBoothNumbers ?? "")
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => int.TryParse(s.Trim(), out var n) ? (int?)n : null)
            .Where(n => n.HasValue).Select(n => n!.Value).ToList();
        var assignedWard = user.AssignedWard?.Trim();

        IQueryable<Voter> voters = _db.Voters.Where(v => !v.IsDeleted);
        if (cId.HasValue) voters = voters.Where(v => v.ConstituencyId == cId);
        if (assignedBooths.Any())
            voters = voters.Where(v => assignedBooths.Contains(v.BoothNumber));
        else if (!string.IsNullOrEmpty(assignedWard))
            voters = voters.Where(v => v.WardNumber == assignedWard);

        MyAssignedVoters = await voters.CountAsync();
        MyFavourVoters   = await voters.CountAsync(v => v.Sentiment == VoterSentiment.Favour);
        MyTotalContacted = await voters.CountAsync(v => v.LastContactedAt != null);
        MyPendingVoters  = MyAssignedVoters - MyTotalContacted;

        var todayStart = DateTime.UtcNow.Date;
        MyContactedToday = await _db.DoorToDoorVisits
            .CountAsync(v => v.WorkerUserId == user.Id && v.VisitedAt >= todayStart);

        // Next 10 uncontacted Favour voters to visit
        MyNextVoters = await voters
            .Where(v => v.Sentiment == VoterSentiment.Favour && v.LastContactedAt == null)
            .OrderBy(v => v.BoothNumber).ThenBy(v => v.SerialNumber)
            .Take(10).ToListAsync();

        // Today's visit log
        MyTodayVisits = await _db.DoorToDoorVisits
            .Include(v => v.Voter)
            .Where(v => v.WorkerUserId == user.Id && v.VisitedAt >= todayStart)
            .OrderByDescending(v => v.VisitedAt).Take(20).ToListAsync();

        // Upcoming events
        if (cId.HasValue)
            MyUpcomingEvents = await _db.CampaignEvents
                .Where(e => e.ConstituencyId == cId && e.ScheduledAt >= DateTime.Now && !e.IsCompleted)
                .OrderBy(e => e.ScheduledAt).Take(3).ToListAsync();
    }

    private async Task LoadAnnouncementsAsync(AppUser? user)
    {
        if (user == null) return;
        var now     = DateTime.UtcNow;
        var roleStr = user.Role.ToString();
        var annList = await _db.Announcements
            .Include(a => a.Acknowledgements)
            .Where(a => a.IsActive && (a.ExpiresAt == null || a.ExpiresAt > now)
                && (a.ConstituencyId == null || a.ConstituencyId == user.ConstituencyId || IsAdmin)
                && (a.TargetRoles == "All"
                    || a.TargetRoles == roleStr
                    || a.TargetRoles.StartsWith(roleStr + ",")
                    || a.TargetRoles.EndsWith("," + roleStr)
                    || a.TargetRoles.Contains("," + roleStr + ",")
                    || a.CreatedByUserId == user.Id))
            .OrderByDescending(a => a.IsPinned).ThenByDescending(a => a.CreatedAt)
            .Take(20).ToListAsync();

        var mapped = annList.Select(a => new AnnouncementViewModel
        {
            Announcement         = a,
            IsAcknowledged       = a.Acknowledgements.Any(x => x.UserId == user.Id),
            AcknowledgementCount = a.Acknowledgements.Count
        }).ToList();

        CriticalAlerts      = mapped.Where(v => v.Announcement.IsPinned).ToList();
        RecentAnnouncements = mapped.Where(v => !v.Announcement.IsPinned).Take(5).ToList();
        UnacknowledgedCount = mapped.Count(v => v.Announcement.RequiresAcknowledgement && !v.IsAcknowledged);
    }
}
