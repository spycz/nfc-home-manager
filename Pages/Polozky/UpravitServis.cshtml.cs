using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NfcHomeManager.Data;
using NfcHomeManager.Models;

namespace NfcHomeManager.Pages.Polozky;

// Oprava existujiciho servisniho zaznamu. Meni jen zaznam v historii -
// planovane terminy polozky se upravuji zvlast na jejim detailu.
public class UpravitServisModel(AppDbContext db) : PageModel
{
    [BindProperty]
    public NovyServisInput Input { get; set; } = new();

    public int Id { get; set; }
    public Polozka Polozka { get; set; } = null!;

    public async Task<IActionResult> OnGetAsync(int id, CancellationToken ct)
    {
        if (await NacistAsync(id, ct) is not { } zaznam)
        {
            return NotFound();
        }

        Input = new NovyServisInput
        {
            Datum = zaznam.Datum,
            Typ = zaznam.Typ,
            Popis = zaznam.Popis,
            CenaKc = zaznam.CenaKc,
            Provozovna = zaznam.Provozovna,
            DalsiTerminDo = zaznam.DalsiTerminDo,
            DalsiTerminTyp = zaznam.DalsiTerminTyp ?? TerminTyp.Servis
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id, CancellationToken ct)
    {
        if (await NacistAsync(id, ct) is not { } zaznam)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        zaznam.Datum = Input.Datum;
        zaznam.Typ = Input.Typ;
        zaznam.Popis = Input.Popis.Trim();
        zaznam.CenaKc = Input.CenaKc;
        zaznam.Provozovna = Input.Provozovna;
        zaznam.DalsiTerminDo = Input.DalsiTerminDo;
        zaznam.DalsiTerminTyp = Input.DalsiTerminDo.HasValue ? Input.DalsiTerminTyp : null;

        await db.SaveChangesAsync(ct);
        return Redirect($"/Polozky/Detail?id={zaznam.PolozkaId}");
    }

    private async Task<ServisniZaznam?> NacistAsync(int id, CancellationToken ct)
    {
        var zaznam = await db.ServisniZaznamy.Include(s => s.Polozka).FirstOrDefaultAsync(s => s.Id == id, ct);
        if (zaznam?.Polozka is null)
        {
            return null;
        }

        Id = id;
        Polozka = zaznam.Polozka;
        return zaznam;
    }
}
