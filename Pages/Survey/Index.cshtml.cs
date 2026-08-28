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

    // ?? Step tracking ?????????????????????????????????????????
    public string Step { get; set; } = "lookup";

    // ?? Step 1: EPIC lookup ???????????????????????????????????
    [BindProperty] public string? EpicNumber { get; set; }
    [BindProperty(SupportsGet = true)] public string? Epic { get; set; }

    // ?? Step 2: Voter identity reference ?????????????????????
    public Voter? FoundVoter { get; set; }
    [BindProperty] public int? VoterDbId { get; set; }

    // ?? Self-Registration fields ??????????????????????????????
    [BindProperty] public string? RegName            { get; set; }
    [BindProperty] public int     RegAge             { get; set; }
    [BindProperty] public string? RegGender          { get; set; }
    [BindProperty] public string? RegMobile          { get; set; }
    [BindProperty] public string? RegAddress         { get; set; }
    [BindProperty] public string? RegFatherHusband   { get; set; }
    [BindProperty] public int     RegConstituencyId  { get; set; }
    [BindProperty] public string? RegWardNumber      { get; set; }
    [BindProperty] public int     RegBoothNumber     { get; set; }
    [BindProperty] public string? RegEpic            { get; set; }

    // Lists for cascading dropdowns
    public List<Constituency> Constituencies { get; set; } = new();
    public List<Ward>         RegWards       { get; set; } = new();
    public List<Booth>        RegBooths      { get; set; } = new();

    // ?? Survey profile fields ?????????????????????????????????
    [BindProperty] public string? AgeBracket          { get; set; }
    [BindProperty] public string? CasteCategory       { get; set; }
    [BindProperty] public string? Religion            { get; set; }
    [BindProperty] public string? Education           { get; set; }
    [BindProperty] public string? Occupation          { get; set; }
    [BindProperty] public string? MonthlyIncomeBracket{ get; set; }
    [BindProperty] public List<string> PrimaryConcerns{ get; set; } = new();
    [BindProperty] public string? PreferredLanguage   { get; set; }

    // ?? Consent ???????????????????????????????????????????????
    [BindProperty] public bool ConsentThirdPartyAdvertising { get; set; }
    [BindProperty] public bool ConsentCampaignOutreach      { get; set; }
    [BindProperty] public bool ConsentWhatsApp              { get; set; }
    [BindProperty] public bool ConsentSchemeNotifications   { get; set; }
    [BindProperty] public bool ConsentAnalytics             { get; set; }

    // ?? Candidate & Party Preference ?????????????????????????
    [BindProperty] public int? PreferredCandidateId { get; set; }
    [BindProperty] public int? PreferredPartyId     { get; set; }
    public List<SurveyCandidate> Candidates { get; set; } = new();
    public List<SurveyParty>     Parties    { get; set; } = new();

    // ?? Static option lists ???????????????????????????????????
    public static readonly string[] AgeBrackets     = { "18-25", "26-35", "36-50", "51-65", "65+" };
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

    public async Task OnGetAsync()
    {
        Step = string.IsNullOrEmpty(Epic) ? "lookup" : "prelookup";
        if (!string.IsNullOrEmpty(Epic))
            EpicNumber = Epic.Trim().ToUpper();
        Constituencies = await _db.Constituencies.OrderBy(c => c.Name).ToListAsync();
    }

    // ?? Step 1: Look up voter by EPIC ?????????????????????????
    public async Task<IActionResult> OnPostLookupAsync()
    {
        Constituencies = await _db.Constituencies.OrderBy(c => c.Name).ToListAsync();

        if (string.IsNullOrWhiteSpace(EpicNumber))
        {
            ModelState.AddModelError(nameof(EpicNumber), "Please enter your EPIC / Voter ID number.");
            Step = "lookup";
            return Page();
        }

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
            // Not found — offer self-registration
            RegEpic = EpicNumber.Trim().ToUpper();
            Step    = "register";
            return Page();
        }

        if (await _db.SurveyCompletions.AnyAsync(s => s.VoterId == voter.Id))
        {
            FoundVoter = voter;
            Step = "already_done";
            return Page();
        }

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

        Candidates = await _db.SurveyCandidates
            .Where(c => c.ConstituencyId == voter.ConstituencyId && c.IsActive)
            .OrderBy(c => c.Name).ToListAsync();
        Parties = await _db.SurveyParties
            .Where(p => p.ConstituencyId == voter.ConstituencyId && p.IsActive)
            .OrderBy(p => p.Name).ToListAsync();

        return Page();
    }

    // ?? AJAX: Get wards for a constituency ???????????????????
    public async Task<IActionResult> OnGetWardsAsync(int constituencyId)
    {
        var wards = await _db.Wards
            .Where(w => w.ConstituencyId == constituencyId)
            .OrderBy(w => w.WardNumber)
            .Select(w => new { w.WardNumber, w.WardName })
            .ToListAsync();
        return new JsonResult(wards);
    }

    // ?? AJAX: Get booths for a constituency + ward ????????????
    public async Task<IActionResult> OnGetBoothsAsync(int constituencyId, string wardNumber)
    {
        var booths = await _db.Booths
            .Where(b => b.ConstituencyId == constituencyId && b.WardNumber == wardNumber)
            .OrderBy(b => b.BoothNumber)
            .Select(b => new { b.BoothNumber, b.BoothName })
            .ToListAsync();
        return new JsonResult(booths);
    }

    // ?? Self-Registration: create voter then show survey form ?
    public async Task<IActionResult> OnPostRegisterAsync()
    {
        Constituencies = await _db.Constituencies.OrderBy(c => c.Name).ToListAsync();

        if (string.IsNullOrWhiteSpace(RegName) || RegAge < 18 || string.IsNullOrWhiteSpace(RegGender)
            || RegConstituencyId == 0 || RegBoothNumber == 0)
        {
            ModelState.AddModelError(string.Empty, "Please fill all required fields.");
            Step = "register";
            return Page();
        }

        // Duplicate mobile check within constituency
        if (!string.IsNullOrWhiteSpace(RegMobile))
        {
            var dupMobile = await _db.Voters.AnyAsync(v =>
                v.MobileNumber == RegMobile.Trim() &&
                v.ConstituencyId == RegConstituencyId &&
                !v.IsDeleted);
            if (dupMobile)
            {
                ModelState.AddModelError(nameof(RegMobile),
                    "A voter with this mobile number is already registered in the selected constituency.");
                Step = "register";
                return Page();
            }
        }

        // Generate a unique self-reg EPIC if not provided
        var epic = string.IsNullOrWhiteSpace(RegEpic)
            ? "SELF" + DateTime.UtcNow.Ticks.ToString()[^8..]
            : RegEpic.Trim().ToUpper();

        // Get next serial number for this booth
        var maxSerial = await _db.Voters
            .Where(v => v.BoothNumber == RegBoothNumber && v.ConstituencyId == RegConstituencyId && !v.IsDeleted)
            .MaxAsync(v => (int?)v.SerialNumber) ?? 0;

        var voter = new Voter
        {
            VoterId          = epic,
            Name             = RegName.Trim().ToUpper(),
            FatherHusbandName= RegFatherHusband?.Trim().ToUpper(),
            Age              = RegAge,
            Gender           = RegGender,
            MobileNumber     = RegMobile?.Trim(),
            Address          = RegAddress?.Trim() ?? string.Empty,
            BoothNumber      = RegBoothNumber,
            WardNumber       = RegWardNumber,
            ConstituencyId   = RegConstituencyId,
            SerialNumber     = maxSerial + 1,
            Sentiment        = Nirvachak_AI.Domain.Enums.VoterSentiment.Unknown,
            IsSelfRegistered = true,
            ImportedAt       = DateTime.UtcNow
        };

        _db.Voters.Add(voter);
        await _db.SaveChangesAsync();

        FoundVoter = voter;
        VoterDbId  = voter.Id;
        Step       = "form";

        Candidates = await _db.SurveyCandidates
            .Where(c => c.ConstituencyId == voter.ConstituencyId && c.IsActive)
            .OrderBy(c => c.Name).ToListAsync();
        Parties = await _db.SurveyParties
            .Where(p => p.ConstituencyId == voter.ConstituencyId && p.IsActive)
            .OrderBy(p => p.Name).ToListAsync();

        return Page();
    }

    // ?? Step 2: Submit profile + consent, issue coupon ????????
    public async Task<IActionResult> OnPostSubmitAsync()
    {
        if (VoterDbId is null) return RedirectToPage();

        var voter = await _db.Voters.FindAsync(VoterDbId.Value);
        if (voter is null) return RedirectToPage();

        if (await _db.SurveyCompletions.AnyAsync(s => s.VoterId == voter.Id))
            return RedirectToPage("/Survey/Complete", new { alreadyDone = true });

        if (!ConsentThirdPartyAdvertising)
        {
            FoundVoter = voter;
            Constituencies = await _db.Constituencies.OrderBy(c => c.Name).ToListAsync();
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

        var profile = await _db.VoterProfiles.FirstOrDefaultAsync(p => p.VoterId == voter.Id);
        if (profile is null)
        {
            profile = new VoterProfile { VoterId = voter.Id };
            _db.VoterProfiles.Add(profile);
        }
        profile.AgeBracket            = AgeBracket;
        profile.CasteCategory         = CasteCategory;
        profile.Religion              = Religion;
        profile.Education             = Education;
        profile.Occupation            = Occupation;
        profile.MonthlyIncomeBracket  = MonthlyIncomeBracket;
        profile.PrimaryConcerns       = PrimaryConcerns.Count > 0
            ? JsonSerializer.Serialize(PrimaryConcerns.Take(3).ToList()) : null;
        profile.PreferredLanguage     = PreferredLanguage;
        profile.CompletedAt           = DateTime.UtcNow;
        profile.IpAddress             = ip;
        profile.PreferredCandidateId  = PreferredCandidateId;
        profile.PreferredPartyId      = PreferredPartyId;

        var consent = await _db.VoterConsents.FirstOrDefaultAsync(c => c.VoterId == voter.Id);
        if (consent is null)
        {
            consent = new VoterConsent { VoterId = voter.Id };
            _db.VoterConsents.Add(consent);
        }
        consent.AllowThirdPartyAdvertising = ConsentThirdPartyAdvertising;
        consent.AllowCampaignOutreach      = ConsentCampaignOutreach;
        consent.AllowWhatsAppMessages      = ConsentWhatsApp;
        consent.AllowSchemeNotifications   = ConsentSchemeNotifications;
        consent.AllowDataForAnalytics      = ConsentAnalytics;
        consent.ConsentGivenAt             = DateTime.UtcNow;
        consent.IpAddress                  = ip;

        var reward = await _db.RewardConfigs
            .Where(r => r.IsActive && r.ConstituencyId == voter.ConstituencyId && r.ExpiryDate > DateTime.UtcNow)
            .OrderByDescending(r => r.CreatedAt).FirstOrDefaultAsync();

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

