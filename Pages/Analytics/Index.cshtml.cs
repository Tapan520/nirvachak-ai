using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Nirvachak_AI.Domain.Entities;
using Nirvachak_AI.Domain.Enums;
using Nirvachak_AI.Infrastructure.Data;

namespace Nirvachak_AI.Pages.Analytics;

public class BoothAnalyticsRow
{
    public int BoothNumber { get; set; }
    public int Total { get; set; }
    public int Favour { get; set; }
    public int Against { get; set; }
    public int Neutral { get; set; }
    public int Unknown { get; set; }
    public int Floating { get; set; }
}

public class DemographicSentimentRow
{
    public string Group    { get; set; } = "";
    public int    Favour   { get; set; }
    public int    Against  { get; set; }
    public int    Neutral  { get; set; }
    public int    Floating { get; set; }
    public int    Unknown  { get; set; }
    public int    Total    => Favour + Against + Neutral + Floating + Unknown;
}

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _userManager;

    public IndexModel(AppDbContext db, UserManager<AppUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    [BindProperty(SupportsGet = true)]
    public int? SelectedConstituencyId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SelectedWard { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? SelectedBoothNumber { get; set; }

    public List<Constituency> Constituencies { get; set; } = new();
    public Constituency? SelectedConstituency { get; set; }
    public List<Ward> Wards { get; set; } = new();
    public List<Booth> Booths { get; set; } = new();

    public List<string> SentimentLabels { get; set; } = new();
    public List<int> SentimentValues { get; set; } = new();
    public List<string> AgeLabels { get; set; } = new();
    public List<int> AgeValues { get; set; } = new();
    public int MaleVoters { get; set; }
    public int FemaleVoters { get; set; }
    public int OtherVoters { get; set; }
    public List<BoothAnalyticsRow> BoothAnalytics { get; set; } = new();
    public List<DemographicSentimentRow> ReligionSentimentMatrix { get; set; } = new();
    public List<DemographicSentimentRow> CasteSentimentMatrix    { get; set; } = new();

    public async Task OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        var isAdmin = user?.Role == UserRole.SuperAdmin;
        var isRestricted = user?.Role == UserRole.FieldWorker || user?.Role == UserRole.BoothAgent;

        if (isAdmin)
        {
            Constituencies = await _db.Constituencies.OrderBy(c => c.Name).ToListAsync();
        }

        int? cId = isAdmin ? SelectedConstituencyId : user?.ConstituencyId;
        if (cId.HasValue)
            SelectedConstituency = await _db.Constituencies.FindAsync(cId.Value);

        // Load wards for drill-down
        if (cId.HasValue)
            Wards = await _db.Wards.Where(w => w.ConstituencyId == cId.Value).OrderBy(w => w.WardNumber).ToListAsync();

        // Load booths for drill-down (filtered by ward if selected)
        if (cId.HasValue)
        {
            var boothQuery = _db.Booths.Where(b => b.ConstituencyId == cId.Value);
            if (!string.IsNullOrEmpty(SelectedWard))
                boothQuery = boothQuery.Where(b => b.WardNumber == SelectedWard);
            Booths = await boothQuery.OrderBy(b => b.BoothNumber).ToListAsync();
        }

        IQueryable<Voter> query = _db.Voters;
        if (cId.HasValue)
            query = query.Where(v => v.ConstituencyId == cId);

        if (isRestricted)
        {
            var assignedBooths = (user?.AssignedBoothNumbers ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => int.TryParse(s.Trim(), out var n) ? (int?)n : null)
                .Where(n => n.HasValue).Select(n => n!.Value).ToList();
            var assignedWard = user?.AssignedWard?.Trim();

            if (assignedBooths.Any())
                query = query.Where(v => assignedBooths.Contains(v.BoothNumber));
            else if (!string.IsNullOrEmpty(assignedWard))
                query = query.Where(v => v.WardNumber == assignedWard);
        }

        // Ward / Booth drill-down
        if (!string.IsNullOrEmpty(SelectedWard))
            query = query.Where(v => v.WardNumber == SelectedWard);
        if (SelectedBoothNumber.HasValue)
            query = query.Where(v => v.BoothNumber == SelectedBoothNumber.Value);

        // Sentiment
        var sentiments = await query.GroupBy(v => v.Sentiment)
            .Select(g => new { s = g.Key, c = g.Count() }).ToListAsync();
        foreach (VoterSentiment s in Enum.GetValues<VoterSentiment>())
        {
            SentimentLabels.Add(s.ToString());
            SentimentValues.Add(sentiments.FirstOrDefault(x => x.s == s)?.c ?? 0);
        }

        // Age groups
        var ageGroups = new[] { "18-25", "26-35", "36-45", "46-55", "56-65", "66+" };
        AgeLabels.AddRange(ageGroups);
        AgeValues.Add(await query.CountAsync(v => v.Age >= 18 && v.Age <= 25));
        AgeValues.Add(await query.CountAsync(v => v.Age >= 26 && v.Age <= 35));
        AgeValues.Add(await query.CountAsync(v => v.Age >= 36 && v.Age <= 45));
        AgeValues.Add(await query.CountAsync(v => v.Age >= 46 && v.Age <= 55));
        AgeValues.Add(await query.CountAsync(v => v.Age >= 56 && v.Age <= 65));
        AgeValues.Add(await query.CountAsync(v => v.Age >= 66));

        // Gender
        MaleVoters = await query.CountAsync(v => v.Gender == "M");
        FemaleVoters = await query.CountAsync(v => v.Gender == "F");
        OtherVoters = await query.CountAsync(v => v.Gender != "M" && v.Gender != "F");

        // Booth analytics
        var boothNums = await query.Select(v => v.BoothNumber).Distinct().OrderBy(n => n).ToListAsync();
        foreach (var bn in boothNums)
        {
            var bVoters = query.Where(v => v.BoothNumber == bn);
            BoothAnalytics.Add(new BoothAnalyticsRow
            {
                BoothNumber = bn,
                Total    = await bVoters.CountAsync(),
                Favour   = await bVoters.CountAsync(v => v.Sentiment == VoterSentiment.Favour),
                Against  = await bVoters.CountAsync(v => v.Sentiment == VoterSentiment.Against),
                Neutral  = await bVoters.CountAsync(v => v.Sentiment == VoterSentiment.Neutral),
                Unknown  = await bVoters.CountAsync(v => v.Sentiment == VoterSentiment.Unknown),
                Floating = await bVoters.CountAsync(v => v.Sentiment == VoterSentiment.Floating)
            });
        }

        // #14 – Demographic × Sentiment matrices
        var voterSentimentPairs = await query
            .Select(v => new { v.Id, v.Sentiment })
            .ToListAsync();
        if (voterSentimentPairs.Any())
        {
            var sentimentLookup = voterSentimentPairs.ToDictionary(v => v.Id, v => v.Sentiment);
            var allVoterIds = sentimentLookup.Keys.ToList();
            var vProfiles = await _db.VoterProfiles
                .Where(p => allVoterIds.Contains(p.VoterId))
                .AsNoTracking()
                .ToListAsync();
            ReligionSentimentMatrix = BuildSentimentMatrix(vProfiles, sentimentLookup, p => p.Religion);
            CasteSentimentMatrix    = BuildSentimentMatrix(vProfiles, sentimentLookup, p => p.CasteCategory);
        }
    }

    private static List<DemographicSentimentRow> BuildSentimentMatrix(
        List<VoterProfile> profiles,
        Dictionary<int, VoterSentiment> sentimentMap,
        Func<VoterProfile, string?> keySelector)
        => profiles
            .Where(p => !string.IsNullOrEmpty(keySelector(p)) && sentimentMap.ContainsKey(p.VoterId))
            .GroupBy(p => keySelector(p)!)
            .Select(g => new DemographicSentimentRow
            {
                Group    = g.Key,
                Favour   = g.Count(p => sentimentMap[p.VoterId] == VoterSentiment.Favour),
                Against  = g.Count(p => sentimentMap[p.VoterId] == VoterSentiment.Against),
                Neutral  = g.Count(p => sentimentMap[p.VoterId] == VoterSentiment.Neutral),
                Floating = g.Count(p => sentimentMap[p.VoterId] == VoterSentiment.Floating),
                Unknown  = g.Count(p => sentimentMap[p.VoterId] == VoterSentiment.Unknown),
            })
            .OrderByDescending(r => r.Total)
            .ToList();
}
