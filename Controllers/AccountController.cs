using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Nirvachak_AI.Domain.Entities;
using Nirvachak_AI.Infrastructure.Services;
using System.Text;
using System.Text.Encodings.Web;

namespace Nirvachak_AI.Controllers;

[ApiController]
[Route("api/account")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class AccountController : ApiBaseController
{
    private readonly UserManager<AppUser> _userManager;
    private readonly UrlEncoder _urlEncoder;
    private readonly AuditService _audit;

    public AccountController(UserManager<AppUser> userManager, UrlEncoder urlEncoder, AuditService audit)
    {
        _userManager = userManager;
        _urlEncoder = urlEncoder;
        _audit = audit;
    }

    public record TwoFactorStatusDto(bool IsEnabled, string SharedKey, string AuthenticatorUri);
    public record VerifyCodeRequest(string Code);

    [HttpGet("2fa")]
    public async Task<IActionResult> Get2FA()
    {
        var user = await _userManager.FindByIdAsync(GetUserId());
        if (user == null) return NotFound();

        var enabled = await _userManager.GetTwoFactorEnabledAsync(user);
        var key = await _userManager.GetAuthenticatorKeyAsync(user);
        if (string.IsNullOrEmpty(key))
        {
            await _userManager.ResetAuthenticatorKeyAsync(user);
            key = await _userManager.GetAuthenticatorKeyAsync(user);
        }
        return Ok(new TwoFactorStatusDto(
            enabled,
            FormatKey(key!),
            BuildOtpAuthUri(user.Email ?? user.UserName ?? "", key!)));
    }

    [HttpPost("2fa/enable")]
    public async Task<IActionResult> Enable2FA([FromBody] VerifyCodeRequest req)
    {
        var user = await _userManager.FindByIdAsync(GetUserId());
        if (user == null) return NotFound();

        var stripped = (req.Code ?? "").Replace(" ", "").Replace("-", "");
        var valid = await _userManager.VerifyTwoFactorTokenAsync(
            user, _userManager.Options.Tokens.AuthenticatorTokenProvider, stripped);
        if (!valid) return BadRequest(new { error = "Invalid verification code" });

        await _userManager.SetTwoFactorEnabledAsync(user, true);
        await _audit.LogAsync(user.Id, user.FullName, "Enable2FA", "AppUser",
            details: "User enabled two-factor authentication (mobile)");
        return Ok(new { enabled = true });
    }

    [HttpPost("2fa/disable")]
    public async Task<IActionResult> Disable2FA()
    {
        var user = await _userManager.FindByIdAsync(GetUserId());
        if (user == null) return NotFound();

        await _userManager.SetTwoFactorEnabledAsync(user, false);
        await _userManager.ResetAuthenticatorKeyAsync(user);
        await _audit.LogAsync(user.Id, user.FullName, "Disable2FA", "AppUser",
            details: "User disabled two-factor authentication (mobile)");
        return Ok(new { enabled = false });
    }

    private static string FormatKey(string key)
    {
        var sb = new StringBuilder();
        var i = 0;
        while (i + 4 < key.Length) { sb.Append(key.AsSpan(i, 4)).Append(' '); i += 4; }
        if (i < key.Length) sb.Append(key.AsSpan(i));
        return sb.ToString().ToLowerInvariant();
    }

    private string BuildOtpAuthUri(string email, string key) =>
        $"otpauth://totp/{_urlEncoder.Encode("Nirvachak AI")}:{_urlEncoder.Encode(email)}" +
        $"?secret={key}&issuer={_urlEncoder.Encode("Nirvachak AI")}&digits=6";
}
