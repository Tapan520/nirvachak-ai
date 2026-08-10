using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Web;
using Nirvachak_AI.Domain.Entities;
using Nirvachak_AI.Infrastructure.Services;

namespace Nirvachak_AI.Pages.Account;

public class ForgotPasswordModel : PageModel
{
    private readonly UserManager<AppUser> _userManager;
    private readonly IEmailService _email;
    private readonly ILogger<ForgotPasswordModel> _logger;

    public ForgotPasswordModel(UserManager<AppUser> userManager,
        IEmailService email, ILogger<ForgotPasswordModel> logger)
    {
        _userManager = userManager;
        _email       = email;
        _logger      = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public bool   EmailSent    { get; set; }
    public string? ErrorMessage { get; set; }

    public class InputModel
    {
        [Required, EmailAddress, Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;
    }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        var user = await _userManager.FindByEmailAsync(Input.Email.Trim());

        // Always show success — do not reveal whether an account exists (security best practice)
        EmailSent = true;

        if (user == null || !user.IsActive)
        {
            _logger.LogWarning("[ForgotPassword] No active account for {Email}", Input.Email);
            return Page();
        }

        try
        {
            var token      = await _userManager.GeneratePasswordResetTokenAsync(user);
            var encodedToken = HttpUtility.UrlEncode(token);
            var baseUrl    = $"{Request.Scheme}://{Request.Host}";
            var resetLink  = $"{baseUrl}/Account/ResetPassword?email={HttpUtility.UrlEncode(user.Email)}&token={encodedToken}";

            var html = $@"
<div style='font-family:Arial,sans-serif;max-width:520px;margin:auto;padding:24px;border:1px solid #dee2e6;border-radius:8px'>
  <div style='text-align:center;margin-bottom:24px'>
    <h2 style='color:#3b5bdb;margin:0'>?? Nirvachak AI</h2>
    <p style='color:#868e96;margin:4px 0 0'>Password Reset Request</p>
  </div>
  <p>Hi <strong>{user.FullName}</strong>,</p>
  <p>We received a request to reset your password. Click the button below to set a new password:</p>
  <div style='text-align:center;margin:28px 0'>
    <a href='{resetLink}'
       style='background:#3b5bdb;color:#fff;padding:12px 28px;border-radius:6px;text-decoration:none;font-weight:600;display:inline-block'>
      Reset My Password
    </a>
  </div>
  <p style='color:#868e96;font-size:13px'>This link expires in <strong>24 hours</strong>. If you did not request a password reset, please ignore this email — your account remains secure.</p>
  <hr style='border:none;border-top:1px solid #dee2e6;margin:20px 0'/>
  <p style='color:#adb5bd;font-size:11px;text-align:center'>Nirvachak AI — India Election Campaign Management</p>
</div>";

            await _email.SendAsync(user.Email!, user.FullName, "Reset Your Nirvachak AI Password", html);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ForgotPassword] Failed to send reset email to {Email}", user.Email);
            // Still show success to avoid revealing account existence
        }

        return Page();
    }
}
