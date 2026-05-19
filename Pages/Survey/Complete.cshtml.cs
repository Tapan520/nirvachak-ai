using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Nirvachak_AI.Pages.VoterSurvey;

public class CompleteModel : PageModel
{
    public string? CouponCode { get; set; }
    public string? VoterName { get; set; }
    public string? RewardTitle { get; set; }
    public string? Brand { get; set; }
    public string? Expiry { get; set; }
    public bool AlreadyDone { get; set; }
    public bool HasCoupon => !string.IsNullOrEmpty(CouponCode);

    public void OnGet(string? couponCode, string? voterName, string? rewardTitle,
                      string? brand, string? expiry, bool alreadyDone = false)
    {
        CouponCode  = couponCode;
        VoterName   = voterName;
        RewardTitle = rewardTitle;
        Brand       = brand;
        Expiry      = expiry;
        AlreadyDone = alreadyDone;
    }
}
