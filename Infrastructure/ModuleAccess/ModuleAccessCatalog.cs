namespace Nirvachak_AI.Infrastructure.ModuleAccess;

public sealed record ModuleGroup(string Key, string Name, IReadOnlyList<SubModuleItem> SubModules);
public sealed record SubModuleItem(
    string Key,
    string Name,
    string[] PagePrefixes,
    string[] ApiPrefixes);

public static class ModuleAccessCatalog
{
    public static readonly IReadOnlyList<ModuleGroup> Modules = new List<ModuleGroup>
    {
        new("Voters", "Voters", new List<SubModuleItem>
        {
            new("VoterList", "Voter List", ["/Voters"], ["/api/voters"]),
            new("AddVoter", "Add Voter", ["/Voters/Create"], Array.Empty<string>()),
            new("ImportVoters", "Import Voters", ["/Voters/Import"], Array.Empty<string>()),
            new("VoterSlips", "Voter Slips", ["/VoterSlips"], ["/api/voterslips"]),
        }),
        new("Campaign", "Campaign", new List<SubModuleItem>
        {
            new("PhoneBanking", "Phone Banking", ["/PhoneBanking"], ["/api/phonebanking"]),
            new("InfluencerNetwork", "Influencer Network", ["/Influencers"], ["/api/influencers"]),
            new("CompetitorIntel", "Competitor Intel", ["/Competitor"], ["/api/competitor"]),
            new("CampaignEvents", "Campaign Events", ["/Campaign"], ["/api/campaign"]),
            new("Volunteers", "Volunteers", ["/Volunteers"], ["/api/volunteers"]),
            new("Leaderboard", "Leaderboard", ["/Leaderboard"], Array.Empty<string>()),
        }),
        new("Operations", "Operations", new List<SubModuleItem>
        {
            new("Dashboard", "Dashboard", ["/Dashboard"], ["/api/dashboard"]),
            new("Announcements", "Announcements", ["/Announcements"], ["/api/announcements"]),
            new("ElectionDay", "Election Day", ["/ElectionDay"], ["/api/electionday"]),
            new("BoothChecklist", "Booth Checklist", ["/ElectionDay/Checklist"], Array.Empty<string>()),
            new("Analytics", "Analytics", ["/Analytics"], ["/api/analytics"]),
            new("PredictiveAnalytics", "Predictive Analytics", ["/Predictions"], ["/api/predictiveanalytics", "/api/predictions"]),
            new("WinProbability", "Win Probability", ["/WinProbability"], ["/api/winprobability"]),
            new("SwingVoters", "Swing Voters", ["/SwingVoters"], Array.Empty<string>()),
            new("BoothHeatMap", "Booth Heat Map", ["/BoothHeatMap"], Array.Empty<string>()),
            new("Grievances", "Grievances", ["/Grievances"], ["/api/grievances"]),
            new("Expenses", "Expenses", ["/Expenses"], ["/api/expenses"]),
            new("Surveys", "Surveys & Feedback", ["/Surveys"], ["/api/surveys"]),
            new("VoterConsentAnalytics", "Voter Consent Analytics", ["/Analytics/SurveyDemographics", "/Analytics/EditVoterSurvey"], ["/api/voterconsent"]),
            new("PreferenceAnalytics", "Preference Analytics", ["/Analytics/PreferenceAnalytics"], Array.Empty<string>()),
            new("VoterTransport", "Voter Transport", ["/Transport"], ["/api/transport"]),
            new("ExportReports", "Export Reports", ["/Reports"], ["/api/reports"]),
        }),
        new("ElectionOps", "Election Ops", new List<SubModuleItem>
        {
            new("PannaPramukh", "Panna Pramukh", ["/PannaPramukh"], ["/api/pannapramukh"]),
            new("BoothShifts", "Booth Shift Planner", ["/BoothShifts"], ["/api/boothshifts"]),
            new("RapidResponse", "Rapid Response", ["/RapidResponse"], ["/api/rapidresponse"]),
            new("Broadcast", "Broadcast", ["/Broadcast"], ["/api/broadcast"]),
            new("BudgetPlanner", "Budget Planner", ["/Budget"], ["/api/budget"]),
            new("Results", "Result Day", ["/Results"], ["/api/results"]),
            new("FieldReports", "Field Reports", ["/FieldReports"], ["/api/fieldreports"]),
        }),
        new("Management", "Management", new List<SubModuleItem>
        {
            new("Wards", "Wards", ["/Admin/Wards"], Array.Empty<string>()),
            new("Booths", "Booths", ["/Booths"], ["/api/booths"]),
            new("Users", "Users", ["/Admin/Index", "/Admin/CreateUser", "/Admin/EditUser", "/Admin/DeleteUser"], ["/api/admin"]),
            new("Rewards", "Rewards", ["/Admin/Rewards"], Array.Empty<string>()),
            new("CandidatesParties", "Candidates & Parties", ["/Admin/CandidatesParties"], Array.Empty<string>()),
            new("Exotel", "Exotel", ["/Admin/Exotel"], ["/api/exotel"]),
        })
    };

    public static IReadOnlyList<SubModuleItem> AllSubModules => Modules.SelectMany(m => m.SubModules).ToList();

    public static string? FindSubModuleKeyByPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;

        var all = AllSubModules
            .OrderByDescending(s => s.PagePrefixes.Max(p => p.Length))
            .ToList();

        foreach (var sub in all)
        {
            if (sub.PagePrefixes.Any(prefix => path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
                return sub.Key;

            if (sub.ApiPrefixes.Any(prefix => path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
                return sub.Key;
        }

        return null;
    }
}
