using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NfcHomeManager.Services;
using System.Security.Claims;

namespace NfcHomeManager.Pages.Admin;

public class LoginModel(IConfiguration configuration) : PageModel
{
    [BindProperty]
    public string Username { get; set; } = string.Empty;

    [BindProperty]
    public string Password { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
    public string? Error { get; set; }

    public void OnGet(string? returnUrl)
    {
        ReturnUrl = returnUrl;
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl)
    {
        ReturnUrl = returnUrl;

        var expectedUsername = configuration["AdminAuth:Username"];
        var expectedHash = configuration["AdminAuth:PasswordHash"];

        if (string.IsNullOrEmpty(expectedUsername) ||
            !string.Equals(Username, expectedUsername, StringComparison.Ordinal) ||
            !AdminCredentialHasher.Verify(Password, expectedHash))
        {
            Error = "Špatné jméno nebo heslo.";
            return Page();
        }

        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.Name, expectedUsername)],
            CookieAuthenticationDefaults.AuthenticationScheme);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30)
            });

        var target = !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? returnUrl
            : "/";

        return Redirect(target);
    }
}
