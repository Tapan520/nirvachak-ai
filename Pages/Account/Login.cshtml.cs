using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using Nirvachak_AI.Domain.Entities;
using Nirvachak_AI.Infrastructure.Services;

namespace Nirvachak_AI.Pages.Account;

public class LoginModel : PageModel
{
    private readonly SignInManager<AppUser> _signInManager;
    private readonly UserManager<AppUser> _userManager;
    private readonly AuditService _audit;

    public LoginModel(SignInManager<AppUser> signInManager, UserManager<AppUser> userManager, AuditService audit)
    {
        _signInManager = signInManager;
        _userManager   = userManager;
        _audit         = audit;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ErrorMessage { get; set; }
    public string? ReturnUrl { get; set; }

    public class InputModel
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; }
    }

    public void OnGet(string? returnUrl = null)
    {
        ReturnUrl = returnUrl;
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        if (!ModelState.IsValid) return Page();

        var result = await _signInManager.PasswordSignInAsync(
            Input.Email, Input.Password, Input.RememberMe, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            var user = await _userManager.FindByEmailAsync(Input.Email);
            if (user != null)
                await _audit.LogAsync(user.Id, user.FullName, "Login", "Session",
                    details: $"Login from {HttpContext.Connection.RemoteIpAddress}",
                    constituencyId: user.ConstituencyId);

            return LocalRedirect(returnUrl ?? "/Dashboard/Index");
        }

        if (result.RequiresTwoFactor)
        {
            return RedirectToPage("/Account/TwoFactorLogin", new { returnUrl });
        }

        await _audit.LogAsync("unknown", Input.Email, "LoginFailed", "Session",
            details: $"Failed login attempt for {Input.Email}");

        ErrorMessage = "Invalid email or password.";
        return Page();
    }
}
