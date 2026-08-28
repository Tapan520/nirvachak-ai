using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Nirvachak_AI.Domain.Entities;
using Nirvachak_AI.Infrastructure.Data;
using System.Text.Json;

namespace Nirvachak_AI.Pages.VoterSurvey;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;

    public IndexModel(AppDbContext db) => _db = db;

    // ?? Step tracking ?????????????????????????????????????????????
    public string Step { get; set; } = "lookup";

    // ?? Step 1: EPIC lookup ???????????????????????????????????????
    [BindProperty]
    public string? EpicNumber { get; set; }

    // Pre-fill from URL: /Survey?epic=MH010012  (QR code / WhatsApp link)
    [BindProperty(SupportsGet = true)]
    public string? Epic { get; set; }

    // ?? Step 2: Voter identity reference ?????????????????????????
    public Voter? FoundVoter { get; set; }

    [BindProperty] public int? VoterDbId { get; set; }
    [BindProperty] public string? AgeBracket { get; set; }
    [BindProperty] public string? CasteCategory { get; set; }
    [BindProperty] public string? Religion { get; set; }
    [BindProperty] public string? Education { get; set; }
    [BindProperty] public string? Occupation { get; set; }
    [BindProperty] public string? MonthlyIncomeBracket { get; set; }
    [BindProperty] public List<string> PrimaryConcerns { get; set; } = new();
    [BindProperty] public string? PreferredLanguage { get; set; }

    // ?? Consent ???????????????????????????????????????????????????
    // Mandatory — voter must accept this to receive their coupon
    [BindProperty] public bool ConsentThirdPartyAdvertising { get; set; }
    // Optional campaign consents
    [BindProperty] public bool ConsentCampaignOutreach { get; set; }
    [BindProperty] public bool ConsentWhatsApp { get; set; }
    [BindProperty] public bool ConsentSchemeNotifications { get; set; }
    [BindProperty] public bool ConsentAnalytics { get; set; }

    // ?? Candidate & Party Preference ?????????????????????????????????????
    [BindProperty] public int? PreferredCandidateId { get; set; }
    [BindProperty] public int? PreferredPartyId { get; set; }
    public List<SurveyCandidate> Candidates { get; set; } = new();
    public List<SurveyParty> Parties { get; set; } = new();

    // ?? Static option lists ???????????????????????????????????????
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

    public void OnGet()
    {
        Step = string.IsNullOrEmpty(Epic) ? "lookup" : "prelookup";
        if (!string.IsNullOrEmpty(Epic))
            EpicNumber = Epic.Trim().ToUpper();
    }

    // ?? Step 1: Look up voter by EPIC ?????????????????????????????
    public async Task<IActionResult> OnPostLookupAsync()
    {
        if (string.IsNullOrWhiteSpace(EpicNumber))
        {
            ModelState.AddModelError(nameof(EpicNumber), "Please enter your EPIC / Voter ID number.");
            Step = "lookup";
            return Page();
        }

        // Basic rate-limit: max 10 lookups per IP per minute (via session counter)
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var lookupKey = $"survey_lookup_{ip}";
        if (HttpContext.Session.TryGetValue(lookupKey, out var raw))
        {
            var attempts = BitConverter.ToInt32(raw);
            if (attempts >= 10)
            {
                ModelState.AddModelError(nameof(EpicNumber), "Too many attempts. Please try again later.");
                Step = "lookup";
                return Page();
            }
            HttpContext.Session.Set(lookupKey, BitConverter.GetBytes(attempts + 1));
        }
        else
        {
            HttpContext.Session.Set(lookupKey, BitConverter.GetBytes(1));
        }

        var voter = await _db.Voters
            .FirstOrDefaultAsync(v => v.VoterId == EpicNumber.Trim().ToUpper() && !v.IsDeleted);

        if (voter is null)
        {
            ModelState.AddModelError(nameof(EpicNumber), "EPIC number not found. Please check and try again.");
            Step = "lookup";
            return Page();
        }

        if (await _db.SurveyCompletions.AnyAsync(s => s.VoterId == voter.Id))
        {
            FoundVoter = voter;
            Step = "already_done";
            return Page();
        }

        // Pre-fill profile if a partial save exists
        var existing = await _db.VoterProfiles.FirstOrDefaultAsync(p => p.VoterId == voter.Id);
        if (existing is not null)
        {
            AgeBracket           = existing.AgeBracket;
            CasteCategory        = existing.CasteCategory;
            Religion             = existing.Religion;
            Education            = existing.Education;
            Occupation           = existing.Occupation;
            MonthlyIncomeBracket = existing.MonthlyIncomeBracket;
            PreferredLanguage    = existing.PreferredLanguage;
            if (!string.IsNullOrEmpty(existing.PrimaryConcerns))
                PrimaryConcerns = JsonSerializer.Deserialize<List<string>>(existing.PrimaryConcerns) ?? new();
        }

        FoundVoter = voter;
        VoterDbId  = voter.Id;
        Step       = "form";

        // Load active candidates & parties for this constituency
        Candidates = await _db.SurveyCandidates
            .Where(c => c.ConstituencyId == voter.ConstituencyId && c.IsActive)
            .OrderBy(c => c.Name).ToListAsync();
        Parties = await _db.SurveyParties
            .Where(p => p.ConstituencyId == voter.ConstituencyId && p.IsActive)
            .OrderBy(p => p.Name).ToListAsync();

        return Page();
    }

    // ?? Step 2: Submit profile + consent, issue coupon ????????????
    public async Task<IActionResult> OnPostSubmitAsync()
    {
        if (VoterDbId is null) return RedirectToPage();

        var voter = await _db.Voters.FindAsync(VoterDbId.Value);
        if (voter is null) return RedirectToPage();

        // Guard against double-submission
        if (await _db.SurveyCompletions.AnyAsync(s => s.VoterId == voter.Id))
            return RedirectToPage("/Survey/Complete", new { alreadyDone = true });

        // Require mandatory third-party advertising consent before issuing coupon
        if (!ConsentThirdPartyAdvertising)
        {
            FoundVoter = voter;
            Candidates = await _db.SurveyCandidates
                .Where(c => c.ConstituencyId == voter.ConstituencyId && c.IsActive)
                .OrderBy(c => c.Name).ToListAsync();
            Parties = await _db.SurveyParties
                .Where(p => p.ConstituencyId == voter.ConstituencyId && p.IsActive)
                .OrderBy(p => p.Name).ToListAsync();
            ModelState.AddModelError("ConsentRequired",
                "You must accept the Third-Party Data & Advertisement consent to claim your reward coupon.");
            Step = "form";
            return Page();
        }

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

        // ?? Upsert VoterProfile ???????????????????????????????????
        var profile = await _db.VoterProfiles.FirstOrDefaultAsync(p => p.VoterId == voter.Id);
        if (profile is null)
        {
            profile = new VoterProfile { VoterId = voter.Id };
            _db.VoterProfiles.Add(profile);
        }
        profile.AgeBracket           = AgeBracket;
        profile.CasteCategory        = CasteCategory;
        profile.Religion             = Religion;
        profile.Education            = Education;
        profile.Occupation           = Occupation;
        profile.MonthlyIncomeBracket = MonthlyIncomeBracket;
        profile.PrimaryConcerns      = PrimaryConcerns.Count > 0
            ? JsonSerializer.Serialize(PrimaryConcerns.Take(3).ToList())
            : null;
        profile.PreferredLanguage    = PreferredLanguage;
        profile.CompletedAt          = DateTime.UtcNow;
        profile.IpAddress            = ip;
        profile.PreferredCandidateId = PreferredCandidateId;
        profile.PreferredPartyId     = PreferredPartyId;

        // ?? Upsert VoterConsent ???????????????????????????????????
        var consent = await _db.VoterConsents.FirstOrDefaultAsync(c => c.VoterId == voter.Id);
        if (consent is null)
        {
            consent = new VoterConsent { VoterId = voter.Id };
            _db.VoterConsents.Add(consent);
        }
        consent.AllowThirdPartyAdvertising = ConsentThirdPartyAdvertising;
        consent.AllowCampaignOutreach    = ConsentCampaignOutreach;
        consent.AllowWhatsAppMessages    = ConsentWhatsApp;
        consent.AllowSchemeNotifications = ConsentSchemeNotifications;
        consent.AllowDataForAnalytics    = ConsentAnalytics;
        consent.ConsentGivenAt           = DateTime.UtcNow;
        consent.IpAddress                = ip;

        // ?? Issue reward coupon (first available in constituency) ?
        var reward = await _db.RewardConfigs
            .Where(r => r.IsActive
                     && r.ConstituencyId == voter.ConstituencyId
                     && r.ExpiryDate > DateTime.UtcNow)
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync();

        CouponPool? coupon = null;
        if (reward is not null)
        {
            coupon = await _db.CouponPools
                .Where(c => c.RewardConfigId == reward.Id && !c.IsIssued)
                .FirstOrDefaultAsync();

            if (coupon is not null)
            {
                coupon.IsIssued        = true;
                coupon.IssuedToVoterId = voter.Id;
                coupon.IssuedAt        = DateTime.UtcNow;
            }
        }

        // ?? Record completion ?????????????????????????????????????
        _db.SurveyCompletions.Add(new SurveyCompletion
        {
            VoterId        = voter.Id,
            ConstituencyId = voter.ConstituencyId,
            CouponId       = coupon?.Id,
            IpAddress      = ip
        });

        await _db.SaveChangesAsync();

        return RedirectToPage("/Survey/Complete", new
        {
            couponCode  = coupon?.CouponCode,
            voterName   = voter.Name,
            rewardTitle = reward?.Title,
            brand       = reward?.PartnerBrand,
            expiry      = reward?.ExpiryDate.ToString("dd MMM yyyy")
        });
    }
}
