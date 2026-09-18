using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NfcHomeManager.Data;
using NfcHomeManager.Models;

namespace NfcHomeManager.Pages.Leky;

public class PridatModel(AppDbContext db) : PageModel
{
    [BindProperty] public LekWizardInput Input { get; set; } = new();
    public List<Polozka> Sady { get; private set; } = [];
    public List<LekPripravek> Pripravky { get; private set; } = [];
    public bool Uprava => Input.LekId.HasValue;

    private async Task LoadAsync(CancellationToken ct)
    {
        Sady = await db.Polozky.AsNoTracking().Where(p => p.Aktivni &&
            (p.Rezim == NfcRezim.Lekarnicka || p.Rezim == NfcRezim.PrvniPomoc))
            .OrderBy(p => p.Nazev).ToListAsync(ct);
        Pripravky = await db.LekPripravky.AsNoTracking().OrderBy(p => p.Nazev)
            .ThenBy(p => p.Sila).ThenBy(p => p.Forma).ToListAsync(ct);
    }

    public async Task<IActionResult> OnGetAsync(int? sadaId, int? lekId, CancellationToken ct)
    {
        await LoadAsync(ct);
        Input.LekarnickaId = sadaId ?? 0;
        if (lekId.HasValue)
        {
            var lek = await db.Leky.AsNoTracking().FirstOrDefaultAsync(l => l.Id == lekId && l.JeLek, ct);
            if (lek is null) return NotFound();
            if (!Sady.Any(p => p.Id == lek.LekarnickaId)) return BadRequest("Nejdříve obnov sadu nebo uprav její režim.");
            Input = new LekWizardInput
            {
                LekId = lek.Id, Verze = lek.Verze, LekarnickaId = lek.LekarnickaId,
                PripravekId = lek.PripravekId, Ean = lek.Ean, Nazev = lek.Nazev,
                Mnozstvi = lek.Mnozstvi, MnozstviNezname = !lek.Mnozstvi.HasValue,
                Jednotka = lek.Jednotka ?? "tableta", Expirace = lek.Expirace,
                ExpiraceNeznama = !lek.Expirace.HasValue, Sarze = lek.Sarze,
                DatumOtevreni = lek.DatumOtevreni, Poznamka = lek.Poznamka,
                SledovatExpiraci = lek.SledovatExpiraci
            };
            var product = Pripravky.FirstOrDefault(p => p.Id == lek.PripravekId);
            if (product is not null)
            {
                Input.Nazev = product.Nazev;
                Input.Sila = product.Sila;
                Input.Forma = product.Forma;
                Input.ObsahBaleni = product.ObsahBaleni;
                Input.Jednotka = product.Jednotka;
                Input.Ean = product.Ean;
            }
        }
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(bool pridatDalsi, CancellationToken ct)
    {
        await LoadAsync(ct);
        if (!Sady.Any(p => p.Id == Input.LekarnickaId))
            ModelState.AddModelError("Input.LekarnickaId", "Vyber aktivní lékárničku nebo první pomoc.");
        if (!Guid.TryParseExact(Input.OperaceId, "N", out _))
            ModelState.AddModelError(string.Empty, "Neplatná operace. Otevři průvodce znovu.");

        LekPripravek? selected = null;
        if (Input.PripravekId.HasValue)
        {
            selected = Pripravky.FirstOrDefault(p => p.Id == Input.PripravekId);
            if (selected is null) ModelState.AddModelError("Input.PripravekId", "Přípravek již není dostupný.");
        }
        else
        {
            Input.Nazev = (Input.Nazev ?? "").Trim();
            Input.Ean = Clean(Input.Ean);
            Input.Sila = Clean(Input.Sila);
            Input.Forma = Clean(Input.Forma);
            if (Input.Nazev.Length == 0) ModelState.AddModelError("Input.Nazev", "Zadej název z krabičky.");
            if (Input.Ean is not null && !ValidBarcode(Input.Ean))
                ModelState.AddModelError("Input.Ean", "Zadej platný EAN/GTIN (8, 12, 13 nebo 14 číslic), nebo kód vynech.");
            if (!Units.Contains(Input.Jednotka)) ModelState.AddModelError("Input.Jednotka", "Vyber jednotku.");
        }
        if (!Input.MnozstviNezname && !Input.Mnozstvi.HasValue)
            ModelState.AddModelError("Input.Mnozstvi", "Zadej zůstatek nebo označ, že jej neznáš.");
        if (!Input.ExpiraceNeznama && !Input.Expirace.HasValue)
            ModelState.AddModelError("Input.Expirace", "Zadej expiraci nebo označ, že ji doplníš později.");
        if (Input.DatumOtevreni > DateOnly.FromDateTime(DateTime.Today))
            ModelState.AddModelError("Input.DatumOtevreni", "Datum otevření nesmí být v budoucnosti.");
        var capacity = selected?.ObsahBaleni ?? (selected is null ? Input.ObsahBaleni : null);
        if (!Input.MnozstviNezname && capacity.HasValue && Input.Mnozstvi > capacity)
            ModelState.AddModelError("Input.Mnozstvi", "Zůstatek jedné krabičky nemůže překročit její obsah. Každou krabičku přidej zvlášť.");
        if (!ModelState.IsValid) return Page();

        try
        {
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            if (!Uprava)
            {
                var previous = await db.Leky.AsNoTracking().FirstOrDefaultAsync(l => l.OperaceId == Input.OperaceId, ct);
                if (previous is not null) return RedirectToPage("/Polozky/Detail", new { id = previous.LekarnickaId });
            }
            // Ověření rodiče se opakuje v transakci, aby se nemohl mezitím archivovat.
            var allowed = await db.Polozky.AnyAsync(p => p.Id == Input.LekarnickaId && p.Aktivni &&
                (p.Rezim == NfcRezim.Lekarnicka || p.Rezim == NfcRezim.PrvniPomoc), ct);
            if (!allowed) return BadRequest("Sada již není dostupná.");

            Lek? lek = null;
            if (Uprava)
            {
                lek = await db.Leky.FirstOrDefaultAsync(l => l.Id == Input.LekId && l.JeLek, ct);
                if (lek is null) return NotFound();
                if (lek.Verze != Input.Verze)
                {
                    ModelState.AddModelError(string.Empty, "Krabičku mezitím někdo změnil. Otevři ji znovu a zkontroluj aktuální množství.");
                    return Page();
                }
            }

            var product = selected is null ? null : await db.LekPripravky.FindAsync([selected.Id], ct);
            if (selected is not null && product is null)
            {
                ModelState.AddModelError("Input.PripravekId", "Přípravek mezitím zmizel. Vyber jej znovu.");
                return Page();
            }
            if (product is null)
            {
                var key = ProductKey(Input);
                product = await db.LekPripravky.FirstOrDefaultAsync(p => p.Klic == key, ct);
                var byCode = Input.Ean is null ? null : await db.LekPripravky.FirstOrDefaultAsync(p => p.Ean == Input.Ean, ct);
                if (byCode is not null && byCode.Klic != key)
                {
                    ModelState.AddModelError("Input.Ean", "Tento kód už patří jinému záznamu. Vyber existující přípravek a ověř krabičku.");
                    return Page();
                }
                if (product is not null && product.Ean is not null && Input.Ean is not null && product.Ean != Input.Ean)
                {
                    ModelState.AddModelError("Input.Ean", "Stejný přípravek má uložený jiný kód. Ověř variantu a velikost balení.");
                    return Page();
                }
                if (product is null)
                {
                    product = new LekPripravek { Klic = key, Nazev = Input.Nazev!, Sila = Input.Sila,
                        Forma = Input.Forma, Ean = Input.Ean, ObsahBaleni = Input.ObsahBaleni, Jednotka = Input.Jednotka };
                    db.LekPripravky.Add(product);
                }
                else if (product.Ean is null && Input.Ean is not null) product.Ean = Input.Ean;
            }

            if (lek is null)
            {
                lek = new Lek { OperaceId = Input.OperaceId, JeLek = true };
                db.Leky.Add(lek);
            }
            lek.Pripravek = product;
            // Kompatibilní snímek pro staré přehledy/export. Zdroj identity je PripravekId.
            lek.Nazev = product.Popisek;
            lek.Ean = product.Ean;
            lek.Jednotka = product.Jednotka;
            lek.LekarnickaId = Input.LekarnickaId;
            lek.Mnozstvi = Input.MnozstviNezname ? null : Input.Mnozstvi;
            lek.Expirace = Input.ExpiraceNeznama ? null : Input.Expirace;
            lek.Sarze = Clean(Input.Sarze);
            lek.DatumOtevreni = Input.DatumOtevreni;
            lek.SledovatExpiraci = Input.SledovatExpiraci;
            lek.Poznamka = Clean(Input.Poznamka);
            lek.UpravenoUtc = DateTime.UtcNow;
            lek.Verze++;
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        catch (DbUpdateException)
        {
            db.ChangeTracker.Clear();
            var previous = await db.Leky.AsNoTracking().FirstOrDefaultAsync(l => l.OperaceId == Input.OperaceId, ct);
            if (!Uprava && previous is not null)
                return RedirectToPage("/Polozky/Detail", new { id = previous.LekarnickaId });
            ModelState.AddModelError(string.Empty, "Zápis se nezdařil nebo mezitím došlo ke změně. Zkontroluj výběr přípravku a zkus znovu.");
            return Page();
        }
        if (pridatDalsi && !Uprava) return RedirectToPage(new { sadaId = Input.LekarnickaId });
        return RedirectToPage("/Polozky/Detail", new { id = Input.LekarnickaId });
    }

    public static readonly string[] Units = ["tableta", "kapsle", "čípek", "sáček", "ml", "g", "ks", "balení"];
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    public static string ProductKey(LekWizardInput input)
    {
        static string Normalize(string? value) => Regex.Replace(value?.Trim() ?? "", @"\s+", " ").ToUpperInvariant();
        var parts = new[] { Normalize(input.Nazev), Normalize(input.Sila), Normalize(input.Forma),
            input.ObsahBaleni?.ToString("0.###", CultureInfo.InvariantCulture) ?? "", Normalize(input.Jednotka) };
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(parts))));
    }
    public static bool ValidBarcode(string value)
    {
        if (value.Length is not (8 or 12 or 13 or 14) || value.Any(c => c < '0' || c > '9')) return false;
        var sum = 0;
        for (var i = value.Length - 2; i >= 0; i--)
            sum += (value[i] - '0') * ((value.Length - 2 - i) % 2 == 0 ? 3 : 1);
        return (10 - sum % 10) % 10 == value[^1] - '0';
    }
}

public class LekWizardInput
{
    public int? LekId { get; set; }
    public int Verze { get; set; }
    [Required, StringLength(32)] public string OperaceId { get; set; } = Guid.NewGuid().ToString("N");
    public int LekarnickaId { get; set; }
    public int? PripravekId { get; set; }
    [StringLength(200)] public string? Nazev { get; set; }
    [StringLength(20)] public string? Ean { get; set; }
    [StringLength(100)] public string? Sila { get; set; }
    [StringLength(100)] public string? Forma { get; set; }
    [Range(typeof(decimal), "0.001", "1000000")] public decimal? ObsahBaleni { get; set; }
    [StringLength(20)] public string Jednotka { get; set; } = "tableta";
    [Range(typeof(decimal), "0", "1000000")] public decimal? Mnozstvi { get; set; }
    public bool MnozstviNezname { get; set; }
    public DateOnly? Expirace { get; set; }
    public bool ExpiraceNeznama { get; set; }
    [StringLength(100)] public string? Sarze { get; set; }
    public DateOnly? DatumOtevreni { get; set; }
    public bool SledovatExpiraci { get; set; } = true;
    [StringLength(500)] public string? Poznamka { get; set; }
}
