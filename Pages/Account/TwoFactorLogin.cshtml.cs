using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using Nirvachak_AI.Domain.Entities;
using Nirvachak_AI.Infrastructure.Services;

namespace Nirvachak_AI.Pages.Account;

public class TwoFactorLoginModel : PageModel
{
    private readonly SignInManager<AppUser> _signInManager;
    private readonly UserManager<AppUser>  _userManager;
    private readonly AuditService          _audit;

    public TwoFactorLoginModel(SignInManager<AppUser> signInManager,
        UserManager<AppUser> userManager, AuditService audit)
    {
        _signInManager = signInManager;
        _userManager   = userManager;
        _audit         = audit;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ReturnUrl  { get; set; }
    public string? ErrorMessage { get; set; }

    public class InputModel
    {
        [Required]
        [StringLength(7, MinimumLength = 6)]
        [Display(Name = "Authenticator Code")]
        public string TwoFactorCode { get; set; } = string.Empty;

        [Display(Name = "Remember this machine")]
        public bool RememberMachine { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(string? returnUrl = null)
    {
        // Ensure the user has gone through the login step
        var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
        if (user == null)
            return RedirectToPage("/Account/Login");

        ReturnUrl = returnUrl;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        if (!ModelState.IsValid) return Page();

        var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
        if (user == null)
            return RedirectToPage("/Account/Login");

        var code = Input.TwoFactorCode.Replace(" ", string.Empty).Replace("-", string.Empty);
        var result = await _signInManager.TwoFactorAuthenticatorSignInAsync(
            code, isPersistent: false, rememberClient: Input.RememberMachine);

        if (result.Succeeded)
        {
            await _audit.LogAsync(user.Id, user.FullName, "Login2FA", "Session",
                details: $"2FA login from {HttpContext.Connection.RemoteIpAddress}",
                constituencyId: user.ConstituencyId);
            return LocalRedirect(returnUrl ?? "/Dashboard/Index");
        }

        if (result.IsLockedOut)
        {
            ErrorMessage = "Account locked. Too many failed attempts.";
            return Page();
        }

        await _audit.LogAsync(user.Id, user.FullName, "Login2FAFailed", "Session",
            details: "Invalid 2FA code entered");

        ErrorMessage = "Invalid authenticator code.";
        return Page();
    }
}
