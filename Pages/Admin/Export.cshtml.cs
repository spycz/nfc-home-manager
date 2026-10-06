using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NfcHomeManager.Data;
using NfcHomeManager.Services;

namespace NfcHomeManager.Pages.Admin;

public class ExportModel(AppDbContext db) : PageModel
{
    public void OnGet()
    {
    }

    // Zaloha cele databaze jako jeden JSON soubor - format viz Services/Zaloha.cs,
    // obnova na /Admin/Obnova.
    public async Task<IActionResult> OnGetStahnoutAsync(CancellationToken ct)
    {
        var bytes = Zaloha.Serializovat(await Zaloha.VytvoritAsync(db, ct));
        var fileName = $"nfc-home-export-{DateTime.UtcNow:yyyyMMdd-HHmm}.json";

        return File(bytes, "application/json", fileName);
    }
}
