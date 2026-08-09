using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using Nirvachak_AI.Domain.Entities;
using Nirvachak_AI.Infrastructure.Services;

namespace Nirvachak_AI.Pages.Account;

public class ManageTwoFactorModel : PageModel
{
    private readonly UserManager<AppUser> _userManager;
    private readonly UrlEncoder           _urlEncoder;
    private readonly AuditService         _audit;

    public ManageTwoFactorModel(UserManager<AppUser> userManager,
        UrlEncoder urlEncoder, AuditService audit)
    {
        _userManager = userManager;
        _urlEncoder  = urlEncoder;
        _audit       = audit;
    }

    public bool   Is2FAEnabled   { get; set; }
    public string SharedKey      { get; set; } = string.Empty;
    public string AuthenticatorUri { get; set; } = string.Empty;
    public string? StatusMessage { get; set; }
    public string? ErrorMessage  { get; set; }

    [BindProperty]
    [Required]
    [StringLength(7, MinimumLength = 6)]
    [Display(Name = "Verification Code")]
    public string Code { get; set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();

        Is2FAEnabled = await _userManager.GetTwoFactorEnabledAsync(user);
        await LoadSharedKeyAsync(user);
        return Page();
    }

    public async Task<IActionResult> OnPostEnableAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();

        var stripped = Code.Replace(" ", "").Replace("-", "");
        var isValid  = await _userManager.VerifyTwoFactorTokenAsync(
            user, _userManager.Options.Tokens.AuthenticatorTokenProvider, stripped);

        if (!isValid)
        {
            await LoadSharedKeyAsync(user);
            ErrorMessage = "Invalid verification code. Please try again.";
            Is2FAEnabled = await _userManager.GetTwoFactorEnabledAsync(user);
            return Page();
        }

        await _userManager.SetTwoFactorEnabledAsync(user, true);
        await _audit.LogAsync(user.Id, user.FullName, "Enable2FA", "AppUser",
            details: "User enabled two-factor authentication");

        StatusMessage = "? Two-factor authentication has been enabled.";
        Is2FAEnabled  = true;
        await LoadSharedKeyAsync(user);
        return Page();
    }

    public async Task<IActionResult> OnPostDisableAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();

        await _userManager.SetTwoFactorEnabledAsync(user, false);
        await _userManager.ResetAuthenticatorKeyAsync(user);
        await _audit.LogAsync(user.Id, user.FullName, "Disable2FA", "AppUser",
            details: "User disabled two-factor authentication");

        StatusMessage = "?? Two-factor authentication has been disabled.";
        Is2FAEnabled  = false;
        await LoadSharedKeyAsync(user);
        return Page();
    }

    private async Task LoadSharedKeyAsync(AppUser user)
    {
        var unformattedKey = await _userManager.GetAuthenticatorKeyAsync(user);
        if (string.IsNullOrEmpty(unformattedKey))
        {
            await _userManager.ResetAuthenticatorKeyAsync(user);
            unformattedKey = await _userManager.GetAuthenticatorKeyAsync(user);
        }

        SharedKey       = FormatKey(unformattedKey!);
        AuthenticatorUri = GenerateQrCodeUri(user.Email ?? user.UserName ?? "", unformattedKey!);
    }

    private static string FormatKey(string key)
    {
        var sb = new StringBuilder();
        var i  = 0;
        while (i + 4 < key.Length)
        {
            sb.Append(key.AsSpan(i, 4)).Append(' ');
            i += 4;
        }
        if (i < key.Length) sb.Append(key.AsSpan(i));
        return sb.ToString().ToLowerInvariant();
    }

    private string GenerateQrCodeUri(string email, string unformattedKey) =>
        $"otpauth://totp/{_urlEncoder.Encode("Nirvachak AI")}:{_urlEncoder.Encode(email)}" +
        $"?secret={unformattedKey}&issuer={_urlEncoder.Encode("Nirvachak AI")}&digits=6";
}
