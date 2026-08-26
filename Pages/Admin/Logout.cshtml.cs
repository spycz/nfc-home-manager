using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace NfcHomeManager.Pages.Admin;

// Samostatna stranka misto "holeho" MapPost endpointu, aby odhlaseni
// prochazelo stejnou automatickou CSRF (antiforgery) ochranou jako
// vsechny ostatni formulare v administraci.
public class LogoutModel : PageModel
{
    public IActionResult OnGet() => RedirectToPage("/Index");

    public async Task<IActionResult> OnPostAsync()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToPage("/Admin/Login");
    }
}
