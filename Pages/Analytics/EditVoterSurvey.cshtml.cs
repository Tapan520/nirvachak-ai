using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Nirvachak_AI.Domain.Entities;
using Nirvachak_AI.Infrastructure.Data;
using System.Text.Json;

namespace Nirvachak_AI.Pages.Analytics;

[Authorize(Roles = "Admin,CampaignManager,Candidate,FieldWorker,BoothAgent")]
public class EditVoterSurveyModel : PageModel
{
    private readonly AppDbContext        _db;
    private readonly UserManager<AppUser> _userManager;

    public EditVoterSurveyModel(AppDbContext db, UserManager<AppUser> userManager)
    {
        _db          = db;
        _userManager = userManager;
    }

    // ?? Route / return params ?????????????????????????????????????
    [BindProperty(SupportsGet = true)] public int     VoterId      { get; set; }
    [BindProperty(SupportsGet = true)] public int?    ReturnBooth  { get; set; }
    [BindProperty(SupportsGet = true)] public string? ReturnWard   { get; set; }

    // ?? Display ???????????????????????????????????????????????????
    public Voter?    FoundVoter    { get; set; }
    public DateTime? LastUpdatedAt { get; set; }

    // ?? Form fields ???????????????????????????????????????????????
    [BindProperty] public string?       MobileNumber         { get; set; }
    [BindProperty] public string?       AgeBracket           { get; set; }
    [BindProperty] public string?       CasteCategory        { get; set; }
    [BindProperty] public string?       Religion             { get; set; }
    [BindProperty] public string?       Education            { get; set; }
    [BindProperty] public string?       Occupation           { get; set; }
    [BindProperty] public string?       MonthlyIncomeBracket { get; set; }
    [BindProperty] public string?       PreferredLanguage    { get; set; }
    [BindProperty] public List<string>  PrimaryConcerns      { get; set; } = new();

    // Consents
    [BindProperty] public bool ConsentThirdPartyAdvertising { get; set; }
    [BindProperty] public bool ConsentCampaignOutreach      { get; set; }
    [BindProperty] public bool ConsentWhatsApp              { get; set; }
    [BindProperty] public bool ConsentSchemeNotifications   { get; set; }
    [BindProperty] public bool ConsentAnalytics             { get; set; }

    // ?? Static option lists (same as Survey/Index.cshtml.cs) ?????
    public static readonly string[] AgeBrackets     = { "18–25", "26–35", "36–50", "51–65", "65+" };
    public static readonly string[] CasteCategories = { "General", "OBC", "SC", "ST", "NT" };
    public static readonly string[] Religions       = { "Hindu", "Muslim", "Christian", "Sikh", "Buddhist", "Jain", "Other" };
    public static readonly string[] Educations      = { "Below 10th", "10th", "12th", "Graduate", "PG+" };
    public static readonly string[] Occupations     = { "Farmer", "Service", "Business", "Student", "Homemaker", "Other" };
    public static readonly string[] IncomeBrackets  = { "<10K", "10-25K", "25-50K", "50K+" };
    public static readonly string[] Languages       = { "Marathi", "Hindi", "English", "Urdu", "Other" };
    public static readonly string[] IssueList =
    {
        "Roads & Infrastructure", "Water Supply", "Employment", "Education",
        "Healthcare", "Electricity", "Agriculture / MSP", "Women Safety",
        "GST / Business", "Housing / Ration", "Law & Order", "Youth Development"
    };

    // ?? GET: load existing profile + consents ????????????????????
    public async Task<IActionResult> OnGetAsync()
    {
        var voter = await _db.Voters.FindAsync(VoterId);
        if (voter is null || voter.IsDeleted) return NotFound();
        FoundVoter    = voter;
        MobileNumber  = voter.MobileNumber;

        var profile = await _db.VoterProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.VoterId == VoterId);

        var consent = await _db.VoterConsents
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.VoterId == VoterId);

        if (profile is not null)
        {
            AgeBracket           = profile.AgeBracket;
            CasteCategory        = profile.CasteCategory;
            Religion             = profile.Religion;
            Education            = profile.Education;
            Occupation           = profile.Occupation;
            MonthlyIncomeBracket = profile.MonthlyIncomeBracket;
            PreferredLanguage    = profile.PreferredLanguage;
            LastUpdatedAt        = profile.CompletedAt;

            if (!string.IsNullOrEmpty(profile.PrimaryConcerns))
            {
                try { PrimaryConcerns = JsonSerializer.Deserialize<List<string>>(profile.PrimaryConcerns) ?? new(); }
                catch { /* ignore */ }
            }
        }

        if (consent is not null)
        {
            ConsentThirdPartyAdvertising = consent.AllowThirdPartyAdvertising;
            ConsentCampaignOutreach      = consent.AllowCampaignOutreach;
            ConsentWhatsApp              = consent.AllowWhatsAppMessages;
            ConsentSchemeNotifications   = consent.AllowSchemeNotifications;
            ConsentAnalytics             = consent.AllowDataForAnalytics;
        }

        return Page();
    }

    // ?? POST: save updated profile + consents ????????????????????
    public async Task<IActionResult> OnPostAsync()
    {
        var voter = await _db.Voters.FindAsync(VoterId);
        if (voter is null || voter.IsDeleted) return NotFound();
        FoundVoter = voter;

        // ?? Update mobile number on the Voter record ??????????????
        voter.MobileNumber = string.IsNullOrWhiteSpace(MobileNumber)
            ? null
            : MobileNumber.Trim();

        // ?? Upsert VoterProfile ???????????????????????????????????
        var profile = await _db.VoterProfiles.FirstOrDefaultAsync(p => p.VoterId == VoterId);
        if (profile is null)
        {
            profile = new VoterProfile { VoterId = VoterId };
            _db.VoterProfiles.Add(profile);
        }
        profile.AgeBracket           = AgeBracket;
        profile.CasteCategory        = CasteCategory;
        profile.Religion             = Religion;
        profile.Education            = Education;
        profile.Occupation           = Occupation;
        profile.MonthlyIncomeBracket = MonthlyIncomeBracket;
        profile.PreferredLanguage    = PreferredLanguage;
        profile.PrimaryConcerns      = PrimaryConcerns.Count > 0
            ? JsonSerializer.Serialize(PrimaryConcerns.Take(3).ToList())
            : null;
        profile.CompletedAt          = DateTime.UtcNow;

        // ?? Upsert VoterConsent ???????????????????????????????????
        var consent = await _db.VoterConsents.FirstOrDefaultAsync(c => c.VoterId == VoterId);
        if (consent is null)
        {
            consent = new VoterConsent { VoterId = VoterId };
            _db.VoterConsents.Add(consent);
        }
        consent.AllowThirdPartyAdvertising = ConsentThirdPartyAdvertising;
        consent.AllowCampaignOutreach      = ConsentCampaignOutreach;
        consent.AllowWhatsAppMessages      = ConsentWhatsApp;
        consent.AllowSchemeNotifications   = ConsentSchemeNotifications;
        consent.AllowDataForAnalytics      = ConsentAnalytics;
        consent.ConsentGivenAt             = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        TempData["Message"] = $"Survey profile for {voter.Name} updated successfully.";

        // Return to the Completed tab with original filters
        return RedirectToPage("/Analytics/SurveyDemographics", new
        {
            FilterTab   = "completed",
            FilterBooth = ReturnBooth,
            FilterWard  = ReturnWard
        });
    }
}
