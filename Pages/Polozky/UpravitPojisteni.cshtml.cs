using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NfcHomeManager.Data;
using NfcHomeManager.Models;

namespace NfcHomeManager.Pages.Polozky;

public class UpravitPojisteniModel(AppDbContext db) : PageModel
{
    [BindProperty]
    public NovePojisteniInput Input { get; set; } = new();

    public int Id { get; set; }
    public Polozka Polozka { get; set; } = null!;

    public async Task<IActionResult> OnGetAsync(int id, CancellationToken ct)
    {
        if (await NacistAsync(id, ct) is not { } pojisteni)
        {
            return NotFound();
        }

        Input = new NovePojisteniInput
        {
            Pojistovna = pojisteni.Pojistovna,
            CisloSmlouvy = pojisteni.CisloSmlouvy,
            Typ = pojisteni.Typ,
            PlatnostOd = pojisteni.PlatnostOd,
            PlatnostDo = pojisteni.PlatnostDo,
            RocniCenaKc = pojisteni.RocniCenaKc,
            Poznamka = pojisteni.Poznamka
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id, CancellationToken ct)
    {
        if (await NacistAsync(id, ct) is not { } pojisteni)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        pojisteni.Pojistovna = Input.Pojistovna.Trim();
        pojisteni.CisloSmlouvy = Input.CisloSmlouvy;
        pojisteni.Typ = Input.Typ;
        pojisteni.PlatnostOd = Input.PlatnostOd;
        pojisteni.PlatnostDo = Input.PlatnostDo;
        pojisteni.RocniCenaKc = Input.RocniCenaKc;
        pojisteni.Poznamka = Input.Poznamka;

        await db.SaveChangesAsync(ct);
        return Redirect($"/Polozky/Detail?id={pojisteni.PolozkaId}");
    }

    private async Task<Pojisteni?> NacistAsync(int id, CancellationToken ct)
    {
        var pojisteni = await db.Pojisteni.Include(i => i.Polozka).FirstOrDefaultAsync(i => i.Id == id, ct);
        if (pojisteni?.Polozka is null)
        {
            return null;
        }

        Id = id;
        Polozka = pojisteni.Polozka;
        return pojisteni;
    }
}
