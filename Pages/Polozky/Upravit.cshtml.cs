using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NfcHomeManager.Data;

namespace NfcHomeManager.Pages.Polozky;

public class UpravitModel(AppDbContext db) : PageModel
{
    [BindProperty]
    public PolozkaFormInput Input { get; set; } = new();

    [BindProperty]
    public int Id { get; set; }

    public async Task<IActionResult> OnGetAsync(int id, CancellationToken ct)
    {
        var polozka = await db.Polozky.FindAsync([id], ct);
        if (polozka is null)
        {
            return NotFound();
        }

        Id = polozka.Id;
        Input = new PolozkaFormInput
        {
            Nazev = polozka.Nazev,
            NfcUid = polozka.NfcUid,
            KategorieId = polozka.KategorieId,
            MistnostId = polozka.MistnostId,
            Vyrobce = polozka.Vyrobce,
            Model = polozka.Model,
            SerioveCislo = polozka.SerioveCislo,
            DatumPorizeni = polozka.DatumPorizeni,
            CenaKc = polozka.CenaKc,
            ZarukaMesice = polozka.ZarukaMesice,
            DalsiServisDo = polozka.DalsiServisDo,
            Poznamka = polozka.Poznamka
        };

        await NacistCiselnikyAsync(ct);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        await NacistCiselnikyAsync(ct);

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var polozka = await db.Polozky.FindAsync([Id], ct);
        if (polozka is null)
        {
            return NotFound();
        }

        polozka.Nazev = Input.Nazev.Trim();
        polozka.NfcUid = string.IsNullOrWhiteSpace(Input.NfcUid) ? null : Input.NfcUid.Trim();
        polozka.KategorieId = Input.KategorieId;
        polozka.MistnostId = Input.MistnostId;
        polozka.Vyrobce = Input.Vyrobce;
        polozka.Model = Input.Model;
        polozka.SerioveCislo = Input.SerioveCislo;
        polozka.DatumPorizeni = Input.DatumPorizeni;
        polozka.CenaKc = Input.CenaKc;
        polozka.ZarukaMesice = Input.ZarukaMesice;
        polozka.DalsiServisDo = Input.DalsiServisDo;
        polozka.Poznamka = Input.Poznamka;
        polozka.UpravenoUtc = DateTime.UtcNow;
        polozka.PrepocitatZaruku();

        await db.SaveChangesAsync(ct);

        return Redirect($"/Polozky/Detail?id={polozka.Id}");
    }

    private async Task NacistCiselnikyAsync(CancellationToken ct)
    {
        ViewData["VsechnyKategorie"] = await db.Kategorie.OrderBy(k => k.Nazev).ToListAsync(ct);
        ViewData["VsechnyMistnosti"] = await db.Mistnosti.OrderBy(m => m.Nazev).ToListAsync(ct);
    }
}
