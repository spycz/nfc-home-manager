using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NfcHomeManager.Data;
using NfcHomeManager.Models;

namespace NfcHomeManager.Pages.Polozky;

public class UpravitLekModel(AppDbContext db) : PageModel
{
    [BindProperty]
    public NovyLekInput Input { get; set; } = new();

    public int Id { get; set; }
    public Polozka Lekarnicka { get; set; } = null!;

    public async Task<IActionResult> OnGetAsync(int id, CancellationToken ct)
    {
        if (await NacistAsync(id, ct) is not { } lek)
        {
            return NotFound();
        }

        Input = new NovyLekInput
        {
            Nazev = lek.Nazev,
            JeLek = lek.JeLek,
            Ean = lek.Ean,
            Mnozstvi = lek.Mnozstvi,
            Jednotka = lek.Jednotka,
            NaCoJe = lek.NaCoJe,
            ProKoho = lek.ProKoho,
            NaPredpis = lek.NaPredpis,
            Davkovani = lek.Davkovani,
            NezadouciUcinky = lek.NezadouciUcinky,
            Interakce = lek.Interakce,
            Expirace = lek.Expirace,
            Poznamka = lek.Poznamka
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id, CancellationToken ct)
    {
        if (await NacistAsync(id, ct) is not { } lek)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        lek.Nazev = Input.Nazev.Trim();
        lek.JeLek = Input.JeLek;
        lek.Ean = Input.Ean;
        lek.Mnozstvi = Input.Mnozstvi;
        lek.Jednotka = Input.Jednotka;
        lek.NaCoJe = Input.NaCoJe;
        lek.ProKoho = Input.ProKoho;
        lek.NaPredpis = Input.NaPredpis;
        lek.Davkovani = Input.Davkovani;
        lek.NezadouciUcinky = Input.NezadouciUcinky;
        lek.Interakce = Input.Interakce;
        lek.Expirace = Input.Expirace;
        lek.Poznamka = Input.Poznamka;

        await db.SaveChangesAsync(ct);
        return Redirect($"/Polozky/Detail?id={lek.LekarnickaId}");
    }

    private async Task<Lek?> NacistAsync(int id, CancellationToken ct)
    {
        var lek = await db.Leky.Include(l => l.Lekarnicka).FirstOrDefaultAsync(l => l.Id == id, ct);
        if (lek?.Lekarnicka is null)
        {
            return null;
        }

        Id = id;
        Lekarnicka = lek.Lekarnicka;
        return lek;
    }
}
