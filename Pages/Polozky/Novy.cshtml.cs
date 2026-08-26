using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NfcHomeManager.Data;
using NfcHomeManager.Models;
using NfcHomeManager.Services;
using System.ComponentModel.DataAnnotations;

namespace NfcHomeManager.Pages.Polozky;

public class NovyModel(AppDbContext db) : PageModel
{
    [BindProperty]
    public PolozkaFormInput Input { get; set; } = new();

    public async Task OnGetAsync(int? kontejnerId, CancellationToken ct)
    {
        Input.ZarukaMesice = 24;
        Input.KontejnerId = kontejnerId;
        await NacistCiselnikyAsync(ct);
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        await NacistCiselnikyAsync(ct);

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var polozka = new Polozka
        {
            Kod = await KodGenerator.VygenerovatUnikatniAsync(db, ct),
            Nazev = Input.Nazev.Trim(),
            NfcUid = string.IsNullOrWhiteSpace(Input.NfcUid) ? null : Input.NfcUid.Trim(),
            Rezim = Input.Rezim,
            Specializace = Input.Specializace,
            Spz = Input.Spz,
            KategorieId = Input.KategorieId,
            MistnostId = Input.MistnostId,
            KontejnerId = Input.KontejnerId,
            Vyrobce = Input.Vyrobce,
            Model = Input.Model,
            SerioveCislo = Input.SerioveCislo,
            DatumPorizeni = Input.DatumPorizeni,
            CenaKc = Input.CenaKc,
            ZarukaMesice = Input.ZarukaMesice,
            DalsiServisDo = Input.DalsiServisDo,
            MaVlastniNfcKartu = Input.MaVlastniNfcKartu,
            SledovatPojisteni = Input.SledovatPojisteni,
            SledovatExpiraci = Input.SledovatExpiraci,
            SledovatServis = Input.SledovatServis,
            SledovatRevizi = Input.SledovatRevizi,
            Expirace = Input.Expirace,
            Poznamka = Input.Poznamka
        };

        polozka.PrepocitatZaruku();

        db.Polozky.Add(polozka);
        await db.SaveChangesAsync(ct);

        return Redirect($"/Polozky/Detail?id={polozka.Id}");
    }

    private async Task NacistCiselnikyAsync(CancellationToken ct)
    {
        ViewData["VsechnyKategorie"] = await db.Kategorie.OrderBy(k => k.Nazev).ToListAsync(ct);
        ViewData["VsechnyMistnosti"] = await db.Mistnosti.OrderBy(m => m.Nazev).ToListAsync(ct);
        ViewData["VsechnyKontejnery"] = await db.Polozky
            .Where(p => p.Rezim == NfcRezim.Kontejner || p.Rezim == NfcRezim.PrvniPomoc)
            .OrderBy(p => p.Nazev)
            .ToListAsync(ct);
    }
}

public class PolozkaFormInput
{
    [Required(ErrorMessage = "Zadejte název.")]
    [StringLength(200)]
    public string Nazev { get; set; } = string.Empty;

    public NfcRezim Rezim { get; set; } = NfcRezim.Predmet;
    public Specializace Specializace { get; set; } = Specializace.Obecna;

    [StringLength(20)]
    public string? Spz { get; set; }

    public int? KategorieId { get; set; }
    public int? MistnostId { get; set; }
    public int? KontejnerId { get; set; }

    [StringLength(100)]
    public string? Vyrobce { get; set; }

    [StringLength(100)]
    public string? Model { get; set; }

    [StringLength(100)]
    public string? SerioveCislo { get; set; }

    [StringLength(40)]
    public string? NfcUid { get; set; }

    public DateOnly? DatumPorizeni { get; set; }

    [Range(0, 100_000_000)]
    public decimal? CenaKc { get; set; }

    [Range(0, 240)]
    public int ZarukaMesice { get; set; } = 24;

    public DateOnly? DalsiServisDo { get; set; }

    public bool MaVlastniNfcKartu { get; set; } = true;
    public bool SledovatPojisteni { get; set; }
    public bool SledovatExpiraci { get; set; }
    public bool SledovatServis { get; set; }
    public bool SledovatRevizi { get; set; }
    public DateOnly? Expirace { get; set; }

    [StringLength(2000)]
    public string? Poznamka { get; set; }
}
