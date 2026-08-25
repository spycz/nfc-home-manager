using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NfcHomeManager.Data;
using NfcHomeManager.Models;
using System.ComponentModel.DataAnnotations;

namespace NfcHomeManager.Pages.Polozky;

public class DetailModel(AppDbContext db) : PageModel
{
    public Polozka Polozka { get; set; } = null!;
    public string VerejnaUrl { get; set; } = string.Empty;

    [BindProperty]
    public NovyServisInput NovyServis { get; set; } = new() { Datum = DateOnly.FromDateTime(DateTime.Today) };

    [BindProperty]
    public NovePojisteniInput NovePojisteni { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id, CancellationToken ct)
    {
        if (!await NacistPolozkuAsync(id, ct))
        {
            return NotFound();
        }

        return Page();
    }

    public async Task<IActionResult> OnPostPridatServisAsync(int id, CancellationToken ct)
    {
        if (!await NacistPolozkuAsync(id, ct))
        {
            return NotFound();
        }

        // Vycistit ModelState od validace nesouvisejiciho formulare (NovePojisteni)
        // navazaneho na stejnou stranku a validovat jen prave odeslany model.
        ModelState.Clear();
        if (!TryValidateModel(NovyServis, nameof(NovyServis)))
        {
            return Page();
        }

        db.ServisniZaznamy.Add(new ServisniZaznam
        {
            PolozkaId = id,
            Datum = NovyServis.Datum,
            Typ = NovyServis.Typ,
            Popis = NovyServis.Popis.Trim(),
            CenaKc = NovyServis.CenaKc,
            Provozovna = NovyServis.Provozovna,
            DalsiTerminDo = NovyServis.DalsiTerminDo
        });

        if (NovyServis.DalsiTerminDo.HasValue)
        {
            Polozka.DalsiServisDo = NovyServis.DalsiTerminDo;
        }

        await db.SaveChangesAsync(ct);
        return Redirect($"/Polozky/Detail?id={id}");
    }

    public async Task<IActionResult> OnPostSmazatServisAsync(int id, int servisId, CancellationToken ct)
    {
        var zaznam = await db.ServisniZaznamy.FirstOrDefaultAsync(s => s.Id == servisId && s.PolozkaId == id, ct);
        if (zaznam is not null)
        {
            db.ServisniZaznamy.Remove(zaznam);
            await db.SaveChangesAsync(ct);
        }

        return Redirect($"/Polozky/Detail?id={id}");
    }

    public async Task<IActionResult> OnPostPridatPojisteniAsync(int id, CancellationToken ct)
    {
        if (!await NacistPolozkuAsync(id, ct))
        {
            return NotFound();
        }

        ModelState.Clear();
        if (!TryValidateModel(NovePojisteni, nameof(NovePojisteni)))
        {
            return Page();
        }

        db.Pojisteni.Add(new Pojisteni
        {
            PolozkaId = id,
            Pojistovna = NovePojisteni.Pojistovna.Trim(),
            CisloSmlouvy = NovePojisteni.CisloSmlouvy,
            Typ = NovePojisteni.Typ,
            PlatnostOd = NovePojisteni.PlatnostOd,
            PlatnostDo = NovePojisteni.PlatnostDo,
            RocniCenaKc = NovePojisteni.RocniCenaKc,
            Poznamka = NovePojisteni.Poznamka
        });

        await db.SaveChangesAsync(ct);
        return Redirect($"/Polozky/Detail?id={id}");
    }

    public async Task<IActionResult> OnPostSmazatPojisteniAsync(int id, int pojisteniId, CancellationToken ct)
    {
        var pojisteni = await db.Pojisteni.FirstOrDefaultAsync(i => i.Id == pojisteniId && i.PolozkaId == id, ct);
        if (pojisteni is not null)
        {
            db.Pojisteni.Remove(pojisteni);
            await db.SaveChangesAsync(ct);
        }

        return Redirect($"/Polozky/Detail?id={id}");
    }

    public async Task<IActionResult> OnPostSmazatPolozkuAsync(int id, CancellationToken ct)
    {
        var polozka = await db.Polozky.FindAsync([id], ct);
        if (polozka is not null)
        {
            db.Polozky.Remove(polozka);
            await db.SaveChangesAsync(ct);
        }

        return Redirect("/Polozky");
    }

    private async Task<bool> NacistPolozkuAsync(int id, CancellationToken ct)
    {
        var polozka = await db.Polozky
            .Include(p => p.Kategorie)
            .Include(p => p.Mistnost)
            .Include(p => p.ServisniZaznamy.OrderByDescending(s => s.Datum))
            .Include(p => p.Pojisteni.OrderByDescending(i => i.PlatnostDo))
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (polozka is null)
        {
            return false;
        }

        Polozka = polozka;
        VerejnaUrl = $"{Request.Scheme}://{Request.Host}/p/{polozka.Kod}";
        return true;
    }
}

public class NovyServisInput
{
    [Required]
    public DateOnly Datum { get; set; }

    public ServisTyp Typ { get; set; } = ServisTyp.Servis;

    [Required(ErrorMessage = "Popište, co se dělalo.")]
    [StringLength(500)]
    public string Popis { get; set; } = string.Empty;

    [Range(0, 100_000_000)]
    public decimal? CenaKc { get; set; }

    [StringLength(150)]
    public string? Provozovna { get; set; }

    public DateOnly? DalsiTerminDo { get; set; }
}

public class NovePojisteniInput
{
    [Required(ErrorMessage = "Zadejte pojišťovnu.")]
    [StringLength(150)]
    public string Pojistovna { get; set; } = string.Empty;

    [StringLength(100)]
    public string? CisloSmlouvy { get; set; }

    [StringLength(100)]
    public string? Typ { get; set; }

    public DateOnly? PlatnostOd { get; set; }
    public DateOnly? PlatnostDo { get; set; }

    [Range(0, 100_000_000)]
    public decimal? RocniCenaKc { get; set; }

    [StringLength(500)]
    public string? Poznamka { get; set; }
}
