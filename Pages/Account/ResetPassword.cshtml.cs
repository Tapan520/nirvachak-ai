using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Web;
using Nirvachak_AI.Domain.Entities;

namespace Nirvachak_AI.Pages.Account;

public class ResetPasswordModel : PageModel
{
    private readonly UserManager<AppUser> _userManager;

    public ResetPasswordModel(UserManager<AppUser> userManager)
        => _userManager = userManager;

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public bool    ResetSuccess  { get; set; }
    public string? ErrorMessage  { get; set; }

    public class InputModel
    {
        [Required]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Token { get; set; } = string.Empty;

        [Required, MinLength(6)]
        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        public string NewPassword { get; set; } = string.Empty;

        [Required, DataType(DataType.Password)]
        [Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match.")]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public IActionResult OnGet(string? email, string? token)
    {
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token))
        {
            ErrorMessage = "Invalid or expired reset link. Please request a new one.";
            return Page();
        }

        Input.Email = email;
        Input.Token = token;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        var user = await _userManager.FindByEmailAsync(Input.Email);
        if (user == null)
        {
            // Do not reveal that the user doesn't exist
            ResetSuccess = true;
            return Page();
        }

        // Token comes URL-encoded — decode before using
        var decodedToken = HttpUtility.UrlDecode(Input.Token);
        var result = await _userManager.ResetPasswordAsync(user, decodedToken, Input.NewPassword);

        if (result.Succeeded)
        {
            ResetSuccess = true;
            return Page();
        }

        ErrorMessage = result.Errors.FirstOrDefault()?.Description
            ?? "Reset failed. The link may have expired. Please request a new one.";
        return Page();
    }
}
