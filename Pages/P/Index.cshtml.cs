using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NfcHomeManager.Data;
using NfcHomeManager.Models;

namespace NfcHomeManager.Pages.P;

// Stranka je verejna (AllowAnonymous), ale rozhoduje se az podle nactenych
// dat - viz OnGetAsync. NFC stitek se pozna jen fyzickou blizkosti (~4 cm),
// coz je pro bezne predmety dostatecna "duvera". Lekarnicka a prvni pomoc
// ale nesou citliva rodinna zdravotni data, takze tam se bez prihlaseni
// (napr. z PC bez predchoziho naskenovani na danem zarizeni) neprojde dal.
[AllowAnonymous]
public class IndexModel(AppDbContext db) : PageModel
{
    public Polozka? Polozka { get; set; }

    public async Task<IActionResult> OnGetAsync(string kod, CancellationToken ct)
    {
        Polozka = await db.Polozky
            .Include(p => p.Kategorie)
            .Include(p => p.Mistnost)
            .Include(p => p.Kontejner)
            .Include(p => p.Obsah.OrderBy(o => o.Nazev))
            .Include(p => p.Leky.OrderBy(l => l.Expirace))
            .Include(p => p.ServisniZaznamy.OrderByDescending(s => s.Datum).Take(5))
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Kod == kod, ct);

        if (Polozka is { Rezim: NfcRezim.Lekarnicka or NfcRezim.PrvniPomoc } &&
            User.Identity?.IsAuthenticated != true)
        {
            return Challenge();
        }

        return Page();
    }
}
