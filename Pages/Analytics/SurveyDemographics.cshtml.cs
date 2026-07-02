using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Nirvachak_AI.Domain.Entities;
using Nirvachak_AI.Domain.Enums;
using Nirvachak_AI.Infrastructure.Data;
using System.Text.Json;

namespace Nirvachak_AI.Pages.Analytics;

[Authorize(Roles = "Admin,SuperAdmin,CampaignManager,Candidate,FieldWorker,BoothAgent")]
public class SurveyDemographicsModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _userManager;

    public SurveyDemographicsModel(AppDbContext db, UserManager<AppUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    // ?? Filters ???????????????????????????????????????????????????
    [BindProperty(SupportsGet = true)] public int?    FilterBooth { get; set; }
    [BindProperty(SupportsGet = true)] public string? FilterWard  { get; set; }
    [BindProperty(SupportsGet = true)] public string? FilterTab   { get; set; } = "analytics";

    public bool IsAdmin       { get; set; }
    public bool CanViewStats  { get; set; }
    public List<int>    AvailableBooths { get; set; } = new();
    public List<string> AvailableWards  { get; set; } = new();

    // ?? Summary stats ?????????????????????????????????????????????
    public int    TotalVoters    { get; set; }
    public int    CompletedCount { get; set; }
    public double CompletionRate { get; set; }
    public int    CouponsIssued  { get; set; }
    public int    CouponsRedeemed { get; set; }
    public int    PendingCount   { get; set; }

    // ?? Consent counts ????????????????????????????????????????????
    public int ConsentThirdParty { get; set; }
    public int ConsentCampaign   { get; set; }
    public int ConsentWhatsApp   { get; set; }
    public int ConsentScheme     { get; set; }
    public int ConsentAnalytics  { get; set; }

    // ?? Demographic breakdowns ????????????????????????????????????
    public Dictionary<string, int> ByCaste      { get; set; } = new();
    public Dictionary<string, int> ByReligion   { get; set; } = new();
    public Dictionary<string, int> ByEducation  { get; set; } = new();
    public Dictionary<string, int> ByOccupation { get; set; } = new();
    public Dictionary<string, int> ByIncome     { get; set; } = new();
    public Dictionary<string, int> ByAgeBracket { get; set; } = new();
    public Dictionary<string, int> TopConcerns  { get; set; } = new();
    public Dictionary<int, int>    CompletionByBooth { get; set; } = new();

    // ?? Voter completion list ?????????????????????????????????????
    public List<VoterCompletionRow> CompletedVoters { get; set; } = new();
    public List<VoterPendingRow>    PendingVoters   { get; set; } = new();
    public string SurveyBaseUrl { get; set; } = string.Empty;

    public record VoterCompletionRow(
        int VoterId, string Name, string VoterEpic,
        string? MobileNumber, int BoothNumber, string? WardNumber,
        DateTime CompletedAt, bool HasCoupon, string? CouponCode);

    public record VoterPendingRow(
        int VoterId, string Name, string VoterEpic,
        string? MobileNumber, int BoothNumber, string? WardNumber);

    public async Task OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        IsAdmin      = user?.Role == UserRole.SuperAdmin;
        CanViewStats = user?.Role is UserRole.Admin or UserRole.SuperAdmin or UserRole.CampaignManager or UserRole.Candidate;

        var cId = user?.ConstituencyId;

        // Resolve assigned booths for restricted roles
        var assignedBooths = (user?.Role is UserRole.FieldWorker or UserRole.BoothAgent)
            ? (user.AssignedBoothNumbers ?? "")
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => int.TryParse(s.Trim(), out var n) ? (int?)n : null)
                .Where(n => n.HasValue).Select(n => n!.Value).ToHashSet()
            : null;

        // Build voter base query scoped to constituency + assigned booths
        IQueryable<Voter> voterQuery = _db.Voters.Where(v => !v.IsDeleted);
        if (cId.HasValue)        voterQuery = voterQuery.Where(v => v.ConstituencyId == cId);
        if (assignedBooths != null && assignedBooths.Count > 0)
            voterQuery = voterQuery.Where(v => assignedBooths.Contains(v.BoothNumber));
        if (FilterBooth.HasValue)
            voterQuery = voterQuery.Where(v => v.BoothNumber == FilterBooth.Value);
        if (!string.IsNullOrEmpty(FilterWard))
            voterQuery = voterQuery.Where(v => v.WardNumber == FilterWard);

        var voterIds = await voterQuery.Select(v => v.Id).ToListAsync();

        // Populate filter dropdown options
        AvailableBooths = await voterQuery.Select(v => v.BoothNumber).Distinct().OrderBy(b => b).ToListAsync();
        AvailableWards  = await voterQuery.Where(v => v.WardNumber != null)
                            .Select(v => v.WardNumber!).Distinct().OrderBy(w => w).ToListAsync();

        // Completions within scope
        var completions = await _db.SurveyCompletions
            .Where(c => voterIds.Contains(c.VoterId))
            .AsNoTracking().ToListAsync();

        var completedVoterIds = completions.Select(c => c.VoterId).ToHashSet();

        TotalVoters    = voterIds.Count;
        CompletedCount = completions.Count;
        PendingCount   = TotalVoters - CompletedCount;
        CompletionRate = TotalVoters > 0 ? Math.Round((double)CompletedCount / TotalVoters * 100, 1) : 0;
        CouponsIssued  = completions.Count(c => c.CouponId.HasValue);
        CouponsRedeemed = await _db.CouponPools
            .Where(cp => completions.Select(c => c.CouponId).Contains(cp.Id))
            .CountAsync(cp => cp.IsRedeemed);

        // Consent counts (scoped to these voters)
        if (CanViewStats)
        {
            var consents = await _db.VoterConsents
                .Where(c => voterIds.Contains(c.VoterId))
                .AsNoTracking().ToListAsync();

            ConsentThirdParty = consents.Count(c => c.AllowThirdPartyAdvertising);
            ConsentCampaign   = consents.Count(c => c.AllowCampaignOutreach);
            ConsentWhatsApp   = consents.Count(c => c.AllowWhatsAppMessages);
            ConsentScheme     = consents.Count(c => c.AllowSchemeNotifications);
            ConsentAnalytics  = consents.Count(c => c.AllowDataForAnalytics);

            // Demographic breakdowns
            var profiles = await _db.VoterProfiles
                .Where(p => voterIds.Contains(p.VoterId))
                .AsNoTracking().ToListAsync();

            ByCaste      = GroupBy(profiles, p => p.CasteCategory);
            ByReligion   = GroupBy(profiles, p => p.Religion);
            ByEducation  = GroupBy(profiles, p => p.Education);
            ByOccupation = GroupBy(profiles, p => p.Occupation);
            ByIncome     = GroupBy(profiles, p => p.MonthlyIncomeBracket);
            ByAgeBracket = GroupBy(profiles, p => p.AgeBracket);

            var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var p in profiles.Where(p => !string.IsNullOrEmpty(p.PrimaryConcerns)))
            {
                var concerns = JsonSerializer.Deserialize<List<string>>(p.PrimaryConcerns!) ?? new();
                foreach (var c in concerns)
                    counts[c] = counts.GetValueOrDefault(c) + 1;
            }
            TopConcerns = counts.OrderByDescending(x => x.Value)
                .Take(10).ToDictionary(x => x.Key, x => x.Value);

            var boothMap = await voterQuery.ToDictionaryAsync(v => v.Id, v => v.BoothNumber);
            CompletionByBooth = completions
                .Where(c => boothMap.ContainsKey(c.VoterId))
                .GroupBy(c => boothMap[c.VoterId])
                .ToDictionary(g => g.Key, g => g.Count());
        }

        // Build survey base URL for share links
        var req = HttpContext.Request;
        SurveyBaseUrl = $"{req.Scheme}://{req.Host}/Survey";

        // ?? Completed voters list ??????????????????????????????????
        var couponMap = await _db.CouponPools
            .Where(cp => completions.Select(c => c.CouponId).Contains(cp.Id))
            .ToDictionaryAsync(cp => cp.Id, cp => cp.CouponCode);

        var completionCouponMap = completions
            .ToDictionary(c => c.VoterId, c => c.CouponId);

        var completedVoterData = await voterQuery
            .Where(v => completedVoterIds.Contains(v.Id))
            .OrderBy(v => v.BoothNumber).ThenBy(v => v.Name)
            .Select(v => new { v.Id, v.Name, v.VoterId, v.MobileNumber, v.BoothNumber, v.WardNumber })
            .ToListAsync();

        CompletedVoters = completedVoterData.Select(v =>
        {
            var couponId   = completionCouponMap.GetValueOrDefault(v.Id);
            var couponCode = couponId.HasValue && couponMap.TryGetValue(couponId.Value, out var code) ? code : null;
            return new VoterCompletionRow(v.Id, v.Name, v.VoterId, v.MobileNumber,
                v.BoothNumber, v.WardNumber,
                completions.First(c => c.VoterId == v.Id).CompletedAt,
                couponId.HasValue, couponCode);
        }).ToList();

        // ?? Pending voters list (not yet completed) ????????????????
        PendingVoters = await voterQuery
            .Where(v => !completedVoterIds.Contains(v.Id))
            .OrderBy(v => v.BoothNumber).ThenBy(v => v.Name)
            .Select(v => new VoterPendingRow(v.Id, v.Name, v.VoterId,
                v.MobileNumber, v.BoothNumber, v.WardNumber))
            .ToListAsync();
    }

    private static Dictionary<string, int> GroupBy(
        List<VoterProfile> profiles, Func<VoterProfile, string?> selector)
        => profiles
            .Where(p => !string.IsNullOrEmpty(selector(p)))
            .GroupBy(p => selector(p)!)
            .OrderByDescending(g => g.Count())
            .ToDictionary(g => g.Key, g => g.Count());

    public async Task<IActionResult> OnGetExportCompletedCsvAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        var cId  = user?.ConstituencyId;

        IQueryable<Voter> voterQuery = _db.Voters.Where(v => !v.IsDeleted);
        if (cId.HasValue) voterQuery = voterQuery.Where(v => v.ConstituencyId == cId);
        if (FilterBooth.HasValue) voterQuery = voterQuery.Where(v => v.BoothNumber == FilterBooth.Value);
        if (!string.IsNullOrEmpty(FilterWard)) voterQuery = voterQuery.Where(v => v.WardNumber == FilterWard);

        var voterIds = await voterQuery.Select(v => v.Id).ToListAsync();

        var completions = await _db.SurveyCompletions
            .Where(c => voterIds.Contains(c.VoterId))
            .AsNoTracking().ToListAsync();

        var completedIds = completions.Select(c => c.VoterId).ToHashSet();

        var couponMap = await _db.CouponPools
            .Where(cp => completions.Select(c => c.CouponId).Contains(cp.Id))
            .ToDictionaryAsync(cp => cp.Id, cp => cp.CouponCode);

        var completionMap = completions.ToDictionary(c => c.VoterId, c => c);

        var completed = await voterQuery
            .Where(v => completedIds.Contains(v.Id))
            .OrderBy(v => v.BoothNumber).ThenBy(v => v.Name)
            .Select(v => new { v.Name, v.VoterId, v.MobileNumber, v.BoothNumber, v.WardNumber, v.Id })
            .ToListAsync();

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Name,EPIC No.,Mobile,Booth,Ward,Completed On,Coupon Code");
        foreach (var v in completed)
        {
            var comp      = completionMap.GetValueOrDefault(v.Id);
            var couponId  = comp?.CouponId;
            var coupon    = couponId.HasValue && couponMap.TryGetValue(couponId.Value, out var code) ? code : "";
            var completedOn = comp?.CompletedAt.ToLocalTime().ToString("dd MMM yyyy HH:mm") ?? "";
            sb.AppendLine(string.Join(",",
                $"\"{ v.Name.Replace("\"", "\"\"")}\"",
                v.VoterId,
                v.MobileNumber ?? "",
                v.BoothNumber.ToString(),
                v.WardNumber ?? "",
                completedOn,
                coupon));
        }

        var bytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
        return File(bytes, "text/csv", $"completed_survey_{DateTime.Today:yyyyMMdd}.csv");
    }

    public async Task<IActionResult> OnGetExportProfilesCsvAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        var cId  = user?.ConstituencyId;

        IQueryable<Voter> voterQuery = _db.Voters.Where(v => !v.IsDeleted);
        if (cId.HasValue) voterQuery = voterQuery.Where(v => v.ConstituencyId == cId);
        if (FilterBooth.HasValue) voterQuery = voterQuery.Where(v => v.BoothNumber == FilterBooth.Value);
        if (!string.IsNullOrEmpty(FilterWard)) voterQuery = voterQuery.Where(v => v.WardNumber == FilterWard);

        var voterIds = await voterQuery.Select(v => v.Id).ToListAsync();

        var profiles = await _db.VoterProfiles
            .Where(p => voterIds.Contains(p.VoterId))
            .AsNoTracking().ToListAsync();

        var profileMap = profiles.ToDictionary(p => p.VoterId);

        var voters = await voterQuery
            .Where(v => voterIds.Contains(v.Id))
            .OrderBy(v => v.BoothNumber).ThenBy(v => v.Name)
            .Select(v => new { v.Id, v.Name, v.VoterId, v.MobileNumber, v.BoothNumber, v.WardNumber })
            .ToListAsync();

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Name,EPIC No.,Mobile,Booth,Ward,Age Bracket,Caste,Religion,Education,Occupation,Preferred Language,Top Concerns");
        foreach (var v in voters)
        {
            profileMap.TryGetValue(v.Id, out var p);
            var concerns = string.Empty;
            if (p != null && !string.IsNullOrEmpty(p.PrimaryConcerns))
            {
                var list = JsonSerializer.Deserialize<List<string>>(p.PrimaryConcerns!) ?? new();
                concerns = string.Join(" | ", list);
            }
            sb.AppendLine(string.Join(",",
                $"\"{v.Name.Replace("\"", "\"\"")}\"",
                v.VoterId,
                v.MobileNumber ?? "",
                v.BoothNumber.ToString(),
                v.WardNumber ?? "",
                p?.AgeBracket ?? "",
                p?.CasteCategory ?? "",
                p?.Religion ?? "",
                p?.Education ?? "",
                p?.Occupation ?? "",
                p?.PreferredLanguage ?? "",
                $"\"{concerns.Replace("\"", "\"\"")}\""));
        }

        var bytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
        return File(bytes, "text/csv", $"voter_survey_profiles_{DateTime.Today:yyyyMMdd}.csv");
    }

    public async Task<IActionResult> OnGetExportPendingCsvAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        var cId  = user?.ConstituencyId;

        IQueryable<Voter> voterQuery = _db.Voters.Where(v => !v.IsDeleted);
        if (cId.HasValue) voterQuery = voterQuery.Where(v => v.ConstituencyId == cId);
        if (FilterBooth.HasValue) voterQuery = voterQuery.Where(v => v.BoothNumber == FilterBooth.Value);
        if (!string.IsNullOrEmpty(FilterWard)) voterQuery = voterQuery.Where(v => v.WardNumber == FilterWard);

        var voterIds = await voterQuery.Select(v => v.Id).ToListAsync();
        var completedIds = (await _db.SurveyCompletions
            .Where(c => voterIds.Contains(c.VoterId))
            .Select(c => c.VoterId)
            .ToListAsync()).ToHashSet();

        var pending = await voterQuery
            .Where(v => !completedIds.Contains(v.Id))
            .OrderBy(v => v.BoothNumber).ThenBy(v => v.Name)
            .Select(v => new { v.Name, v.VoterId, v.MobileNumber, v.BoothNumber, v.WardNumber })
            .ToListAsync();

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Name,EPIC No.,Mobile,Booth,Ward");
        foreach (var v in pending)
        {
            sb.AppendLine(string.Join(",",
                $"\"{v.Name.Replace("\"", "\"\"")}\"",
                v.VoterId,
                v.MobileNumber ?? "",
                v.BoothNumber.ToString(),
                v.WardNumber ?? ""));
        }

        var bytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
        return File(bytes, "text/csv", $"pending_survey_{DateTime.Today:yyyyMMdd}.csv");
    }
}
