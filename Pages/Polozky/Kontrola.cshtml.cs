using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NfcHomeManager.Data;
using NfcHomeManager.Models;
using NfcHomeManager.Services;
using System.ComponentModel.DataAnnotations;

namespace NfcHomeManager.Pages.Polozky;

// Pruvodce kontrolou sady prvni pomoci: po skupinach vybaveni se zapise
// skutecna zasoba a expirace, na konci se kontrola potvrdi. Vse je jeden
// formular - kroky prepina site.js, bez JavaScriptu je to jedna dlouha stranka.
// Stav sady se pocita ze zapsanych zasob; potvrzeni neuplnou sadu "neopravi".
public class KontrolaModel(AppDbContext db) : PageModel
{
    public Polozka Sada { get; set; } = null!;

    // Kroky pruvodce: skupina a indexy jejich radku v Radky.
    public List<(string Skupina, List<int> Indexy)> Kroky { get; set; } = [];
    public Dictionary<int, Lek> Vybaveni { get; set; } = [];

    [BindProperty]
    public List<KontrolaRadekInput> Radky { get; set; } = [];

    [BindProperty]
    [Required(ErrorMessage = "Zadej datum další kontroly.")]
    public DateOnly? DalsiKontrola { get; set; }

    [BindProperty]
    [StringLength(500)]
    public string? Poznamka { get; set; }

    public async Task<IActionResult> OnGetAsync(int id, CancellationToken ct)
    {
        if (!await NacistAsync(id, ct))
        {
            return NotFound();
        }

        if (Sada.Leky.Count == 0)
        {
            return Redirect($"/Polozky/Detail?id={id}");
        }

        SestavitKroky(zadane: null);
        DalsiKontrola = DateOnly.FromDateTime(DateTime.Today).AddMonths(StavSady.MesicuDoDalsiKontroly);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id, Guid operaceId, CancellationToken ct)
    {
        if (operaceId == Guid.Empty)
        {
            return BadRequest();
        }

        if (await JednorazovaOperace.PresmerovaniAsync(db, operaceId, ct) is { } drive)
        {
            return Redirect(drive);
        }

        if (!await NacistAsync(id, ct))
        {
            return NotFound();
        }

        var dnes = DateOnly.FromDateTime(DateTime.Today);
        var zadane = Radky.GroupBy(r => r.Id).ToDictionary(g => g.Key, g => g.First());

        // Radek, ktery do teto sady nepatri, znamena rucne sestaveny POST.
        if (zadane.Keys.Any(lekId => Sada.Leky.All(l => l.Id != lekId)))
        {
            return BadRequest();
        }

        if (DalsiKontrola is { } dalsi && dalsi < dnes)
        {
            ModelState.AddModelError(nameof(DalsiKontrola), "Další kontrola nemůže být v minulosti.");
        }

        if (!ModelState.IsValid)
        {
            SestavitKroky(zadane);
            return Page();
        }

        // Polozka, ktera ve formulari nebyla (pridana mezitim z jineho telefonu), zustava beze zmeny.
        foreach (var lek in Sada.Leky)
        {
            if (zadane.TryGetValue(lek.Id, out var radek))
            {
                lek.Mnozstvi = radek.Mnozstvi;
                lek.Expirace = radek.Expirace;
            }
        }

        var stav = StavSady.Vyhodnotit(Sada.Leky, dnes);
        db.KontrolySady.Add(new KontrolaSady
        {
            PolozkaId = id,
            Datum = dnes,
            Chybi = stav.Chybi.Count,
            Prosle = stav.Prosle.Count,
            BrzyExpiruje = stav.BrzyExpiruje.Count,
            Souhrn = stav.Souhrn() is { Length: > 0 } souhrn ? souhrn : null,
            Poznamka = string.IsNullOrWhiteSpace(Poznamka) ? null : Poznamka.Trim()
        });

        var termin = Sada.Terminy.FirstOrDefault(t => t.Typ == TerminTyp.Kontrola);
        if (termin is null)
        {
            Sada.Terminy.Add(new Termin { Typ = TerminTyp.Kontrola, DatumDo = DalsiKontrola!.Value });
        }
        else
        {
            termin.DatumDo = DalsiKontrola!.Value;
            termin.UpravenoUtc = DateTime.UtcNow;
        }

        Sada.UpravenoUtc = DateTime.UtcNow;

        var cil = $"/Polozky/Detail?id={id}";
        await JednorazovaOperace.UlozitAsync(db, operaceId, cil, ct);
        return Redirect(cil);
    }

    private async Task<bool> NacistAsync(int id, CancellationToken ct)
    {
        var sada = await db.Polozky
            .Include(p => p.Leky)
            .Include(p => p.Terminy)
            .FirstOrDefaultAsync(p => p.Id == id && p.Rezim == NfcRezim.PrvniPomoc && p.Aktivni, ct);

        if (sada is null)
        {
            return false;
        }

        Sada = sada;
        return true;
    }

    // Radky jsou vzdy v poradi skupin z databaze; hodnoty z odeslaneho
    // formulare (pri chybe validace) se do nich doplni podle Id.
    private void SestavitKroky(Dictionary<int, KontrolaRadekInput>? zadane)
    {
        Radky = [];
        Kroky = [];
        Vybaveni = Sada.Leky.ToDictionary(l => l.Id);

        foreach (var (skupina, polozky) in StavSady.PoSkupinach(Sada.Leky))
        {
            var indexy = new List<int>();
            foreach (var lek in polozky)
            {
                indexy.Add(Radky.Count);
                Radky.Add(zadane?.GetValueOrDefault(lek.Id) ??
                          new KontrolaRadekInput { Id = lek.Id, Mnozstvi = lek.Mnozstvi, Expirace = lek.Expirace });
            }

            Kroky.Add((skupina, indexy));
        }
    }
}

public class KontrolaRadekInput
{
    public int Id { get; set; }

    // Prazdne = nespocitano; u polozky s cilovou zasobou se pak pocita jako chybejici.
    [Range(0, 1_000_000, ErrorMessage = "Množství musí být 0 až 1 000 000.")]
    public decimal? Mnozstvi { get; set; }

    public DateOnly? Expirace { get; set; }
}
