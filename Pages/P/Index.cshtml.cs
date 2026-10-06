using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NfcHomeManager.Data;
using NfcHomeManager.Models;

namespace NfcHomeManager.Pages.P;

// Stranka je bez [Authorize], ale rozhoduje se az podle nactenych dat - viz
// OnGetAsync. Znalost URL NENI dukaz fyzickeho prilozeni telefonu: odkaz lze
// opsat, sdilet nebo znovu otevrit z historie. Proto se neprihlasenemu
// zobrazi jen polozka vyslovne oznacena jako verejna (Polozka.Verejna), a to
// v omezene podobe bez poznamky a serioveho cisla. Lekarnicka a prvni pomoc
// nejsou verejne nikdy.
[AllowAnonymous]
public class IndexModel(AppDbContext db) : PageModel
{
    public Polozka? Polozka { get; set; }

    public bool JePrihlaseny => User.Identity?.IsAuthenticated == true;

    // Obsah kontejneru, ktery smi aktualni navstevnik videt.
    public List<Polozka> ViditelnyObsah { get; set; } = [];
    public int SkrytychPolozek { get; set; }

    public async Task<IActionResult> OnGetAsync(string kod, CancellationToken ct)
    {
        Polozka = await db.Polozky
            .Include(p => p.Kategorie)
            .Include(p => p.Mistnost)
            .Include(p => p.Kontejner)
            .Include(p => p.Obsah.Where(o => o.Aktivni).OrderBy(o => o.Nazev))
            .Include(p => p.Leky.OrderBy(l => l.Expirace))
            .Include(p => p.ServisniZaznamy.OrderByDescending(s => s.Datum).Take(5))
            .Include(p => p.Terminy.OrderBy(t => t.DatumDo))
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Kod == kod, ct);

        // Neznamy kod i archivovana polozka se neprihlasenemu jevi stejne,
        // aby se z odpovedi nedalo poznat, ktere kody existuji.
        if (Polozka is null || (!Polozka.Aktivni && !JePrihlaseny))
        {
            Polozka = null;
            Response.StatusCode = StatusCodes.Status404NotFound;
            return Page();
        }

        if (!JePrihlaseny && !Polozka.JeVerejnaStranka)
        {
            return Challenge();
        }

        if (JePrihlaseny)
        {
            ViditelnyObsah = Polozka.Obsah;
        }
        else
        {
            ViditelnyObsah = Polozka.Obsah.Where(o => o.JeVerejnaStranka).ToList();
            SkrytychPolozek = Polozka.Obsah.Count - ViditelnyObsah.Count;

            // Archivovany rodic sam vraci 404, proto se nesmi prozradit ani zde.
            if (Polozka.Kontejner is { } rodic && !(rodic.JeVerejnaStranka && rodic.Aktivni))
            {
                Polozka.Kontejner = null;
            }
        }

        return Page();
    }
}
