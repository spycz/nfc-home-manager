using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NfcHomeManager.Data;
using System.Text;
using System.Text.Json;

namespace NfcHomeManager.Pages.Admin;

public class ExportModel(AppDbContext db) : PageModel
{
    public void OnGet()
    {
    }

    // Plocha zaloha cele databaze jako jeden JSON soubor. Zadne .Include()
    // navigacnich vlastnosti - entity se serializuji jen s vlastnimi
    // sloupci a cizimi klici, takze nehrozi cyklus pres obousmerne vazby.
    public async Task<IActionResult> OnGetStahnoutAsync(CancellationToken ct)
    {
        var export = new
        {
            ExportovanoUtc = DateTime.UtcNow,
            Mistnosti = await db.Mistnosti.AsNoTracking().ToListAsync(ct),
            Kategorie = await db.Kategorie.AsNoTracking().ToListAsync(ct),
            Polozky = await db.Polozky.AsNoTracking().ToListAsync(ct),
            ServisniZaznamy = await db.ServisniZaznamy.AsNoTracking().ToListAsync(ct),
            Pojisteni = await db.Pojisteni.AsNoTracking().ToListAsync(ct),
            Leky = await db.Leky.AsNoTracking().ToListAsync(ct)
        };

        var json = JsonSerializer.Serialize(export, new JsonSerializerOptions { WriteIndented = true });
        var bytes = Encoding.UTF8.GetBytes(json);
        var fileName = $"nfc-home-export-{DateTime.UtcNow:yyyyMMdd-HHmm}.json";

        return File(bytes, "application/json", fileName);
    }
}
